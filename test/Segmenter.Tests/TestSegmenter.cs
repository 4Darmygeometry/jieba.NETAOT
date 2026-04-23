using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JiebaNet.Segmenter.Common;
using JiebaNet.Segmenter.PosSeg;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace JiebaNet.Segmenter.Tests
{
    [TestFixture]
    public class TestSegmenter
    {
        private string[] GetTestSentences()
        {
            return File.ReadAllLines(TestHelper.GetCaseFilePath("jieba_test.txt"));
        }

        [TestCase]
        public void TestGetDag()
        {
            var seg = new JiebaSegmenter();
            var dag = seg.GetDag("语言学家参加学术会议");
            foreach (var key in dag.Keys.ToList().OrderBy(k => k))
            {
                Console.Write("{0}: ", key);
                foreach (var i in dag[key])
                {
                    Console.Write("{0} ", i);
                }
                Console.WriteLine();
            }
        }

        [TestCase]
        public void TestCalc()
        {
            var s = "语言学家参加学术会议";
            var seg = new JiebaSegmenter();
            var dag = seg.GetDag(s);
            var route = seg.Calc(s, dag);
            foreach (var key in route.Keys.ToList().OrderBy(k => k))
            {
                Console.Write("{0}: ", key);
                var pair = route[key];
                Console.WriteLine("({0}, {1})", pair.Freq, pair.Key);
            }
        }

        [TestCase]
        public void TestCutDag()
        {
            var s = "语言学家去参加了那个学术会议";
            var seg = new JiebaSegmenter();
            var words = seg.CutDag(s);
            foreach (var w in words)
            {
                Console.WriteLine(w);
            }
        }

        [TestCase]
        public void TestCutDagWithoutHmm()
        {
            var s = "语言学家去参加了那个学术会议";
            var seg = new JiebaSegmenter();
            var words = seg.CutDagWithoutHmm(s);
            foreach (var w in words)
            {
                Console.WriteLine(w);
            }
        }

        #region Jieba Python Test Cases

        [TestCase]
        public void TestCut()
        {
            TestCutFunction((new JiebaSegmenter()).Cut, false, true, TestHelper.GetCaseFilePath("accurate_hmm.txt"));
        }

        [TestCase]
        public void TestCutAll()
        {
            TestCutFunction((new JiebaSegmenter()).Cut, true, false, TestHelper.GetCaseFilePath("cut_all.txt"));
        }

        [TestCase]
        public void TestCutWithoutHmm()
        {
            TestCutFunction((new JiebaSegmenter()).Cut, false, false, TestHelper.GetCaseFilePath("accurate_no_hmm.txt"));
        }

        [TestCase]
        public void TestCutForSearch()
        {
            TestCutSearchFunction((new JiebaSegmenter()).CutForSearch, true, TestHelper.GetCaseFilePath("cut_search_hmm.txt"));
        }

        [TestCase]
        public void TestCutForSearchWithoutHmm()
        {
            TestCutSearchFunction((new JiebaSegmenter()).CutForSearch, false, TestHelper.GetCaseFilePath("cut_search_no_hmm.txt"));
        }

        #endregion

        [TestCase]
        public void TestTokenize()
        {
            var seg = new JiebaSegmenter();
            foreach (var token in seg.Tokenize("小明最近在学习机器学习、自然语言处理、云计算和大数据"))
            {
                Console.WriteLine(token);
            }
            Console.WriteLine();

            foreach (var token in seg.Tokenize("小明最近在学习机器学习、自然语言处理、云计算和大数据", TokenizerMode.Search))
            {
                Console.WriteLine(token);
            }
        }

        [TestCase]
        public void TestTokenizeWithSpace()
        {
            var seg = new JiebaSegmenter();

            var s = "永和服装饰品有限公司";
            var tokens = seg.Tokenize(s).ToList();
            Assert.That(tokens.Count, Is.EqualTo(4));
            Assert.That(tokens.Last().EndIndex, Is.EqualTo(s.Length));

            s = "永和服装饰品 有限公司";
            tokens = seg.Tokenize(s).ToList();
            Assert.That(tokens.Count, Is.EqualTo(5));
            Assert.That(tokens.Last().EndIndex, Is.EqualTo(s.Length));
        }

        private static void TestCutThenPrint(JiebaSegmenter segmenter, string s)
        {
            Console.WriteLine(string.Join("/ ", segmenter.Cut(s)));
        }

        [TestCase]
        public void TestAddWord()
        {
            var seg = new JiebaSegmenter();
            var s = "小明最近在学习机器学习和自然语言处理";

            var segments = seg.Cut(s);
            Assert.That(segments, Contains.Item("机器"));
            Assert.That(segments, Contains.Item("学习"));

            seg.AddWord("机器学习");
            segments = seg.Cut(s);
            Assert.That(segments, Contains.Item("机器学习"));
            Assert.That(segments, Is.Not.Contains("机器"));

            // reset dict otherwise other test cases would be affected.
            seg.DeleteWord("机器学习");
        }

        [TestCase]
        public void TestDeleteWord()
        {
            var seg = new JiebaSegmenter();
            var s = "小明最近在学习机器学习和自然语言处理";

            var segments = seg.Cut(s);
            Assert.That(segments, Contains.Item("机器"));
            Assert.That(segments, Is.Not.Contains("机器学习"));

            seg.AddWord("机器学习");
            segments = seg.Cut(s);
            Assert.That(segments, Contains.Item("机器学习"));
            Assert.That(segments, Is.Not.Contains("机器"));

            seg.DeleteWord("机器学习");
            segments = seg.Cut(s);
            Assert.That(segments, Contains.Item("机器"));
            Assert.That(segments, Is.Not.Contains("机器学习"));
        }

        [TestCase]
        public void TestCutSpecialWords()
        {
            var seg = new JiebaSegmenter();
            seg.AddWord(".NET");
            seg.AddWord("U.S.A.");
            
            var s = ".NET平台是微软推出的, U.S.A.是美国的简写";

            var segments = seg.Cut(s);
            foreach (var segment in segments)
            {
                Console.WriteLine(segment);
            }

            seg.LoadUserDict(TestHelper.GetResourceFilePath("user_dict.txt"));
            s = "Steve Jobs重新定义了手机";
            segments = seg.Cut(s);
            foreach (var segment in segments)
            {
                Console.WriteLine(segment);
            }

            s = "我们所熟悉的一个版本是Mac OS X 10.11 EI Capitan，在2015年推出。";
            segments = seg.Cut(s);
            foreach (var segment in segments)
            {
                Console.WriteLine(segment);
            }
        }

        [TestCase]
        public void TestCutAllSpecialWords()
        {
            var seg = new JiebaSegmenter();
            seg.AddWord(".NET");
            seg.AddWord("U.S.A.");
            seg.AddWord("Steve Jobs");

            var s = ".NET平台是微软推出的, U.S.A.是美国的简写";
            var segments = seg.Cut(s).ToList();
            Assert.That(segments,   Contains.Item(".NET"));
            Assert.That(segments,   Contains.Item("U.S.A."));

            s = "Steve Jobs重新定义了手机";
            segments = seg.Cut(s).ToList();
            Assert.That(segments,   Has.No.Member("Steve Jobs"));
        }

        [TestCase]
        public void TestCutTraditionalChinese()
        {
            var seg = new JiebaSegmenter();
            TestCutThenPrint(seg, "小明最近在學習機器學習和自然語言處理");
        }

        [TestCase]
        public void TestUserDict()
        {
            var dict = TestHelper.GetResourceFilePath("user_dict.txt");
            var seg = new JiebaSegmenter();

            TestCutThenPrint(seg, "小明最近在学习机器学习、自然语言处理、云计算和大数据");
            seg.LoadUserDict(dict);
            TestCutThenPrint(seg, "小明最近在学习机器学习、自然语言处理、云计算和大数据");
        }

        [TestCase]
        public void TestSplit_Han_Default()
        {
            var s = "IBM是一家不错的公司，给你发offer了吗？";
            foreach (var part in JiebaSegmenter.RegexChineseDefault.Split(s))
            {
                Console.WriteLine(part);
            }

            foreach (var part in JiebaSegmenter.RegexChineseCutAll.Split(s))
            {
                Console.WriteLine(part);
            }
        }

        [TestCase]
        public void TestPercentages()
        {
            var seg = new JiebaSegmenter();
            
            var s = "看上去iphone8手机样式很赞,售价699美元,销量涨了5%么？";
            var segments = seg.Cut(s);
            Assert.That(segments, Contains.Item("5%"));
            foreach (var sm in segments)
            {
                Console.WriteLine(sm);
            }

            s = "pi的值是3.14，这是99.99%的人都知道的。";
            segments = seg.Cut(s);
            Assert.That(segments, Contains.Item("3.14"));
            Assert.That(segments, Contains.Item("99.99%"));
        }

        [TestCase]
        public void TestHyphen()
        {
            var seg = new JiebaSegmenter();
            seg.AddWord("cet-4");

            var s = "你一定也考过cet-4了。";
            var segments = seg.Cut(s).ToList();
            Assert.That(segments, Contains.Item("cet-4"));
            Console.WriteLine(segments);
            foreach (var sm in segments)
            {
                Console.WriteLine(sm);
            }
        }

        [TestCase]
        [Category("Issue")]
        public void TestChineseDot()
        {
            // for #42, #43
            var seg = new JiebaSegmenter();
            seg.AddWord("艾尔肯·吐尼亚孜");
            seg.AddWord("短P-R间期");

            var s = "艾尔肯·吐尼亚孜新疆阿克苏人。 在短P-R间期。";
            var segments = seg.Cut(s).ToList();
            Assert.That(segments, Contains.Item("艾尔肯·吐尼亚孜"));
            Assert.That(segments, Contains.Item("短P-R间期"));
        }

        [TestCase]
        [Category("Issue")]
        public void TestIssue49()
        {
            // for #49
            var seg = new JiebaSegmenter();

            var s = "简历名称 JAVA后端";
            var segments = seg.Cut(s);
            Assert.That(segments.Count(), Is.EqualTo(5));

            s = "简历名称JAVA后端";
            segments = seg.Cut(s);
            Assert.That(segments.Count(), Is.EqualTo(4));
        }

        [TestCase]
        public void TestCutAllMixedZhEn()
        {
            var seg = new JiebaSegmenter();
            seg.AddWord("超敏C反应蛋白");

            var s = "很多人的第一门语言是C语言。超敏C反应蛋白是什么？";
            var segments = seg.CutAll(s).ToList();
            Assert.That(segments, Contains.Item("C语言"));
            Console.WriteLine(segments);
            foreach (var sm in segments)
            {
                Console.WriteLine(sm);
            }
        }

        [TestCase]
        [Category("Issue")]
        public void TestIssue46()
        {
            var seg = new JiebaSegmenter();
            seg.DeleteWord("天半");
            
            var segments = seg.CutAll("2天半").ToList();
            Assert.That(segments, Contains.Item("天"));
            Assert.That(segments, Contains.Item("半"));
        }

        [TestCase]
        [Category("Issue")]
        public void TestEnglishWordsCut()
        {
            var seg = new JiebaSegmenter();
            var text = "HighestDegree";
            Assert.That(seg.Cut(text), Is.EqualTo(new[] { text }));
            text = "HelloWorld";
            Assert.That(seg.Cut(text), Is.EqualTo(new[] { text }));
            text = "HelloWorldle";
            Assert.That(seg.Cut(text), Is.EqualTo(new[] { text }));
            text = "HelloWorldlee";
            Assert.That(seg.Cut(text), Is.EqualTo(new[] { text }));
        }

        [Test]
        public void TestWordFreq()
        {
            var s = "在数学和计算机科学之中，算法（algorithm）为任何良定义的具体计算步骤的一个序列，常用于计算、数据处理和自动推理。精确而言，算法是一个表示为有限长列表的有效方法。算法应包含清晰定义的指令用于计算函数。";
            var seg = new JiebaSegmenter();
            var freqs = new Counter<string>(seg.Cut(s));
            // TODO: use stopwords.
            foreach (var pair in freqs.MostCommon(5))
            {
                Console.WriteLine($"{pair.Key}: {pair.Value}");
            }
        }

        #region Private Helpers

        private void TestCutFunction(Func<string, bool, bool, IEnumerable<string>> method,
                                     bool cutAll, bool useHmm,
                                     string testResultFile)
        {
            var testCases = GetTestSentences();
            var testResults = File.ReadAllLines(testResultFile);
            Assert.That(testCases.Length, Is.EqualTo(testResults.Length));
            for (int i = 0; i < testCases.Length; i++)
            {
                var testCase = testCases[i];
                var testResult = testResults[i];
                Assert.That(method(testCase, cutAll, useHmm).Join("/ "), Is.EqualTo(testResult));
            }
        }

        private void TestCutSearchFunction(Func<string, bool, IEnumerable<string>> method,
                                     bool useHmm,
                                     string testResultFile)
        {
            var testCases = GetTestSentences();
            var testResults = File.ReadAllLines(testResultFile);
            Assert.That(testCases.Length, Is.EqualTo(testResults.Length));
            for (int i = 0; i < testCases.Length; i++)
            {
                var testCase = testCases[i];
                var testResult = testResults[i];
                Assert.That(method(testCase, useHmm).Join("/ "), Is.EqualTo(testResult));
            }
        }

        #endregion

#if AOTBA
        #region AOTBA 新功能测试

        /// <summary>
        /// 测试 lcut 方法（直接返回 List&lt;string&gt;）
        /// </summary>
        [TestCase]
        public void Test_Lcut()
        {
            var seg = new JiebaSegmenter();
            var text = "我来到北京清华大学";
            var words = seg.Lcut(text);
            Assert.IsInstanceOf<List<string>>(words);
            CollectionAssert.AreEqual(seg.Cut(text).ToList(), words);
            Console.WriteLine(string.Join("/", words));
        }

        /// <summary>
        /// 测试 lcut 全模式
        /// </summary>
        [TestCase]
        public void Test_Lcut_CutAll()
        {
            var seg = new JiebaSegmenter();
            var text = "我来到北京清华大学";
            var words = seg.Lcut(text, cutAll: true);
            Assert.IsInstanceOf<List<string>>(words);
            CollectionAssert.AreEqual(seg.Cut(text, cutAll: true).ToList(), words);
            Console.WriteLine(string.Join("/", words));
        }

        /// <summary>
        /// 测试 lcut_for_search 方法（直接返回 List&lt;string&gt;）
        /// </summary>
        [TestCase]
        public void Test_LcutForSearch()
        {
            var seg = new JiebaSegmenter();
            var text = "小明硕士毕业于中国科学院计算所，后在日本京都大学深造";
            var words = seg.LcutForSearch(text);
            Assert.IsInstanceOf<List<string>>(words);
            CollectionAssert.AreEqual(seg.CutForSearch(text).ToList(), words);
            Console.WriteLine(string.Join("/", words));
        }

        /// <summary>
        /// 测试 lcut_for_search 不使用 HMM
        /// </summary>
        [TestCase]
        public void Test_LcutForSearch_WithoutHmm()
        {
            var seg = new JiebaSegmenter();
            var text = "小明硕士毕业于中国科学院计算所，后在日本京都大学深造";
            var words = seg.LcutForSearch(text, hmm: false);
            Assert.IsInstanceOf<List<string>>(words);
            CollectionAssert.AreEqual(seg.CutForSearch(text, hmm: false).ToList(), words);
            Console.WriteLine(string.Join("/", words));
        }

        /// <summary>
        /// 测试 Tokenizer 自定义分词器（独立词典）
        /// </summary>
        [TestCase]
        public void Test_Tokenizer()
        {
            var tokenizer = new Tokenizer();
            var text = "我来到北京清华大学";
            var words = tokenizer.Lcut(text);
            Assert.IsInstanceOf<List<string>>(words);
            Assert.That(words, Contains.Item("清华大学"));
            Console.WriteLine(string.Join("/", words));
        }

        /// <summary>
        /// 测试 Tokenizer 使用配置创建
        /// </summary>
        [TestCase]
        public void Test_Tokenizer_WithConfig()
        {
            var config = new JiebaConfig(JiebaMode.ZhHans);
            var tokenizer = new Tokenizer(config);
            var text = "我来到北京清华大学";
            var words = tokenizer.Lcut(text);
            Assert.IsInstanceOf<List<string>>(words);
            Assert.That(words, Contains.Item("清华大学"));
            Console.WriteLine(string.Join("/", words));
        }

        /// <summary>
        /// 测试 Tokenizer 独立词典（添加词不影响其他实例）
        /// </summary>
        [TestCase]
        public void Test_Tokenizer_IndependentDict()
        {
            var tokenizer1 = new Tokenizer();
            var tokenizer2 = new Tokenizer();
            var text = "小明最近在学习机器学习和自然语言处理";

            // tokenizer1 添加新词
            tokenizer1.AddWord("机器学习");
            var words1 = tokenizer1.Lcut(text);
            Assert.That(words1, Contains.Item("机器学习"));

            // tokenizer2 不受影响
            var words2 = tokenizer2.Lcut(text);
            Assert.That(words2, Is.Not.Contains("机器学习"));
            Assert.That(words2, Contains.Item("机器"));
        }

        /// <summary>
        /// 测试 Jieba.Dt 默认分词器
        /// </summary>
        [TestCase]
        public void Test_JiebaDt()
        {
            var text = "我来到北京清华大学";
            var words = Jieba.Lcut(text);
            Assert.IsInstanceOf<List<string>>(words);
            Assert.That(words, Contains.Item("清华大学"));
            Console.WriteLine(string.Join("/", words));
        }

        /// <summary>
        /// 测试 Jieba 静态方法（都是 Jieba.Dt 的映射）
        /// </summary>
        [TestCase]
        public void Test_Jieba_StaticMethods()
        {
            var text = "我来到北京清华大学";

            // 测试 Cut
            var cutResult = Jieba.Cut(text).ToList();
            Assert.That(cutResult, Contains.Item("清华大学"));

            // 测试 Lcut
            var lcutResult = Jieba.Lcut(text);
            CollectionAssert.AreEqual(cutResult, lcutResult);

            // 测试 CutForSearch
            var searchResult = Jieba.CutForSearch(text).ToList();
            Assert.That(searchResult.Count, Is.GreaterThan(0));

            // 测试 LcutForSearch
            var lcutSearchResult = Jieba.LcutForSearch(text);
            CollectionAssert.AreEqual(searchResult, lcutSearchResult);
        }

        /// <summary>
        /// 测试异步创建 Tokenizer
        /// </summary>
        [TestCase]
        public async System.Threading.Tasks.Task Test_Tokenizer_CreateAsync()
        {
            var tokenizer = await Tokenizer.CreateAsync();
            var text = "我来到北京清华大学";
            var words = tokenizer.Lcut(text);
            Assert.IsInstanceOf<List<string>>(words);
            Assert.That(words, Contains.Item("清华大学"));
            Console.WriteLine(string.Join("/", words));
        }

        /// <summary>
        /// 测试异步创建 JiebaSegmenter
        /// </summary>
        [TestCase]
        public async System.Threading.Tasks.Task Test_JiebaSegmenter_CreateAsync()
        {
            var seg = await JiebaSegmenter.CreateAsync();
            var text = "我来到北京清华大学";
            var words = seg.Lcut(text);
            Assert.IsInstanceOf<List<string>>(words);
            Assert.That(words, Contains.Item("清华大学"));
            Console.WriteLine(string.Join("/", words));
        }

        #endregion
        #region 日期时间识别测试

        /// <summary>
        /// 测试时间格式 4:50（Issue #1）
        /// </summary>
        [TestCase]
        [Category("Issue")]
        public void TestDateTime_Issue1_TimeFormat()
        {
            var seg = new JiebaSegmenter();
            var text = "今天4:50某某某领了一只记号笔";
            var result = seg.Cut(text).ToList();
            Console.WriteLine(string.Join("/", result));
            Assert.That(result, Contains.Item("今天4:50"), "'今天4:50'应被识别为整体时间");
        }

        /// <summary>
        /// 测试ISO日期时间格式（Issue #2）
        /// </summary>
        [TestCase]
        [Category("Issue")]
        public void TestDateTime_Issue2_IsoDateTime()
        {
            var seg = new JiebaSegmenter();
            var text = "会议时间是2021-01-01 09:00:00";
            var result = seg.Cut(text).ToList();
            Console.WriteLine(string.Join("/", result));
            Assert.That(result.Any(w => w.Contains("2021-01-01")), "ISO日期时间应被识别");
        }

        /// <summary>
        /// 测试中文日期格式（DateChineseRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_ChineseDate()
        {
            var seg = new JiebaSegmenter();
            var text = "2021年1月1日是元旦";
            var result = seg.Cut(text).ToList();
            Console.WriteLine(string.Join("/", result));
            Assert.That(result, Contains.Item("2021年1月1日"), "中文日期应被识别");
        }

        /// <summary>
        /// 测试节日识别（FestivalRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_Festival()
        {
            var seg = new JiebaSegmenter();
            var text = "春节是中国的传统节日";
            var result = seg.Cut(text).ToList();
            Console.WriteLine(string.Join("/", result));
            Assert.That(result, Contains.Item("春节"), "节日应被识别");
        }

        /// <summary>
        /// 测试相对时间（RelativeRegex）—— "明天下午3点"应为整体
        /// </summary>
        [TestCase]
        public void TestDateTime_RelativeTime()
        {
            var seg = new JiebaSegmenter();
            var text = "明天下午3点开会";
            var result = seg.Cut(text).ToList();
            Console.WriteLine(string.Join("/", result));
            Assert.That(result, Contains.Item("明天下午3点"), "'明天下午3点'应被识别为整体时间");
        }

        /// <summary>
        /// 测试搜索引擎模式日期时间识别
        /// </summary>
        [TestCase]
        public void TestDateTime_SearchMode()
        {
            var seg = new JiebaSegmenter();
            var text = "今天4:50某某某领了一只记号笔";
            var result = seg.CutForSearch(text).ToList();
            Console.WriteLine(string.Join("/", result));
            // 搜索引擎模式也进行日期时间识别，"今天4:50"作为整体
            Assert.That(result.Contains("今天4:50"), "搜索引擎模式应识别时间");
        }

        /// <summary>
        /// 测试词性标注中的日期时间识别
        /// </summary>
        [TestCase]
        public void TestDateTime_PosSegment()
        {
            var posSeg = new PosSegmenter();
            var text = "今天4:50某某某领了一只记号笔";
            var result = posSeg.Cut(text).ToList();
            Console.WriteLine(string.Join("/", result));
            Assert.That(result.Any(p => p.Word == "今天4:50" && p.Flag == "t"), "'今天4:50'应被标记为时间词性");
        }

        /// <summary>
        /// 测试中文时间表达（TimeRegex格式2）
        /// 上午9点、下午3点半、晚上8点30分、凌晨2点
        /// </summary>
        [TestCase]
        public void TestDateTime_ChineseTimeExpressions()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("上午9点开会", "上午9点"),
                ("下午3点半", "下午3点半"),
                ("晚上8点30分", "晚上8点30分"),
                ("凌晨2点", "凌晨2点"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为整体时间");
            }
        }

        /// <summary>
        /// 测试数字时间格式（TimeRegex格式1）
        /// 14:30:00, 4:50
        /// </summary>
        [TestCase]
        public void TestDateTime_NumericTimeFormat()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("14:30:00", "14:30:00"),
                ("4:50", "4:50"),
                ("23:59", "23:59"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为时间");
            }
        }

        /// <summary>
        /// 测试ISO日期格式（DateTimeIsoRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_IsoDate()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("2024-12-25", "2024-12-25"),
                ("2024/12/25", "2024/12/25"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为日期");
            }
        }

        /// <summary>
        /// 测试中文日期（DateChineseRegex）—— 12月25日
        /// </summary>
        [TestCase]
        public void TestDateTime_ChineseDateShort()
        {
            var seg = new JiebaSegmenter();
            var text = "12月25日";
            var result = seg.Cut(text).ToList();
            Console.WriteLine($"12月25日 -> {string.Join("/", result)}");
            Assert.That(result, Contains.Item("12月25日"), "'12月25日'应被识别为日期");
        }

        /// <summary>
        /// 测试节日识别（FestivalRegex）—— 多种节日
        /// </summary>
        [TestCase]
        public void TestDateTime_Festivals()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("国庆节快乐", "国庆节"),
                ("中秋节团圆", "中秋节"),
                ("元旦放假", "元旦"),
                ("情人节快乐", "情人节"),
                ("端午节吃粽子", "端午节"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为节日");
            }
        }

        /// <summary>
        /// 测试节气识别（SolarTermRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_SolarTerms()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("立春到了", "立春"),
                ("清明时节雨纷纷", "清明"),
                ("冬至吃饺子", "冬至"),
                ("芒种忙种", "芒种"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为节气");
            }
        }

        /// <summary>
        /// 测试农历识别（LunarRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_LunarDate()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("腊月初八是腊八节", "腊月初八"),
                ("正月初一过年", "正月初一"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为农历日期");
            }
        }

        /// <summary>
        /// 测试天干地支+生肖（TraditionalRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_Traditional()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("甲子时", "甲子时"),
                ("属龙的人", "属龙"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为天干地支/生肖");
            }
        }

        /// <summary>
        /// 测试朝代识别（DynastyRegex）—— 确保朝代正常识别且不误匹配"明天"
        /// </summary>
        [TestCase]
        public void TestDateTime_Dynasty()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("唐朝盛世", "唐朝"),
                ("明朝灭亡", "明朝"),
                ("宋朝文化", "宋朝"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为朝代");
            }
        }

        /// <summary>
        /// 测试朝代不误匹配"明天"、"明年"等
        /// </summary>
        [TestCase]
        public void TestDateTime_DynastyNotFalsePositive()
        {
            var seg = new JiebaSegmenter();
            var text = "明天下午3点开会";
            var result = seg.Cut(text).ToList();
            Console.WriteLine($"{text} -> {string.Join("/", result)}");
            Assert.That(result, Contains.Item("明天下午3点"), "'明天下午3点'应被识别为整体时间，'明'不应被误识别为朝代");
        }

        /// <summary>
        /// 测试相对时间组合（RelativeRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_RelativeTimeCombinations()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("昨天上午9点出发", "昨天上午9点"),
                ("后天下午2点", "后天下午2点"),
                ("今天晚上8点", "今天晚上8点"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为整体相对时间");
            }
        }

        /// <summary>
        /// 测试持续时间（DurationRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_Duration()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("3个小时", "3个小时"),
                ("5分钟", "5分钟"),
                ("2天", "2天"),
                ("十周", "十周"),
                ("三周", "三周"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为持续时间");
            }
        }

        /// <summary>
        /// 测试时间范围（RangeRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_TimeRange()
        {
            var seg = new JiebaSegmenter();
            var text = "9:00到17:00上班";
            var result = seg.Cut(text).ToList();
            var joined = string.Join("/", result);
            Console.WriteLine($"{text} -> {joined}");
            Assert.That(result.Any(w => w.Contains("9:00") && w.Contains("17:00")), "时间范围应被识别");
        }

        /// <summary>
        /// 测试截止时间（DeadlineRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_Deadline()
        {
            var seg = new JiebaSegmenter();
            var text = "截止2024-12-31提交";
            var result = seg.Cut(text).ToList();
            var joined = string.Join("/", result);
            Console.WriteLine($"{text} -> {joined}");
            Assert.That(result.Any(w => w.Contains("截止") || w.Contains("2024-12-31")), "截止时间应被识别");
        }

        /// <summary>
        /// 测试季度（QuarterRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_Quarter()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("2024年第一季度", "2024年第一季度"),
                ("2024年Q1", "2024年Q1"),
                ("Q3财报", "Q3"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result.Any(w => w.Contains(expected)), $"'{expected}'应被识别为季度");
            }
        }

        /// <summary>
        /// 测试星期（WeekdayRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_Weekday()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("星期一开会", "星期一"),
                ("周三见", "周三"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为星期");
            }
        }

        /// <summary>
        /// 测试时区（TimezoneRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_Timezone()
        {
            var seg = new JiebaSegmenter();
            var text = "UTC+8北京时间";
            var result = seg.Cut(text).ToList();
            var joined = string.Join("/", result);
            Console.WriteLine($"{text} -> {joined}");
            Assert.That(result.Any(w => w.Contains("UTC")), "UTC时区应被识别");
            Assert.That(result.Any(w => w.Contains("北京时间")), "'北京时间'应被识别为完整时区");
        }

        /// <summary>
        /// 测试纪念日（AnniversaryRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_Anniversary()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("十周年纪念", "十周年"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result.Any(w => w.Contains(expected)), $"'{expected}'应被识别为纪念日");
            }
        }

        /// <summary>
        /// 测试模糊时间（FuzzyRegex）
        /// </summary>
        [TestCase]
        public void TestDateTime_Fuzzy()
        {
            var seg = new JiebaSegmenter();

            var testCases = new (string text, string expected)[]
            {
                ("上旬开会", "上旬"),
                ("月底截止", "月底"),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"{text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为模糊时间");
            }
        }

        #endregion
#endif
    }
}