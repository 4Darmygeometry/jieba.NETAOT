using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JiebaNet.Segmenter.Common;
using JiebaNet.Segmenter.FinalSeg;

namespace JiebaNet.Segmenter
{
    public class JiebaSegmenter
    {
        private static readonly WordDictionary WordDict = WordDictionary.Instance;
        private static readonly IFinalSeg FinalSeg = Viterbi.Instance;
        private static readonly ISet<string> LoadedPath = new HashSet<string>();

        private static readonly object locker = new object();

        /// <summary>
        /// 时间识别器实例（根据平台自动选择实现）
        /// 所有框架使用正则识别器
        /// </summary>
        private static readonly Lazy<ITimeRecognizer> TimeRecognizer = new Lazy<ITimeRecognizer>(() =>
        {
            return new RegexTimeRecognizer();
        });

        /// <summary>
        /// 当前分词器使用的词典实例（支持按配置加载）
        /// 当使用配置构造时，使用独立实例；否则使用全局单例
        /// </summary>
        internal WordDictionary CurrentWordDict { get; }

        internal IDictionary<string, string> UserWordTagTab { get; set; }

        #region Regular Expressions

        internal static readonly Regex RegexChineseDefault = new Regex(@"([\u4E00-\u9FD5a-zA-Z0-9+#&\._%·\-]+)", RegexOptions.Compiled);

        internal static readonly Regex RegexSkipDefault = new Regex(@"(\r\n|\s)", RegexOptions.Compiled);

        internal static readonly Regex RegexChineseCutAll = new Regex(@"([\u4E00-\u9FD5]+)", RegexOptions.Compiled);
        internal static readonly Regex RegexSkipCutAll = new Regex(@"[^a-zA-Z0-9+#\n]", RegexOptions.Compiled);

        internal static readonly Regex RegexEnglishChars = new Regex(@"[a-zA-Z0-9]", RegexOptions.Compiled);

        internal static readonly Regex RegexUserDict = new Regex("^(?<word>.+?)(?<freq> [0-9]+)?(?<tag> [a-z]+)?$", RegexOptions.Compiled);

        /// <summary>
        /// Emoji正则表达式：匹配常见emoji范围
        /// 包括：表情符号、各种符号、旗帜等
        /// 使用代理对范围匹配
        /// </summary>
        internal static readonly Regex RegexEmoji = new Regex(
            @"[\uD83C-\uDBFF][\uDC00-\uDFFF]|" +
            @"[\u2600-\u27BF]|" +
            @"[\uFE00-\uFE0F]",
            RegexOptions.Compiled);

        #endregion

        public JiebaSegmenter()
        {
            UserWordTagTab = new Dictionary<string, string>();
            CurrentWordDict = WordDict;
        }

        /// <summary>
        /// 使用指定配置创建分词器实例（带缓存）
        /// 相同配置会复用已加载的词典实例
        /// </summary>
        /// <param name="config">分词器配置，控制词典加载模式</param>
        public JiebaSegmenter(JiebaConfig config)
        {
            UserWordTagTab = new Dictionary<string, string>();
            CurrentWordDict = WordDictionary.GetOrCreate(config);
        }

        /// <summary>
        /// 内部构造函数，用于异步工厂方法和Tokenizer
        /// 直接传入已异步加载完成的词典实例
        /// </summary>
        /// <param name="wordDict">已加载完成的词典实例</param>
        internal JiebaSegmenter(WordDictionary wordDict)
        {
            UserWordTagTab = new Dictionary<string, string>();
            CurrentWordDict = wordDict;
        }

        /// <summary>
        /// 异步创建分词器实例（全量加载）
        /// 使用await using加速大词典文件读取
        /// </summary>
        public static async Task<JiebaSegmenter> CreateAsync()
        {
            var dict = await WordDictionary.CreateAsync().ConfigureAwait(false);
            return new JiebaSegmenter(dict);
        }

        /// <summary>
        /// 异步创建分词器实例（按配置加载）
        /// 使用await using加速大词典文件读取
        /// </summary>
        /// <param name="config">分词器配置</param>
        public static async Task<JiebaSegmenter> CreateAsync(JiebaConfig config)
        {
            var dict = await WordDictionary.CreateAsync(config).ConfigureAwait(false);
            return new JiebaSegmenter(dict);
        }

        /// <summary>
        /// The main function that segments an entire sentence that contains 
        /// Chinese characters into seperated words.
        /// </summary>
        /// <param name="text">The string to be segmented.</param>
        /// <param name="cutAll">Specify segmentation pattern. True for full pattern, False for accurate pattern.</param>
        /// <param name="hmm">Whether to use the Hidden Markov Model.</param>
        /// <returns></returns>
        public IEnumerable<string> Cut(string text, bool cutAll = false, bool hmm = true)
        {
            // 全模式不进行日期时间识别
            if (cutAll)
            {
                var reHan = RegexChineseCutAll;
                var reSkip = RegexSkipCutAll;
                return CutIt(text, CutAll, reHan, reSkip, true);
            }

            // 精确模式：先识别日期时间，再进行分词
            return CutWithDateTimeRecognition(text, hmm);
        }

        /// <summary>
        /// 带日期时间识别的分词方法（仅用于精确模式和搜索引擎模式）
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果</returns>
        private IEnumerable<string> CutWithDateTimeRecognition(string text, bool hmm)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Enumerable.Empty<string>();
            }

            // 识别日期时间实体
            var timeEntities = TimeRecognizer.Value.Recognize(text);

            if (timeEntities.Count == 0)
            {
                // 没有日期时间实体，使用普通分词
                var reHan = RegexChineseDefault;
                var reSkip = RegexSkipDefault;
                var cutMethod = hmm ? CutDag : (Func<string, IEnumerable<string>>)CutDagWithoutHmm;
                return CutIt(text, cutMethod, reHan, reSkip, false);
            }

            // 有日期时间实体，需要保护这些区域不被分词
            return CutWithProtectedRegions(text, timeEntities, hmm);
        }

        /// <summary>
        /// 对文本进行分词，保护指定区域不被分词
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="protectedRegions">受保护区域（日期时间实体）</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果</returns>
        private IEnumerable<string> CutWithProtectedRegions(string text, List<TimeEntity> protectedRegions, bool hmm)
        {
            var result = new List<string>();
            var reHan = RegexChineseDefault;
            var reSkip = RegexSkipDefault;
            var cutMethod = hmm ? CutDag : (Func<string, IEnumerable<string>>)CutDagWithoutHmm;

            var lastEnd = 0;

            foreach (var entity in protectedRegions)
            {
                // 先处理受保护区域之前的文本
                if (entity.Start > lastEnd)
                {
                    var beforeText = text.Substring(lastEnd, entity.Start - lastEnd);
                    if (!string.IsNullOrEmpty(beforeText))
                    {
                        result.AddRange(CutIt(beforeText, cutMethod, reHan, reSkip, false));
                    }
                }

                // 添加日期时间实体作为整体
                result.Add(entity.Text);
                lastEnd = entity.End;
            }

            // 处理最后一个受保护区域之后的文本
            if (lastEnd < text.Length)
            {
                var afterText = text.Substring(lastEnd);
                if (!string.IsNullOrEmpty(afterText))
                {
                    result.AddRange(CutIt(afterText, cutMethod, reHan, reSkip, false));
                }
            }

            return result;
        }

        /// <summary>
        /// jieba.lcut的等价方法，直接返回List&lt;string&gt;
        /// 精确模式分词，返回列表
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="cutAll">是否全模式</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果列表</returns>
        public List<string> Lcut(string text, bool cutAll = false, bool hmm = true)
        {
            return Cut(text, cutAll, hmm).ToList();
        }

        /// <summary>
        /// jieba.lcut_for_search的等价方法，直接返回List&lt;string&gt;
        /// 搜索引擎模式分词，返回列表
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果列表</returns>
        public List<string> LcutForSearch(string text, bool hmm = true)
        {
            return CutForSearch(text, hmm).ToList();
        }
        
        public IEnumerable<IEnumerable<string>> CutInParallel(IEnumerable<string> texts, bool cutAll = false, bool hmm = true)
        {
            // 使用Cut方法以支持日期时间识别
            return texts.AsParallel().AsOrdered().Select(text => Cut(text, cutAll, hmm));
        }
        
        public IEnumerable<string> CutInParallel(string text, bool cutAll = false, bool hmm = true)
        {
            var lines = text.SplitLines();
            return CutInParallel(lines, cutAll, hmm).SelectMany(words => words);
        }

        public IEnumerable<string> CutForSearch(string text, bool hmm = true)
        {
            var result = new List<string>();

            // 搜索引擎模式也进行日期时间识别
            // 先识别日期时间实体，避免提取日期时间实体的子词
            var timeEntities = TimeRecognizer.Value.Recognize(text);
            var words = Cut(text, hmm: hmm);
            
            foreach (var w in words)
            {
                // 检查当前词是否是日期时间实体
                var isTimeEntity = timeEntities.Any(e => e.Text == w);
                
                // 只有非日期时间实体才提取子词
                if (!isTimeEntity && w.Length > 2)
                {
                    // 使用Span优化，避免重复Substring分配
                    var wSpan = w.AsSpan();
                    for (var i = 0; i < w.Length - 1; i++)
                    {
                        var gram2 = wSpan.Slice(i, 2);
                        if (CurrentWordDict.ContainsWord(gram2))
                        {
                            result.Add(gram2.ToString());
                        }
                    }
                }

                if (!isTimeEntity && w.Length > 3)
                {
                    var wSpan = w.AsSpan();
                    for (var i = 0; i < w.Length - 2; i++)
                    {
                        var gram3 = wSpan.Slice(i, 3);
                        if (CurrentWordDict.ContainsWord(gram3))
                        {
                            result.Add(gram3.ToString());
                        }
                    }
                }

                result.Add(w);
            }

            return result;
        }
        
        public IEnumerable<IEnumerable<string>> CutForSearchInParallel(IEnumerable<string> texts, bool hmm = true)
        {
            return texts.AsParallel().AsOrdered().Select(line => CutForSearch(line, hmm));
        }
        
        public IEnumerable<string> CutForSearchInParallel(string text, bool hmm = true)
        {
            var lines = text.SplitLines();
            return CutForSearchInParallel(lines, hmm).SelectMany(words => words);
        }

        public IEnumerable<Token> Tokenize(string text, TokenizerMode mode = TokenizerMode.Default, bool hmm = true)
        {
            var result = new List<Token>();

            var start = 0;
            if (mode == TokenizerMode.Default)
            {
                foreach (var w in Cut(text, hmm: hmm))
                {
                    var width = w.Length;
                    result.Add(new Token(w, start, start + width));
                    start += width;
                }
            }
            else
            {
                foreach (var w in Cut(text, hmm: hmm))
                {
                    var width = w.Length;
                    if (width > 2)
                    {
                        var wSpan = w.AsSpan();
                        for (var i = 0; i < width - 1; i++)
                        {
                            var gram2 = wSpan.Slice(i, 2);
                            if (CurrentWordDict.ContainsWord(gram2))
                            {
                                result.Add(new Token(gram2.ToString(), start + i, start + i + 2));
                            }
                        }
                    }
                    if (width > 3)
                    {
                        var wSpan = w.AsSpan();
                        for (var i = 0; i < width - 2; i++)
                        {
                            var gram3 = wSpan.Slice(i, 3);
                            if (CurrentWordDict.ContainsWord(gram3))
                            {
                                result.Add(new Token(gram3.ToString(), start + i, start + i + 3));
                            }
                        }
                    }

                    result.Add(new Token(w, start, start + width));
                    start += width;
                }
            }

            return result;
        }

        #region Internal Cut Methods

        /// <summary>
        /// 构建有向无环图(DAG)，使用Span优化字符串切片
        /// 使用CurrentWordDict而非全局WordDict，支持独立词典
        /// </summary>
        internal IDictionary<int, List<int>> GetDag(string sentence)
        {
            var dag = new Dictionary<int, List<int>>();
            var sentenceSpan = sentence.AsSpan();
            var N = sentence.Length;

            for (var k = 0; k < N; k++)
            {
                var templist = new List<int>();
                var i = k;
                
                // 使用Span进行切片，避免Substring分配
                var frag = sentenceSpan.Slice(k, 1);
                while (i < N && CurrentWordDict.ContainsPrefix(frag))
                {
                    var trieValue = CurrentWordDict.GetTrieValue(frag);
                    if (trieValue > 0)
                    {
                        templist.Add(i);
                    }

                    i++;
                    if (i < N)
                    {
                        // 使用Span切片，长度为 (i + 1 - k)
                        frag = sentenceSpan.Slice(k, i + 1 - k);
                    }
                }
                
                if (templist.Count == 0)
                {
                    templist.Add(k);
                }
                dag[k] = templist;
            }

            return dag;
        }

        /// <summary>
        /// 计算最大概率路径，使用Span优化字符串切片
        /// 使用CurrentWordDict而非全局WordDict，支持独立词典
        /// </summary>
        internal IDictionary<int, Pair<int>> Calc(string sentence, IDictionary<int, List<int>> dag)
        {
            var n = sentence.Length;
            var route = new Dictionary<int, Pair<int>>();
            route[n] = new Pair<int>(0, 0.0);

            var sentenceSpan = sentence.AsSpan();
            var logtotal = Math.Log(CurrentWordDict.Total);
            
            for (var i = n - 1; i > -1; i--)
            {
                var candidate = new Pair<int>(-1, double.MinValue);
                foreach (int x in dag[i])
                {
                    // 使用Span切片获取子串
                    var subSpan = sentenceSpan.Slice(i, x + 1 - i);
                    var freq = Math.Log(CurrentWordDict.GetFreqOrDefault(subSpan)) - logtotal + route[x + 1].Freq;
                    if (candidate.Freq < freq)
                    {
                        candidate.Freq = freq;
                        candidate.Key = x;
                    }
                }
                route[i] = candidate;
            }
            return route;
        }

        internal IEnumerable<string> CutAll(string sentence)
        {
            var dag = GetDag(sentence);

            var words = new List<string>();
            var lastPos = -1;

            foreach (var pair in dag)
            {
                var k = pair.Key;
                var nexts = pair.Value;
                if (nexts.Count == 1 && k > lastPos)
                {
                    words.Add(sentence.Substring(k, nexts[0] + 1 - k));
                    lastPos = nexts[0];
                }
                else
                {
                    foreach (var j in nexts)
                    {
                        if (j > k)
                        {
                            words.Add(sentence.Substring(k, j + 1 - k));
                            lastPos = j;
                        }
                    }
                }
            }

            return words;
        }

        internal IEnumerable<string> CutDag(string sentence)
        {
            var dag = GetDag(sentence);
            var route = Calc(sentence, dag);

            var tokens = new List<string>();

            var x = 0;
            var n = sentence.Length;
            var buf = string.Empty;
            while (x < n)
            {
                var y = route[x].Key + 1;
                var w = sentence.Substring(x, y - x);
                if (y - x == 1)
                {
                    buf += w;
                }
                else
                {
                    if (buf.Length > 0)
                    {
                        AddBufferToWordList(tokens, buf);
                        buf = string.Empty;
                    }
                    tokens.Add(w);
                }
                x = y;
            }

            if (buf.Length > 0)
            {
                AddBufferToWordList(tokens, buf);
            }

            return tokens;
        }

        internal IEnumerable<string> CutDagWithoutHmm(string sentence)
        {
            var dag = GetDag(sentence);
            var route = Calc(sentence, dag);

            var words = new List<string>();

            var x = 0;
            string buf = string.Empty;
            var N = sentence.Length;

            var y = -1;
            while (x < N)
            {
                y = route[x].Key + 1;
                var l_word = sentence.Substring(x, y - x);
                if (RegexEnglishChars.IsMatch(l_word) && l_word.Length == 1)
                {
                    buf += l_word;
                    x = y;
                }
                else
                {
                    if (buf.Length > 0)
                    {
                        words.Add(buf);
                        buf = string.Empty;
                    }
                    words.Add(l_word);
                    x = y;
                }
            }

            if (buf.Length > 0)
            {
                words.Add(buf);
            }

            return words;
        }

        internal IEnumerable<string> CutIt(string text, Func<string, IEnumerable<string>> cutMethod,
                                           Regex reHan, Regex reSkip, bool cutAll)
        {
            var result = new List<string>();
            var blocks = reHan.Split(text);
            foreach (var blk in blocks)
            {
                if (string.IsNullOrEmpty(blk))
                {
                    continue;
                }

                if (reHan.IsMatch(blk))
                {
                    foreach (var word in cutMethod(blk))
                    {
                        result.Add(word);
                    }
                }
                else
                {
                    var tmp = reSkip.Split(blk);
                    foreach (var x in tmp)
                    {
                        if (reSkip.IsMatch(x))
                        {
                            result.Add(x);
                        }
                        else if (!cutAll)
                        {
                            // 优先使用emoji词典匹配复杂emoji
                            // 如果匹配到emoji词典中的词，直接作为一个整体
                            // 否则使用Grapheme Cluster分割
                            var i = 0;
                            while (i < x.Length)
                            {
                                // 尝试匹配emoji词典
                                var emojiLen = CurrentWordDict.MatchEmoji(x, i);
                                if (emojiLen > 0)
                                {
                                    // 匹配到emoji，直接添加
                                    result.Add(x.Substring(i, emojiLen));
                                    i += emojiLen;
                                }
                                else
                                {
                                    // 没有匹配到emoji，使用Grapheme Cluster分割
                                    // 找到下一个可能的emoji开始位置
                                    var graphemes = RuneHelper.SplitToGraphemes(x.Substring(i));
                                    if (graphemes.Count > 0)
                                    {
                                        result.Add(graphemes[0]);
                                        i += graphemes[0].Length;
                                    }
                                    else
                                    {
                                        // 兜底：单字符
                                        result.Add(x[i].ToString());
                                        i++;
                                    }
                                }
                            }
                        }
                        else
                        {
                            result.Add(x);
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region Extend Main Dict

        /// <summary>
        /// Loads user dictionaries.
        /// </summary>
        /// <param name="userDictFile"></param>
        public void LoadUserDict(string userDictFile)
        {
            var dictFullPath = Path.GetFullPath(userDictFile);
            Debug.WriteLine("Initializing user dictionary: " + userDictFile);

            lock (locker)
            {
                if (LoadedPath.Contains(dictFullPath))
                    return;

                try
                {
                    var startTime = DateTime.Now.Millisecond;

                    var lines = File.ReadAllLines(dictFullPath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        var tokens = RegexUserDict.Match(line.Trim()).Groups;
                        var word = tokens["word"].Value.Trim();
                        var freq = tokens["freq"].Value.Trim();
                        var tag = tokens["tag"].Value.Trim();

                        var actualFreq = freq.Length > 0 ? int.Parse(freq) : 0;
                        AddWord(word, actualFreq, tag);
                    }

                    Debug.WriteLine("user dict '{0}' load finished, time elapsed {1} ms",
                        dictFullPath, DateTime.Now.Millisecond - startTime);
                }
                catch (IOException e)
                {
                    Debug.Fail(string.Format("'{0}' load failure, reason: {1}", dictFullPath, e.Message));
                }
                catch (FormatException fe)
                {
                    Debug.Fail(fe.Message);
                }
            }
        }

        public void AddWord(string word, int freq = 0, string tag = null)
        {
            if (freq <= 0)
            {
                freq = CurrentWordDict.SuggestFreq(word, Cut(word, hmm: false));
            }
            CurrentWordDict.AddWord(word, freq);

            // Add user word tag of POS
            if (!string.IsNullOrEmpty(tag))
            {
                UserWordTagTab[word] = tag;
            }
        }

        public void DeleteWord(string word)
        {
            CurrentWordDict.DeleteWord(word);
        }

        #endregion

        #region Private Helpers

        private void AddBufferToWordList(List<string> words, string buf)
        {
            if (buf.Length == 1)
            {
                words.Add(buf);
            }
            else
            {
                if (!CurrentWordDict.ContainsWord(buf))
                {
                    var tokens = FinalSeg.Cut(buf);
                    words.AddRange(tokens);
                }
                else
                {
                    words.AddRange(buf.Select(ch => ch.ToString()));
                }
            }
        }

        #endregion
    }

    public enum TokenizerMode
    {
        Default,
        Search
    }
}
