using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JiebaNet.Segmenter.Common;
using JiebaNet.Segmenter.FinalSeg;

namespace JiebaNet.Segmenter
{
    /// <summary>
    /// 自定义分词器类，等价于原版jieba的jieba.Tokenizer
    /// 每个Tokenizer实例拥有独立的词典，可用于同时使用不同词典
    /// jieba.dt为默认分词器实例，所有全局分词相关函数都是该分词器的映射
    /// </summary>
    public class Tokenizer
    {
        /// <summary>
        /// 当前分词器使用的词典实例
        /// </summary>
        internal WordDictionary CurrentWordDict { get; }

        /// <summary>
        /// 当前分词器使用的JiebaSegmenter实例
        /// </summary>
        private readonly JiebaSegmenter _segmenter;

        /// <summary>
        /// 用户自定义词性标签表
        /// </summary>
        internal IDictionary<string, string> UserWordTagTab { get; set; }

        /// <summary>
        /// 使用默认词典创建分词器
        /// 等价于原版jieba的Tokenizer(dictionary=DEFAULT_DICT)
        /// </summary>
        public Tokenizer() : this(new JiebaConfig())
        {
        }

        /// <summary>
        /// 使用指定配置创建分词器
        /// 等价于原版jieba的Tokenizer(dictionary=custom_dict)
        /// 每个实例拥有独立的词典，互不影响
        /// </summary>
        /// <param name="config">分词器配置</param>
        public Tokenizer(JiebaConfig config)
        {
            UserWordTagTab = new Dictionary<string, string>();
            CurrentWordDict = new WordDictionary(config);
            _segmenter = new JiebaSegmenter(config);
        }

        /// <summary>
        /// 内部构造函数，用于异步工厂方法
        /// 直接传入已异步加载完成的词典实例和分词器
        /// </summary>
        /// <param name="wordDict">已加载完成的词典实例</param>
        /// <param name="segmenter">已创建的分词器实例</param>
        private Tokenizer(WordDictionary wordDict, JiebaSegmenter segmenter)
        {
            UserWordTagTab = new Dictionary<string, string>();
            CurrentWordDict = wordDict;
            _segmenter = segmenter;
        }

        /// <summary>
        /// 异步创建分词器实例（默认配置）
        /// 使用await using加速大词典文件读取
        /// </summary>
        public static async Task<Tokenizer> CreateAsync()
        {
            var dict = await WordDictionary.CreateAsync().ConfigureAwait(false);
            var segmenter = new JiebaSegmenter(dict);
            return new Tokenizer(dict, segmenter);
        }

        /// <summary>
        /// 异步创建分词器实例（按配置加载）
        /// 使用await using加速大词典文件读取
        /// </summary>
        /// <param name="config">分词器配置</param>
        public static async Task<Tokenizer> CreateAsync(JiebaConfig config)
        {
            var dict = await WordDictionary.CreateAsync(config).ConfigureAwait(false);
            var segmenter = new JiebaSegmenter(dict);
            return new Tokenizer(dict, segmenter);
        }

        #region 分词方法（映射到JiebaSegmenter）

        /// <summary>
        /// 精确模式分词，返回IEnumerable
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="cutAll">是否全模式</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果</returns>
        public IEnumerable<string> Cut(string text, bool cutAll = false, bool hmm = true)
        {
            return _segmenter.Cut(text, cutAll, hmm);
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
            return _segmenter.Lcut(text, cutAll, hmm);
        }

        /// <summary>
        /// 搜索引擎模式分词，返回IEnumerable
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果</returns>
        public IEnumerable<string> CutForSearch(string text, bool hmm = true)
        {
            return _segmenter.CutForSearch(text, hmm);
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
            return _segmenter.LcutForSearch(text, hmm);
        }

        /// <summary>
        /// 并行分词
        /// </summary>
        /// <param name="texts">待分词文本集合</param>
        /// <param name="cutAll">是否全模式</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果集合</returns>
        public IEnumerable<IEnumerable<string>> CutInParallel(IEnumerable<string> texts, bool cutAll = false, bool hmm = true)
        {
            return _segmenter.CutInParallel(texts, cutAll, hmm);
        }

        /// <summary>
        /// 并行分词（按行分割）
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="cutAll">是否全模式</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果</returns>
        public IEnumerable<string> CutInParallel(string text, bool cutAll = false, bool hmm = true)
        {
            return _segmenter.CutInParallel(text, cutAll, hmm);
        }

        /// <summary>
        /// 并行搜索引擎模式分词
        /// </summary>
        /// <param name="texts">待分词文本集合</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果集合</returns>
        public IEnumerable<IEnumerable<string>> CutForSearchInParallel(IEnumerable<string> texts, bool hmm = true)
        {
            return _segmenter.CutForSearchInParallel(texts, hmm);
        }

        /// <summary>
        /// 并行搜索引擎模式分词（按行分割）
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果</returns>
        public IEnumerable<string> CutForSearchInParallel(string text, bool hmm = true)
        {
            return _segmenter.CutForSearchInParallel(text, hmm);
        }

        /// <summary>
        /// 分词并返回Token位置信息
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="mode">分词模式</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>Token列表</returns>
        public IEnumerable<Token> Tokenize(string text, TokenizerMode mode = TokenizerMode.Default, bool hmm = true)
        {
            return _segmenter.Tokenize(text, mode, hmm);
        }

        #endregion

        #region 词典操作

        /// <summary>
        /// 加载用户自定义词典
        /// </summary>
        /// <param name="userDictFile">词典文件路径</param>
        public void LoadUserDict(string userDictFile)
        {
            _segmenter.LoadUserDict(userDictFile);
        }

        /// <summary>
        /// 添加自定义词
        /// </summary>
        /// <param name="word">词</param>
        /// <param name="freq">词频（0表示自动计算）</param>
        /// <param name="tag">词性标签</param>
        public void AddWord(string word, int freq = 0, string tag = null)
        {
            _segmenter.AddWord(word, freq, tag);
        }

        /// <summary>
        /// 删除词
        /// </summary>
        /// <param name="word">要删除的词</param>
        public void DeleteWord(string word)
        {
            _segmenter.DeleteWord(word);
        }

        #endregion
    }

    /// <summary>
    /// jieba全局静态类，等价于原版jieba的模块级接口
    /// jieba.dt为默认分词器实例，所有全局分词相关函数都是该分词器的映射
    /// </summary>
    public static class Jieba
    {
        /// <summary>
        /// 默认分词器实例，等价于原版jieba的jieba.dt
        /// 所有全局分词相关函数都是该分词器的映射
        /// 使用Lazy延迟初始化，首次访问时加载词典
        /// </summary>
        public static readonly Lazy<Tokenizer> Dt = new Lazy<Tokenizer>(() => new Tokenizer());

        /// <summary>
        /// 精确模式分词，返回IEnumerable
        /// 映射到jieba.dt.Cut
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="cutAll">是否全模式</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果</returns>
        public static IEnumerable<string> Cut(string text, bool cutAll = false, bool hmm = true)
        {
            return Dt.Value.Cut(text, cutAll, hmm);
        }

        /// <summary>
        /// jieba.lcut的等价方法，直接返回List&lt;string&gt;
        /// 映射到jieba.dt.Lcut
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="cutAll">是否全模式</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果列表</returns>
        public static List<string> Lcut(string text, bool cutAll = false, bool hmm = true)
        {
            return Dt.Value.Lcut(text, cutAll, hmm);
        }

        /// <summary>
        /// 搜索引擎模式分词，返回IEnumerable
        /// 映射到jieba.dt.CutForSearch
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果</returns>
        public static IEnumerable<string> CutForSearch(string text, bool hmm = true)
        {
            return Dt.Value.CutForSearch(text, hmm);
        }

        /// <summary>
        /// jieba.lcut_for_search的等价方法，直接返回List&lt;string&gt;
        /// 映射到jieba.dt.LcutForSearch
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果列表</returns>
        public static List<string> LcutForSearch(string text, bool hmm = true)
        {
            return Dt.Value.LcutForSearch(text, hmm);
        }

        /// <summary>
        /// 并行分词
        /// 映射到jieba.dt.CutInParallel
        /// </summary>
        /// <param name="texts">待分词文本集合</param>
        /// <param name="cutAll">是否全模式</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果集合</returns>
        public static IEnumerable<IEnumerable<string>> CutInParallel(IEnumerable<string> texts, bool cutAll = false, bool hmm = true)
        {
            return Dt.Value.CutInParallel(texts, cutAll, hmm);
        }

        /// <summary>
        /// 并行分词（按行分割）
        /// 映射到jieba.dt.CutInParallel
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="cutAll">是否全模式</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果</returns>
        public static IEnumerable<string> CutInParallel(string text, bool cutAll = false, bool hmm = true)
        {
            return Dt.Value.CutInParallel(text, cutAll, hmm);
        }

        /// <summary>
        /// 并行搜索引擎模式分词
        /// 映射到jieba.dt.CutForSearchInParallel
        /// </summary>
        /// <param name="texts">待分词文本集合</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果集合</returns>
        public static IEnumerable<IEnumerable<string>> CutForSearchInParallel(IEnumerable<string> texts, bool hmm = true)
        {
            return Dt.Value.CutForSearchInParallel(texts, hmm);
        }

        /// <summary>
        /// 并行搜索引擎模式分词（按行分割）
        /// 映射到jieba.dt.CutForSearchInParallel
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>分词结果</returns>
        public static IEnumerable<string> CutForSearchInParallel(string text, bool hmm = true)
        {
            return Dt.Value.CutForSearchInParallel(text, hmm);
        }

        /// <summary>
        /// 分词并返回Token位置信息
        /// 映射到jieba.dt.Tokenize
        /// </summary>
        /// <param name="text">待分词文本</param>
        /// <param name="mode">分词模式</param>
        /// <param name="hmm">是否使用HMM</param>
        /// <returns>Token列表</returns>
        public static IEnumerable<Token> Tokenize(string text, TokenizerMode mode = TokenizerMode.Default, bool hmm = true)
        {
            return Dt.Value.Tokenize(text, mode, hmm);
        }

        /// <summary>
        /// 加载用户自定义词典
        /// 映射到jieba.dt.LoadUserDict
        /// </summary>
        /// <param name="userDictFile">词典文件路径</param>
        public static void LoadUserDict(string userDictFile)
        {
            Dt.Value.LoadUserDict(userDictFile);
        }

        /// <summary>
        /// 添加自定义词
        /// 映射到jieba.dt.AddWord
        /// </summary>
        /// <param name="word">词</param>
        /// <param name="freq">词频（0表示自动计算）</param>
        /// <param name="tag">词性标签</param>
        public static void AddWord(string word, int freq = 0, string tag = null)
        {
            Dt.Value.AddWord(word, freq, tag);
        }

        /// <summary>
        /// 删除词
        /// 映射到jieba.dt.DeleteWord
        /// </summary>
        /// <param name="word">要删除的词</param>
        public static void DeleteWord(string word)
        {
            Dt.Value.DeleteWord(word);
        }
    }
}
