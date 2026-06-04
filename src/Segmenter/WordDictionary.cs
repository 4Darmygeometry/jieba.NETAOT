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
        /// 词典中实际出现的最大词长（按 .Length 计）。
        /// 初始化为 30 保持向后兼容；词典加载完毕后会被覆盖为实际最大值；
        /// 用户通过 AddWord 添加新词时若超过当前最大值，也会自动扩展。
        /// 供 FindLongestWordLength / FindLongestWordEndingAt / MatchEmoji 等限定
        /// 最大扫描长度的方法使用，避免硬编码 30 把过长的词典词切碎。
        /// </summary>
        private int _maxWordLength = 30;
        private readonly object _maxWordLengthLock = new object();

        /// <summary>
        /// Emoji专用前缀树，用于快速匹配复杂emoji（ZWJ序列等）
        /// </summary>
        internal readonly ConcurrentDictionary<string, int> EmojiTrie = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// 字符串池，用于缓存高频字符串切片，减少GC压力
        /// </summary>
        private readonly ConcurrentDictionary<int, string> _stringPool = new ConcurrentDictionary<int, string>();

        /// <summary>
        /// 带空格词集合，用于快速查找文本中的带空格词典词
        /// </summary>
        internal readonly ConcurrentDictionary<string, byte> SpaceContainingWords = new ConcurrentDictionary<string, byte>();

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
            var cacheKey = $"{effectiveConfig.Mode}_{effectiveConfig.EntityProtect}";

            return _cache.GetOrAdd(cacheKey, _ => new Lazy<WordDictionary>(() =>
            {
                var dict = new WordDictionary(true);
                dict.LoadDictAsync(effectiveConfig).GetAwaiter().GetResult();

                Debug.WriteLine("{0} words (and their prefixes)", dict.Trie.Count);
                Debug.WriteLine("total freq: {0}", dict.Total);
                Debug.WriteLine("加载模式: {0}, 实体保护: {1}", effectiveConfig.Mode, effectiveConfig.EntityProtect);
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
            Debug.WriteLine("[独立词典] 加载模式: {0}, 实体保护: {1}", effectiveConfig.Mode, effectiveConfig.EntityProtect);
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
            var cacheKey = $"{effectiveConfig.Mode}_{effectiveConfig.EntityProtect}";

            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached.Value;
            }

            var dict = new WordDictionary(true);
            await dict.LoadDictAsync(effectiveConfig).ConfigureAwait(false);

            Debug.WriteLine("{0} words (and their prefixes)", dict.Trie.Count);
            Debug.WriteLine("total freq: {0}", dict.Total);
            Debug.WriteLine("加载模式: {0}, 实体保护: {1}", effectiveConfig.Mode, effectiveConfig.EntityProtect);

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
        /// 支持带空格词：倒数第二个元素为HMM发射频数，倒数第一个元素为词性
        /// 格式：word freq 或 word freq tag 或 word with spaces freq tag
        /// </summary>
        private void ProcessDictLine(string line)
        {
            var tokens = line.Split(' ');
            if (tokens.Length < 2)
            {
                return;
            }

            string word;
            int freq;

            // 支持带空格词：tokens.Length >= 3且倒数第二个元素为有效整数时
            // 格式：word_with_spaces freq tag 或 word freq tag
            if (tokens.Length >= 3 && int.TryParse(tokens[tokens.Length - 2], out freq))
            {
                word = string.Join(" ", tokens, 0, tokens.Length - 2);
            }
            else if (int.TryParse(tokens[1], out freq))
            {
                // 传统格式：word freq
                word = tokens[0];
            }
            else
            {
                return;
            }

            if (string.IsNullOrEmpty(word))
            {
                return;
            }

            Trie[word] = freq;
            AddToTotal(freq);

            // 记录最大词长（仅在频率>0 的真实词上更新，前缀不参与）
            if (freq > 0 && word.Length > _maxWordLength)
            {
                lock (_maxWordLengthLock)
                {
                    if (word.Length > _maxWordLength)
                        _maxWordLength = word.Length;
                }
            }

            // 记录带空格词，用于分词时整体保护
            if (word.Contains(' '))
            {
                SpaceContainingWords.TryAdd(word, 0);
            }

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

        /// <summary>
        /// 检查字符串是否包含ASCII字母（用于判断是否需要大小写不敏感回退）
        /// </summary>
        private static bool HasAsciiLetter(string word)
        {
            foreach (char c in word)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 检查ReadOnlySpan是否包含ASCII字母
        /// </summary>
        private static bool HasAsciiLetter(ReadOnlySpan<char> word)
        {
            foreach (char c in word)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    return true;
            }
            return false;
        }

        public bool ContainsWord(string word)
        {
            if (Trie.TryGetValue(word, out var freq) && freq > 0)
                return true;
            // 大小写不敏感回退：仅当词中包含ASCII字母时才尝试
            if (HasAsciiLetter(word))
            {
                var upper = word.ToUpperInvariant();
                if (upper != word)
                    return Trie.TryGetValue(upper, out freq) && freq > 0;
            }
            return false;
        }

        /// <summary>
        /// 使用Span检查词是否存在（高性能版本）
        /// </summary>
        public bool ContainsWord(ReadOnlySpan<char> word)
        {
            var key = GetOrCreateString(word);
            if (Trie.TryGetValue(key, out var freq) && freq > 0)
                return true;
            // 大小写不敏感回退
            if (HasAsciiLetter(word))
            {
                var upper = key.ToUpperInvariant();
                if (upper != key)
                    return Trie.TryGetValue(upper, out freq) && freq > 0;
            }
            return false;
        }

        public int GetFreqOrDefault(string key)
        {
            if (Trie.TryGetValue(key, out var freq) && freq > 0)
                return freq;
            // 大小写不敏感回退
            if (HasAsciiLetter(key))
            {
                var upper = key.ToUpperInvariant();
                if (upper != key && Trie.TryGetValue(upper, out freq) && freq > 0)
                    return freq;
            }
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
            // 大小写不敏感回退
            if (HasAsciiLetter(key))
            {
                var upper = str.ToUpperInvariant();
                if (upper != str && Trie.TryGetValue(upper, out freq) && freq > 0)
                    return freq;
            }
            return 1;
        }

        /// <summary>
        /// 检查前缀是否存在于Trie中（高性能版本）
        /// 用于DAG构建时的快速查找
        /// </summary>
        public bool ContainsPrefix(ReadOnlySpan<char> prefix)
        {
            var key = GetOrCreateString(prefix);
            if (Trie.ContainsKey(key))
                return true;
            // 大小写不敏感回退
            if (HasAsciiLetter(prefix))
            {
                var upper = key.ToUpperInvariant();
                if (upper != key)
                    return Trie.ContainsKey(upper);
            }
            return false;
        }

        /// <summary>
        /// 获取前缀的频率值（0表示只是前缀，>0表示是完整词）
        /// </summary>
        public int GetTrieValue(ReadOnlySpan<char> key)
        {
            var str = GetOrCreateString(key);
            if (Trie.TryGetValue(str, out var value))
                return value;
            // 大小写不敏感回退
            if (HasAsciiLetter(key))
            {
                var upper = str.ToUpperInvariant();
                if (upper != str && Trie.TryGetValue(upper, out value))
                    return value;
            }
            return -1;
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

            // 记录最大词长（仅在频率>0 的真实词上更新，前缀不参与）
            if (freq > 0 && word.Length > _maxWordLength)
            {
                lock (_maxWordLengthLock)
                {
                    if (word.Length > _maxWordLength)
                        _maxWordLength = word.Length;
                }
            }

            // 记录带空格词
            if (word.Contains(' '))
            {
                SpaceContainingWords.TryAdd(word, 0);
            }

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

            // 限制最大匹配长度：根据 emojiTrie 实际最大键长（带安全余量）
            var maxCheck = Math.Min(text.Length - startIndex, MaxScanLength);

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
        /// 最大扫描长度。默认 30（emoji 与词典词硬编码历史值），
        /// 词典加载或 AddWord 时若检测到更长词条会自动扩展。
        /// 公开该属性的目的是让 RegexTimeRecognizer 等组件
        /// 在做最大长度截断时与词典保持一致。
        /// </summary>
        public int MaxScanLength
        {
            get { lock (_maxWordLengthLock) { return _maxWordLength; } }
        }

        /// <summary>
        /// 查找从指定位置开始的最长词典词长度。
        /// 用于判断时间实体是否为词典词的前缀（避免破坏词典词的完整性）。
        /// 例如：在"百年孤独"中，从位置0开始查找，最长词典词为"百年孤独"（长度4）。
        /// </summary>
        /// <param name="text">源文本</param>
        /// <param name="start">开始匹配的位置</param>
        /// <returns>最长词典词的长度（字符数），如果没有找到返回0</returns>
        public int FindLongestWordLength(string text, int start)
        {
            if (string.IsNullOrEmpty(text) || start < 0 || start >= text.Length)
                return 0;

            var textSpan = text.AsSpan(start);
            var maxLen = 0;
            var len = 1;

            // 限制最大匹配长度，使用词典动态维护的最大词长（避免硬编码 30 切碎长词）
            var maxCheck = Math.Min(textSpan.Length, MaxScanLength);

            while (len <= maxCheck)
            {
                var frag = textSpan.Slice(0, len);
                if (ContainsPrefix(frag))
                {
                    if (GetTrieValue(frag) > 0)
                    {
                        maxLen = len;
                    }
                    len++;
                }
                else
                {
                    break;
                }
            }

            return maxLen;
        }

        /// <summary>
        /// 查找在指定位置结束的词典词的最大长度。
        /// 用于判断时间实体是否为词典词的后缀（避免破坏词典词的完整性）。
        /// 例如：在"一笑千年"中，在位置4结束查找，最长词典词为"一笑千年"（长度4）。
        /// </summary>
        /// <param name="text">源文本</param>
        /// <param name="end">结束位置（开区间，词典词的最后一个字符索引+1）</param>
        /// <returns>最长词典词的长度（字符数），如果没有找到返回0</returns>
        public int FindLongestWordEndingAt(string text, int end)
        {
            if (string.IsNullOrEmpty(text) || end <= 0 || end > text.Length)
                return 0;

            var textSpan = text.AsSpan();
            var maxLen = 0;
            // 限制向前查找的最大长度，使用词典动态维护的最大词长
            var maxCheck = Math.Min(end, MaxScanLength);

            // 从 end-1 向前遍历，逐个检查 [end-len, end) 是否为词典词
            for (var len = 1; len <= maxCheck; len++)
            {
                var frag = textSpan.Slice(end - len, len);
                if (GetTrieValue(frag) > 0)
                {
                    maxLen = len;
                }
            }

            return maxLen;
        }

        /// <summary>
        /// 过滤时间实体：如果某个时间实体是某个词典词的前缀或后缀（即存在更长的词典词），
        /// 则认为该时间实体不应该被提取（避免破坏词典词的完整性）。
        ///
        /// 规则：
        /// - 时间实体 T (长度 L_T) 在位置 [S, E)
        /// - 词典中存在以 S 开头的最长词，长度为 L_start
        /// - 词典中存在以 E 结尾的最长词，长度为 L_end
        /// - 若 L_start == L_T 且 L_end == L_T：T 本身就是完整词典词，保留
        /// - 若 L_start &gt; L_T：T 是某个更长词典词的前缀，丢弃
        /// - 若 L_end &gt; L_T：T 是某个更长词典词的后缀，丢弃
        ///
        /// 取消强弱类型判定：对所有类型一视同仁，统一保留完整词典词。
        /// 例如"今年春节"作为整体提取后是 festival 实体，且"今年春节"本身是完整词典词，
        ///   T=春节 在"妈，今年春节是..."中会作为"今年春节"的后缀被丢弃；
        ///   而 T=今年春节 则是完整词典词被保留。
        /// 性能：每个时间实体最多两次 O(L) Trie 遍历。
        /// 该方法被 RegexTimeRecognizer 调用，确保 ITimeRecognizer 公开 API
        /// 不会因词典词前缀/后缀而误识别（如"百年孤独"中的"百年"、"今年春节"中的"春节"）。
        /// </summary>
        public List<TimeEntity> FilterTimeEntitiesByDictionary(string text, List<TimeEntity> timeEntities)
        {
            if (timeEntities == null || timeEntities.Count == 0)
                return timeEntities ?? new List<TimeEntity>();

            var filtered = new List<TimeEntity>(timeEntities.Count);
            foreach (var entity in timeEntities)
            {
                var entityLen = entity.Text.Length;
                var longestStartLen = FindLongestWordLength(text, entity.Start);
                var longestEndLen = FindLongestWordEndingAt(text, entity.End);

                // 前缀检查：存在以 entity.Start 开头的更长词典词
                // 后缀检查：存在以 entity.End 结尾的更长词典词
                if (longestStartLen <= entityLen && longestEndLen <= entityLen)
                {
                    // 不是词典词的前缀或后缀（或本身就是完整词典词），保留
                    filtered.Add(entity);
                }
                // 否则，该时间实体是词典词的前缀或后缀，会破坏词典词的完整性，丢弃
            }
            return filtered;
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
