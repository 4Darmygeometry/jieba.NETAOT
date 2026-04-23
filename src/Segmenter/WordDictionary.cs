using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JiebaNet.Segmenter.Common;

namespace JiebaNet.Segmenter
{
    public class WordDictionary
    {
        // 词典缓存：避免重复加载相同配置的词典
        private static readonly ConcurrentDictionary<string, Lazy<WordDictionary>> _cache
            = new ConcurrentDictionary<string, Lazy<WordDictionary>>();

        private static readonly Lazy<WordDictionary> lazy = new Lazy<WordDictionary>(() => new WordDictionary());
        private static readonly string MainDict = ConfigManager.MainDictFile;
        private static readonly string EmojiDict = ConfigManager.EmojiDictFile;
        private static readonly string MainDictHant = ConfigManager.MainDictHantFile;

        // 使用并发字典，支持并行加载
        internal readonly ConcurrentDictionary<string, int> Trie = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// Emoji专用前缀树，用于快速匹配复杂emoji（ZWJ序列等）
        /// </summary>
        internal readonly ConcurrentDictionary<string, int> EmojiTrie = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// 字符串池，用于缓存高频字符串切片，减少GC压力
        /// </summary>
        private readonly ConcurrentDictionary<int, string> _stringPool = new ConcurrentDictionary<int, string>();

        /// <summary>
        /// total occurrence of all words.
        /// </summary>
        public double Total { get; set; }

        // 用于线程安全的Total累加
        private double _total;
        private readonly object _totalLock = new object();

        private WordDictionary()
        {
            // 直接同步等待异步加载（内部已使用ConfigureAwait(false)避免死锁）
            LoadDictAsync().GetAwaiter().GetResult();

            Debug.WriteLine("{0} words (and their prefixes)", Trie.Count);
            Debug.WriteLine("total freq: {0}", Total);
        }

        /// <summary>
        /// 使用指定配置创建词典实例（带缓存）
        /// 相同配置会复用已加载的词典实例
        /// 适用于JiebaSegmenter等不需要独立词典的场景
        /// </summary>
        /// <param name="config">分词器配置，控制加载哪些词库</param>
        internal static WordDictionary GetOrCreate(JiebaConfig config)
        {
            var effectiveConfig = config.ApplyAutoFallback(ConfigManager.ConfigFileBaseDir);
            var cacheKey = $"{effectiveConfig.Mode}_{effectiveConfig.Emoji}";

            return _cache.GetOrAdd(cacheKey, _ => new Lazy<WordDictionary>(() =>
            {
                var dict = new WordDictionary(true);
                dict.LoadDictAsync(effectiveConfig).GetAwaiter().GetResult();

                Debug.WriteLine("{0} words (and their prefixes)", dict.Trie.Count);
                Debug.WriteLine("total freq: {0}", dict.Total);
                Debug.WriteLine("加载模式: {0}, 表情包: {1}", effectiveConfig.Mode, effectiveConfig.Emoji);
                return dict;
            })).Value;
        }

        /// <summary>
        /// 创建独立的词典实例（不缓存）
        /// 每次调用都会创建新的词典实例，互不影响
        /// 适用于Tokenizer等需要独立词典的场景
        /// </summary>
        /// <param name="config">分词器配置，控制加载哪些词库</param>
        internal static WordDictionary CreateIndependent(JiebaConfig config)
        {
            var effectiveConfig = config.ApplyAutoFallback(ConfigManager.ConfigFileBaseDir);
            var dict = new WordDictionary(true);
            dict.LoadDictAsync(effectiveConfig).GetAwaiter().GetResult();

            Debug.WriteLine("[独立词典] {0} words (and their prefixes)", dict.Trie.Count);
            Debug.WriteLine("[独立词典] total freq: {0}", dict.Total);
            Debug.WriteLine("[独立词典] 加载模式: {0}, 表情包: {1}", effectiveConfig.Mode, effectiveConfig.Emoji);
            return dict;
        }

        /// <summary>
        /// 私有构造函数，用于异步工厂方法
        /// 不执行任何加载，由调用方负责初始化
        /// </summary>
        private WordDictionary(bool skipLoad)
        {
            // 异步工厂模式：不在此处加载词典
        }

        /// <summary>
        /// 异步创建词典实例（全量加载，带缓存）
        /// </summary>
        public static async Task<WordDictionary> CreateAsync()
        {
            var cacheKey = "full_all";

            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached.Value;
            }

            var dict = new WordDictionary(true);
            await dict.LoadDictAsync().ConfigureAwait(false);

            Debug.WriteLine("{0} words (and their prefixes)", dict.Trie.Count);
            Debug.WriteLine("total freq: {0}", dict.Total);

            _cache.TryAdd(cacheKey, new Lazy<WordDictionary>(() => dict));
            return dict;
        }

        /// <summary>
        /// 异步创建词典实例（按配置加载，带缓存）
        /// </summary>
        /// <param name="config">分词器配置</param>
        public static async Task<WordDictionary> CreateAsync(JiebaConfig config)
        {
            var effectiveConfig = config.ApplyAutoFallback(ConfigManager.ConfigFileBaseDir);
            var cacheKey = $"{effectiveConfig.Mode}_{effectiveConfig.Emoji}";

            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached.Value;
            }

            var dict = new WordDictionary(true);
            await dict.LoadDictAsync(effectiveConfig).ConfigureAwait(false);

            Debug.WriteLine("{0} words (and their prefixes)", dict.Trie.Count);
            Debug.WriteLine("total freq: {0}", dict.Total);
            Debug.WriteLine("加载模式: {0}, 表情包: {1}", effectiveConfig.Mode, effectiveConfig.Emoji);

            _cache.TryAdd(cacheKey, new Lazy<WordDictionary>(() => dict));
            return dict;
        }

        /// <summary>
        /// 清除词典缓存（用于重新加载词典）
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }

        public static WordDictionary Instance
        {
            get { return lazy.Value; }
        }

        #region 异步加载方法

        /// <summary>
        /// 异步加载词典（全量模式）
        /// 使用内存映射文件和并行加载优化
        /// </summary>
        private async Task LoadDictAsync()
        {
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                // 并行异步加载所有词典
                var tasks = new List<Task>
                {
                    LoadDictFileWithMemoryMapAsync(MainDict, "主词典(简体)"),
                    LoadDictFileWithMemoryMapAsync(MainDictHant, "繁体中文词典"),
                    LoadEmojiDictFileWithMemoryMapAsync(EmojiDict)
                };

                await Task.WhenAll(tasks).ConfigureAwait(false);

                stopWatch.Stop();
                Debug.WriteLine("词典异步加载完成，耗时 {0} ms", stopWatch.ElapsedMilliseconds);
            }
            catch (IOException e)
            {
                Debug.Fail(string.Format("词典异步加载失败，原因: {0}", e.Message));
            }
            catch (FormatException fe)
            {
                Debug.Fail(fe.Message);
            }
        }

        /// <summary>
        /// 异步加载词典（按配置）
        /// 使用内存映射文件和并行加载优化
        /// </summary>
        /// <param name="config">分词器配置</param>
        private async Task LoadDictAsync(JiebaConfig config)
        {
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                var tasks = new List<Task>();

                if (config.ShouldLoadZhHans)
                {
                    tasks.Add(LoadDictFileWithMemoryMapAsync(MainDict, "主词典(简体)"));
                }

                if (config.ShouldLoadZhHant)
                {
                    tasks.Add(LoadDictFileWithMemoryMapAsync(MainDictHant, "繁体中文词典"));
                }

                if (config.ShouldLoadEmoji)
                {
                    tasks.Add(LoadEmojiDictFileWithMemoryMapAsync(EmojiDict));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                stopWatch.Stop();
                Debug.WriteLine("词典异步加载完成（按配置），耗时 {0} ms", stopWatch.ElapsedMilliseconds);
            }
            catch (IOException e)
            {
                Debug.Fail(string.Format("词典异步加载失败，原因: {0}", e.Message));
            }
            catch (FormatException fe)
            {
                Debug.Fail(fe.Message);
            }
        }

        /// <summary>
        /// 使用内存映射文件异步加载词典文件
        /// 适用于大词典文件，减少内存拷贝
        /// </summary>
        /// <param name="dictFile">词典文件路径</param>
        /// <param name="dictName">词典名称（用于日志）</param>
        private async Task LoadDictFileWithMemoryMapAsync(string dictFile, string dictName)
        {
            if (!File.Exists(dictFile))
            {
                Debug.WriteLine("词典文件不存在: {0}", dictFile);
                return;
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var fileInfo = new FileInfo(dictFile);
            var fileSize = fileInfo.Length;

            // 小文件直接用StreamReader，大文件用内存映射
            if (fileSize < 1024 * 1024) // < 1MB
            {
                await LoadDictFileSmallAsync(dictFile, dictName, stopWatch).ConfigureAwait(false);
            }
            else
            {
                await LoadDictFileLargeAsync(dictFile, dictName, stopWatch).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 加载小词典文件（直接读取）
        /// </summary>
        private async Task LoadDictFileSmallAsync(string dictFile, string dictName, Stopwatch stopWatch)
        {
            using (var sr = new StreamReader(dictFile, Encoding.UTF8))
            {
                string line;
                while ((line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    ProcessDictLine(line);
                }
            }

            stopWatch.Stop();
            Debug.WriteLine("{0}异步加载完成，耗时 {1} ms", dictName, stopWatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// 加载大词典文件（内存映射）
        /// </summary>
        private async Task LoadDictFileLargeAsync(string dictFile, string dictName, Stopwatch stopWatch)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var mmf = MemoryMappedFile.CreateFromFile(dictFile, FileMode.Open, null, 0,
                        MemoryMappedFileAccess.Read))
                    using (var accessor = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
                    using (var sr = new StreamReader(accessor, Encoding.UTF8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            ProcessDictLine(line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("内存映射加载失败，回退到普通读取: {0}", ex.Message);
                    // 回退到普通读取
                    using (var sr = new StreamReader(dictFile, Encoding.UTF8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            ProcessDictLine(line);
                        }
                    }
                }
            }).ConfigureAwait(false);

            stopWatch.Stop();
            Debug.WriteLine("{0}异步加载完成（内存映射），耗时 {1} ms", dictName, stopWatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// 处理词典行（提取公共逻辑）
        /// </summary>
        private void ProcessDictLine(string line)
        {
            var tokens = line.Split(' ');
            if (tokens.Length < 2)
            {
                return;
            }

            var word = tokens[0];
            if (!int.TryParse(tokens[1], out var freq))
            {
                return;
            }

            Trie[word] = freq;
            AddToTotal(freq);

            // 并行构建前缀（使用Span优化）
            var wordSpan = word.AsSpan();
            for (var i = 0; i < wordSpan.Length; i++)
            {
                var wfrag = wordSpan.Slice(0, i + 1).ToString();
                Trie.TryAdd(wfrag, 0);
            }
        }

        /// <summary>
        /// 线程安全的Total累加
        /// </summary>
        private void AddToTotal(double value)
        {
            lock (_totalLock)
            {
                _total += value;
                Total = _total;
            }
        }

        /// <summary>
        /// 使用内存映射文件异步加载emoji词典
        /// </summary>
        /// <param name="dictFile">emoji词典文件路径</param>
        private async Task LoadEmojiDictFileWithMemoryMapAsync(string dictFile)
        {
            if (!File.Exists(dictFile))
            {
                Debug.WriteLine("emoji词典文件不存在: {0}", dictFile);
                return;
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var count = 0;

            using (var sr = new StreamReader(dictFile, Encoding.UTF8))
            {
                string line;
                while ((line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    // 解析格式：emoji 频率 词性
                    var tokens = line.Split(' ');
                    var emoji = tokens[0];
                    var freq = tokens.Length >= 2 && int.TryParse(tokens[1], out var f) ? f : 10000;

                    Trie[emoji] = freq;
                    AddToTotal(freq);

                    // 构建emoji专用前缀树（包含前缀，用于匹配）
                    EmojiTrie[emoji] = freq;
                    for (var i = 0; i < emoji.Length; i++)
                    {
                        var prefix = emoji.Substring(0, i + 1);
                        EmojiTrie.TryAdd(prefix, 0);
                    }

                    count++;
                }
            }

            stopWatch.Stop();
            Debug.WriteLine("emoji词典异步加载完成，共 {0} 个emoji，耗时 {1} ms", count, stopWatch.ElapsedMilliseconds);
        }

        #endregion

        public bool ContainsWord(string word)
        {
            return Trie.TryGetValue(word, out var freq) && freq > 0;
        }

        /// <summary>
        /// 使用Span检查词是否存在（高性能版本）
        /// </summary>
        public bool ContainsWord(ReadOnlySpan<char> word)
        {
            var key = GetOrCreateString(word);
            return Trie.TryGetValue(key, out var freq) && freq > 0;
        }

        public int GetFreqOrDefault(string key)
        {
            if (ContainsWord(key))
                return Trie[key];
            else
                return 1;
        }

        /// <summary>
        /// 使用Span获取词频（高性能版本）
        /// </summary>
        public int GetFreqOrDefault(ReadOnlySpan<char> key)
        {
            var str = GetOrCreateString(key);
            if (Trie.TryGetValue(str, out var freq) && freq > 0)
                return freq;
            return 1;
        }

        /// <summary>
        /// 检查前缀是否存在于Trie中（高性能版本）
        /// 用于DAG构建时的快速查找
        /// </summary>
        public bool ContainsPrefix(ReadOnlySpan<char> prefix)
        {
            var key = GetOrCreateString(prefix);
            return Trie.ContainsKey(key);
        }

        /// <summary>
        /// 获取前缀的频率值（0表示只是前缀，>0表示是完整词）
        /// </summary>
        public int GetTrieValue(ReadOnlySpan<char> key)
        {
            var str = GetOrCreateString(key);
            return Trie.TryGetValue(str, out var value) ? value : -1;
        }

        /// <summary>
        /// 从字符串池获取或创建字符串实例
        /// 减少重复字符串的分配
        /// </summary>
        private string GetOrCreateString(ReadOnlySpan<char> span)
        {
            var hash = span.GetSpanHashCode();
            // 先检查是否已存在
            if (_stringPool.TryGetValue(hash, out var cached))
            {
                return cached;
            }
            // 创建新字符串并缓存
            var newString = span.ToString();
            _stringPool.TryAdd(hash, newString);
            return newString;
        }

        public void AddWord(string word, int freq, string? tag = null)
        {
            if (ContainsWord(word))
            {
                AddToTotal(-Trie[word]);
            }

            Trie[word] = freq;
            AddToTotal(freq);
            for (var i = 0; i < word.Length; i++)
            {
                var wfrag = word.Substring(0, i + 1);
                Trie.TryAdd(wfrag, 0);
            }
        }

        public void DeleteWord(string word)
        {
            AddWord(word, 0);
        }

        internal int SuggestFreq(string word, IEnumerable<string> segments)
        {
            double freq = 1;
            foreach (var seg in segments)
            {
                freq *= GetFreqOrDefault(seg) / Total;
            }

            return Math.Max((int)(freq * Total) + 1, GetFreqOrDefault(word));
        }

        /// <summary>
        /// 尝试从文本的指定位置匹配最长的emoji
        /// 用于处理复杂emoji（ZWJ序列、变体选择符等）
        /// </summary>
        /// <param name="text">源文本</param>
        /// <param name="startIndex">开始匹配的位置</param>
        /// <returns>匹配到的emoji长度，如果没有匹配到返回0</returns>
        public int MatchEmoji(string text, int startIndex)
        {
            if (startIndex >= text.Length)
                return 0;

            var maxLen = 0;
            var len = 1;

            // 限制最大匹配长度（最长的emoji约20个字符）
            var maxCheck = Math.Min(text.Length - startIndex, 30);

            while (len <= maxCheck)
            {
                var substr = text.Substring(startIndex, len);
                if (EmojiTrie.TryGetValue(substr, out var freq))
                {
                    // 如果是完整emoji（freq > 0），记录长度
                    if (freq > 0)
                    {
                        maxLen = len;
                    }
                    len++;
                }
                else
                {
                    // 前缀不匹配，停止
                    break;
                }
            }

            return maxLen;
        }

        /// <summary>
        /// 检查指定位置是否可能是emoji的开始
        /// </summary>
        /// <param name="text">源文本</param>
        /// <param name="startIndex">开始位置</param>
        /// <returns>如果是emoji前缀返回true</returns>
        public bool IsEmojiPrefix(string text, int startIndex)
        {
            if (startIndex >= text.Length)
                return false;

            var ch = text[startIndex];
            // 快速检查：emoji通常是代理对或特定范围
            if (char.IsSurrogate(ch) && char.IsHighSurrogate(ch))
            {
                return true;
            }

            // 检查是否在emoji前缀树中
            if (startIndex < text.Length)
            {
                var substr = text.Substring(startIndex, 1);
                return EmojiTrie.ContainsKey(substr);
            }

            return false;
        }
    }
}
