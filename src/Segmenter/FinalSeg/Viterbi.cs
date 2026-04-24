using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JiebaNet.Segmenter.Common;

namespace JiebaNet.Segmenter.FinalSeg
{
    public class Viterbi : IFinalSeg
    {
        private static readonly Lazy<Viterbi> Lazy = new Lazy<Viterbi>(() => new Viterbi());
        private static readonly char[] States = { 'B', 'M', 'E', 'S' };

        // 使用GB18030_2022的中文块正则，支持扩展B-I区的代理对字符
        private static readonly Regex RegexChinese = new Regex(@"(" + GB18030_2022.ChineseBlockPattern + @")", RegexOptions.Compiled);
        private static readonly Regex RegexSkip = new Regex(@"([a-zA-Z0-9]+(?:\.\d+)?%?)", RegexOptions.Compiled);

        private static IDictionary<char, IDictionary<char, double>> _emitProbs = null!;
        private static IDictionary<char, double> _startProbs = null!;
        private static IDictionary<char, IDictionary<char, double>> _transProbs = null!;
        private static IDictionary<char, char[]> _prevStatus = null!;

        private Viterbi()
        {
            LoadModel();
        }

        // TODO: synchronized
        public static Viterbi Instance
        {
            get { return Lazy.Value; }
        }

        public IEnumerable<string> Cut(string sentence)
        {
            var tokens = new List<string>();
            foreach (var blk in RegexChinese.Split(sentence))
            {
                if (RegexChinese.IsMatch(blk))
                {
                    tokens.AddRange(ViterbiCut(blk));
                }
                else
                {
                    var segments = RegexSkip.Split(blk).Where(seg => !string.IsNullOrEmpty(seg));
                    tokens.AddRange(segments);
                }
            }
            return tokens;
        }

        #region Private Helpers

        private void LoadModel()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            _prevStatus = new Dictionary<char, char[]>()
            {
                {'B', new []{'E', 'S'}},
                {'M', new []{'M', 'B'}},
                {'S', new []{'S', 'E'}},
                {'E', new []{'B', 'M'}}
            };

            _startProbs = new Dictionary<char, double>()
            {
                {'B', -0.26268660809250016},
                {'E', -3.14e+100},
                {'M', -3.14e+100},
                {'S', -1.4652633398537678}
            };

            _transProbs = JsonHelper.ParseCharCharDoubleDict(ConfigManager.ProbTransFile);

            _emitProbs = JsonHelper.ParseCharCharDoubleDict(ConfigManager.ProbEmitFile);

            stopWatch.Stop();
            Debug.WriteLine("model loading finished, time elapsed {0} ms.", stopWatch.ElapsedMilliseconds);
        }

        private IEnumerable<string> ViterbiCut(string sentence)
        {
            // 将字符串转换为"逻辑字符"列表，代理对作为一个逻辑字符
            var logicalChars = new List<string>();
            for (var i = 0; i < sentence.Length; i++)
            {
                if (char.IsHighSurrogate(sentence[i]) && i + 1 < sentence.Length && char.IsLowSurrogate(sentence[i + 1]))
                {
                    logicalChars.Add(sentence.Substring(i, 2));
                    i++;
                }
                else
                {
                    logicalChars.Add(sentence[i].ToString());
                }
            }

            var n = logicalChars.Count;
            if (n == 0)
                return Enumerable.Empty<string>();

            var v = new List<IDictionary<char, double>>();
            IDictionary<char, Node> path = new Dictionary<char, Node>();

            // Init weights and paths.
            v.Add(new Dictionary<char, double>());
            foreach (var state in States)
            {
                var emP = _emitProbs[state].GetDefault(logicalChars[0][0], Constants.MinProb);
                v[0][state] = _startProbs[state] + emP;
                path[state] = new Node(state, null);
            }

            // For each remaining logical char
            for (var i = 1; i < n; ++i)
            {
                IDictionary<char, double> vv = new Dictionary<char, double>();
                v.Add(vv);
                IDictionary<char, Node> newPath = new Dictionary<char, Node>();
                foreach (var y in States)
                {
                    var emp = _emitProbs[y].GetDefault(logicalChars[i][0], Constants.MinProb);

                    Pair<char> candidate = new Pair<char>('\0', double.MinValue);
                    foreach (var y0 in _prevStatus[y])
                    {
                        var tranp = _transProbs[y0].GetDefault(y, Constants.MinProb);
                        tranp = v[i - 1][y0] + tranp + emp;
                        if (candidate.Freq <= tranp)
                        {
                            candidate.Freq = tranp;
                            candidate.Key = y0;
                        }
                    }
                    vv[y] = candidate.Freq;
                    newPath[y] = new Node(y, path[candidate.Key]);
                }
                path = newPath;
            }

            var probE = v[n - 1]['E'];
            var probS = v[n - 1]['S'];
            var finalPath = probE < probS ? path['S'] : path['E'];

            var posList = new List<char>(n);
            while (finalPath != null)
            {
                posList.Add(finalPath.Value);
                finalPath = finalPath.Parent;
            }
            posList.Reverse();

            var tokens = new List<string>();
            int begin = 0, next = 0;
            for (var i = 0; i < n; i++)
            {
                var pos = posList[i];
                if (pos == 'B')
                    begin = i;
                else if (pos == 'E')
                {
                    var sb = new StringBuilder();
                    for (var j = begin; j <= i; j++)
                        sb.Append(logicalChars[j]);
                    tokens.Add(sb.ToString());
                    next = i + 1;
                }
                else if (pos == 'S')
                {
                    tokens.Add(logicalChars[i]);
                    next = i + 1;
                }
            }
            if (next < n)
            {
                var sb = new StringBuilder();
                for (var j = next; j < n; j++)
                    sb.Append(logicalChars[j]);
                tokens.Add(sb.ToString());
            }

            return tokens;
        }

        #endregion
    }
}