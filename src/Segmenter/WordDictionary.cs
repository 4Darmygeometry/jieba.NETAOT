using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JiebaNet.Segmenter.Common;

namespace JiebaNet.Segmenter
{
    public class WordDictionary
    {
        private static readonly Lazy<WordDictionary> lazy = new Lazy<WordDictionary>(() => new WordDictionary());
        private static readonly string MainDict = ConfigManager.MainDictFile;
        private static readonly string EmojiDict = ConfigManager.EmojiDictFile;
        private static readonly string MainDictHant = ConfigManager.MainDictHantFile;

        internal IDictionary<string, int> Trie = new Dictionary<string, int>();

        /// <summary>
        /// total occurrence of all words.
        /// </summary>
        public double Total { get; set; }

        private WordDictionary()
        {
            LoadDict();

            Debug.WriteLine("{0} words (and their prefixes)", Trie.Count);
            Debug.WriteLine("total freq: {0}", Total);
        }

        public static WordDictionary Instance
        {
            get { return lazy.Value; }
        }

        private void LoadDict()
        {
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                // 加载主词典（简体中文）
                LoadDictFile(MainDict, "主词典(简体)");

                // 加载繁体中文词典
                LoadDictFile(MainDictHant, "繁体中文词典");

                // 加载emoji词典
                LoadEmojiDictFile(EmojiDict);

                stopWatch.Stop();
                Debug.WriteLine("词典加载完成，耗时 {0} ms", stopWatch.ElapsedMilliseconds);
            }
            catch (IOException e)
            {
                Debug.Fail(string.Format("词典加载失败，原因: {0}", e.Message));
            }
            catch (FormatException fe)
            {
                Debug.Fail(fe.Message);
            }
        }

        /// <summary>
        /// 加载词典文件
        /// </summary>
        /// <param name="dictFile">词典文件路径</param>
        /// <param name="dictName">词典名称（用于日志）</param>
        private void LoadDictFile(string dictFile, string dictName)
        {
            if (!File.Exists(dictFile))
            {
                Debug.WriteLine("词典文件不存在: {0}", dictFile);
                return;
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            using (var sr = new StreamReader(dictFile, Encoding.UTF8))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    var tokens = line.Split(' ');
                    if (tokens.Length < 2)
                    {
                        continue;
                    }

                    var word = tokens[0];
                    var freq = int.Parse(tokens[1]);

                    Trie[word] = freq;
                    Total += freq;

                    foreach (var ch in Enumerable.Range(0, word.Length))
                    {
                        var wfrag = word.Sub(0, ch + 1);
                        if (!Trie.ContainsKey(wfrag))
                        {
                            Trie[wfrag] = 0;
                        }
                    }
                }
            }

            stopWatch.Stop();
            Debug.WriteLine("{0}加载完成，耗时 {1} ms", dictName, stopWatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// 加载emoji词典文件
        /// emoji词典格式：emoji 频率 词性（每行一个emoji）
        /// 使用Rune正确处理多码点emoji
        /// </summary>
        /// <param name="dictFile">emoji词典文件路径</param>
        private void LoadEmojiDictFile(string dictFile)
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
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    // 解析格式：emoji 频率 词性
                    var tokens = line.Split(' ');
                    var emoji = tokens[0];
                    var freq = tokens.Length >= 2 ? int.Parse(tokens[1]) : 10000;

                    Trie[emoji] = freq;
                    Total += freq;

                    // emoji不需要添加前缀到trie树，因为emoji是独立的字符序列
                    // 如果添加前缀会导致代理对被错误拆分
                    count++;
                }
            }

            stopWatch.Stop();
            Debug.WriteLine("emoji词典加载完成，共 {0} 个emoji，耗时 {1} ms", count, stopWatch.ElapsedMilliseconds);
        }

        public bool ContainsWord(string word)
        {
            return Trie.ContainsKey(word) && Trie[word] > 0;
        }

        public int GetFreqOrDefault(string key)
        {
            if (ContainsWord(key))
                return Trie[key];
            else
                return 1;
        }

        public void AddWord(string word, int freq, string tag = null)
        {
            if (ContainsWord(word))
            {
                Total -= Trie[word];
            }

            Trie[word] = freq;
            Total += freq;
            for (var i = 0; i < word.Length; i++)
            {
                var wfrag = word.Substring(0, i + 1);
                if (!Trie.ContainsKey(wfrag))
                {
                    Trie[wfrag] = 0;
                }
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
    }
}