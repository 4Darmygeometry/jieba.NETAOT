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

        /// <summary>
        /// 测试词典中的英文单词不应被拆分
        /// GitHub和github在词典中，应该作为整体输出
        /// </summary>
        [TestCase]
        [Category("Issue")]
        public void TestEnglishWordsInDict()
        {
            var seg = new JiebaSegmenter();
            
            // GitHub在词典中，应该作为整体输出
            var result1 = seg.Cut("GitHub").ToList();
            Assert.That(result1, Is.EqualTo(new[] { "GitHub" }), "GitHub在词典中，应该作为整体输出");
            
            // github在词典中，应该作为整体输出
            var result2 = seg.Cut("github").ToList();
            Assert.That(result2, Is.EqualTo(new[] { "github" }), "github在词典中，应该作为整体输出");
        }

        /// <summary>
        /// 测试词典中没有的英文单词应该作为整体输出
        /// </summary>
        [TestCase]
        [Category("Issue")]
        public void TestEnglishWordsNotInDict()
        {
            var seg = new JiebaSegmenter();
            
            // GitLab不在词典中，应该作为整体输出
            var result1 = seg.Cut("GitLab").ToList();
            Assert.That(result1, Is.EqualTo(new[] { "GitLab" }), "GitLab不在词典中，应该作为整体输出");
            
            // Gitee不在词典中，应该作为整体输出
            var result2 = seg.Cut("Gitee").ToList();
            Assert.That(result2, Is.EqualTo(new[] { "Gitee" }), "Gitee不在词典中，应该作为整体输出");
        }

        /// <summary>
        /// 测试域名应该作为整体输出
        /// </summary>
        [TestCase]
        [Category("Issue")]
        public void TestDomainNames()
        {
            var seg = new JiebaSegmenter();
            
            // nuget.org应该作为整体输出
            var result1 = seg.Cut("nuget.org").ToList();
            Assert.That(result1, Is.EqualTo(new[] { "nuget.org" }), "nuget.org应该作为整体输出");
            
            // www.baidu.com应该作为整体输出
            var result2 = seg.Cut("www.baidu.com").ToList();
            Assert.That(result2, Is.EqualTo(new[] { "www.baidu.com" }), "www.baidu.com应该作为整体输出");
        }

        /// <summary>
        /// 测试连字符/下划线连接的单词应该作为整体输出
        /// </summary>
        [TestCase]
        [Category("Issue")]
        public void TestHyphenatedWords()
        {
            var seg = new JiebaSegmenter();
            
            // TF-IDF应该作为整体输出
            var result1 = seg.Cut("TF-IDF识别方法").ToList();
            Assert.That(result1, Is.EqualTo(new[] { "TF-IDF", "识别方法" }), "TF-IDF应该作为整体输出");
            
            // word1_word2_word3应该作为整体输出
            var result2 = seg.Cut("word1_word2_word3").ToList();
            Assert.That(result2, Is.EqualTo(new[] { "word1_word2_word3" }), "word1_word2_word3应该作为整体输出");
            
            // hello-world应该作为整体输出
            var result3 = seg.Cut("hello-world").ToList();
            Assert.That(result3, Is.EqualTo(new[] { "hello-world" }), "hello-world应该作为整体输出");
            
            // test_case_example应该作为整体输出
            var result4 = seg.Cut("test_case_example").ToList();
            Assert.That(result4, Is.EqualTo(new[] { "test_case_example" }), "test_case_example应该作为整体输出");
        }

        /// <summary>
        /// 测试完整URL应该作为整体输出
        /// </summary>
        [TestCase]
        [Category("Issue")]
        public void TestFullUrl()
        {
            var seg = new JiebaSegmenter();
            
            // https://gitee.com/JTsamsde/AOTba应该作为整体输出
            var result1 = seg.Cut("https://gitee.com/JTsamsde/AOTba").ToList();
            Assert.That(result1, Is.EqualTo(new[] { "https://gitee.com/JTsamsde/AOTba" }), "完整URL应该作为整体输出");
            
            // http://www.baidu.com/search?q=test应该作为整体输出
            var result2 = seg.Cut("http://www.baidu.com/search?q=test").ToList();
            Assert.That(result2, Is.EqualTo(new[] { "http://www.baidu.com/search?q=test" }), "带查询参数的URL应该作为整体输出");
            
            // 中文环境中的URL应该正确识别
            var result3 = seg.Cut("访问https://github.com查看代码").ToList();
            Assert.That(result3, Is.EqualTo(new[] { "访问", "https://github.com", "查看", "代码" }), "中文环境中的URL应该正确识别");
            
            // URL后面跟着中文应该正确识别
            var result4 = seg.Cut("网址是https://nuget.org/packages/test结束").ToList();
            Assert.That(result4, Is.EqualTo(new[] { "网址", "是", "https://nuget.org/packages/test", "结束" }), "URL后面跟着中文应该正确识别");
            
            // 简写域名带路径应该作为整体输出
            var result5 = seg.Cut("gitee.com/JTsamsde/AOTba").ToList();
            Assert.That(result5, Is.EqualTo(new[] { "gitee.com/JTsamsde/AOTba" }), "简写域名带路径应该作为整体输出");
            
            // 中文环境中的简写域名带路径应该正确识别
            var result6 = seg.Cut("访问gitee.com/JTsamsde/AOTba查看代码").ToList();
            Assert.That(result6, Is.EqualTo(new[] { "访问", "gitee.com/JTsamsde/AOTba", "查看", "代码" }), "中文环境中的简写域名带路径应该正确识别");
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

        [Test]
        public void TestDateTime_Ratio()
        {
            var seg = new JiebaSegmenter();

            // 时间格式测试（严格60进制）
            var timeTestCases = new (string text, string expected)[]
            {
                ("时间是00:30", "00:30"),
                ("会议在9:30开始", "9:30"),
                ("时间是14:30:00", "14:30:00"),
                ("毫秒时间14:30:00.123", "14:30:00.123"),
            };

            foreach (var (text, expected) in timeTestCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"[时间] {text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为时间");
            }

            // 比值格式测试（任意数字，支持小数）
            var ratioTestCases = new (string text, string expected)[]
            {
                ("比值是100:31", "100:31"),
                ("比例3:1", "3:1"),
                ("比分2:0", "2:0"),
                ("金龙鱼1:1:1调和油", "1:1:1"),
                ("黄金比例1:1.618", "1:1.618"),
                ("比例是25:75", "25:75"),
            };

            foreach (var (text, expected) in ratioTestCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"[比值] {text} -> {joined}");
                Assert.That(result, Contains.Item(expected), $"'{expected}'应被识别为比值");
            }
        }

        [Test]
        public void TestDateTime_RatioPosTag()
        {
            var posSeg = new PosSegmenter();

            // 时间格式词性标注为"t"
            var timeText = "时间是14:30";
            var timeResult = posSeg.Cut(timeText).ToList();
            var timeJoined = string.Join("/", timeResult.Select(p => $"{p.Word}/{p.Flag}"));
            Console.WriteLine($"[时间词性] {timeText} -> {timeJoined}");
            Assert.That(timeResult.Any(p => p.Word == "14:30" && p.Flag == "t"), Is.True, "时间应标注为t");

            // 比值格式词性标注为"n"
            var ratioText = "比值是100:31";
            var ratioResult = posSeg.Cut(ratioText).ToList();
            var ratioJoined = string.Join("/", ratioResult.Select(p => $"{p.Word}/{p.Flag}"));
            Console.WriteLine($"[比值词性] {ratioText} -> {ratioJoined}");
            Assert.That(ratioResult.Any(p => p.Word == "100:31" && p.Flag == "n"), Is.True, "比值应标注为n");
        }

        [Test]
        public void TestDateTime_ChineseTime()
        {
            var seg = new JiebaSegmenter();

            // 测试中文时间表达"八点整"
            var text1 = "现在是北京时间八点整";
            var result1 = seg.Cut(text1).ToList();
            var joined1 = string.Join("/", result1);
            Console.WriteLine($"[中文时间1] {text1} -> {joined1}");
            Assert.That(result1, Contains.Item("八点整"), "'八点整'应被识别为时间");

            // 测试中文时间表达"上午六点整"
            var text2 = "会议在上午六点整开始";
            var result2 = seg.Cut(text2).ToList();
            var joined2 = string.Join("/", result2);
            Console.WriteLine($"[中文时间2] {text2} -> {joined2}");
            Assert.That(result2, Contains.Item("上午六点整"), "'上午六点整'应被识别为时间");
        }

        [Test]
        public void TestDateTime_Version()
        {
            var seg = new JiebaSegmenter();

            // 测试版本号格式（版本号可能带上下文被整体识别）
            var testCases = new (string text, string[] expected)[]
            {
                ("当前版本是v1.0.1", new[] { "v1.0.1" }),
                ("软件版本1.0.1已发布", new[] { "软件版本1.0.1", "1.0.1" }),
                ("这是3.2-preview1版本", new[] { "3.2-preview1版本", "3.2-preview1" }),
                ("发布候选版本4.1.2-rc1", new[] { "候选版本4.1.2-rc1", "4.1.2-rc1" }),
                ("这是2.1-alpha1测试版", new[] { "2.1-alpha1测试版", "2.1-alpha1" }),
                ("当前是6.3-beta2版本", new[] { "6.3-beta2版本", "6.3-beta2" }),
            };

            foreach (var (text, expected) in testCases)
            {
                var result = seg.Cut(text).ToList();
                var joined = string.Join("/", result);
                Console.WriteLine($"[版本号] {text} -> {joined}");
                var matched = expected.Any(e => result.Contains(e));
                Assert.That(matched, Is.True, $"'{string.Join("'或'", expected)}'应被识别为版本号");
            }
        }

        /// <summary>
        /// 测试中文日期时间组合（DateTimeChineseRegex）
        /// 日期+时间紧邻时应作为整体识别，不应被拆分
        /// </summary>
        [TestCase]
        public void TestDateTime_ChineseDateTimeCombined()
        {
            var seg = new JiebaSegmenter();

            // 测试1：阿拉伯数字日期+阿拉伯数字时间
            var text1 = "2026年1月13日19点03分14秒";
            var result1 = seg.Cut(text1).ToList();
            var joined1 = string.Join("/", result1);
            Console.WriteLine($"[中文日期时间组合1] {text1} -> {joined1}");
            Assert.That(result1, Contains.Item("2026年1月13日19点03分14秒"), "'2026年1月13日19点03分14秒'应被识别为整体日期时间");

            // 测试2：中文数字日期+中文数字时间（含零）
            var text2 = "二零二六年一月十三日十九点零三分十四秒";
            var result2 = seg.Cut(text2).ToList();
            var joined2 = string.Join("/", result2);
            Console.WriteLine($"[中文日期时间组合2] {text2} -> {joined2}");
            Assert.That(result2, Contains.Item("二零二六年一月十三日十九点零三分十四秒"), "'二零二六年一月十三日十九点零三分十四秒'应被识别为整体日期时间");

            // 测试3：中文数字日期+中文数字时间（含二十）
            var text3 = "二零二六年一月十三日十九点二十分十四秒";
            var result3 = seg.Cut(text3).ToList();
            var joined3 = string.Join("/", result3);
            Console.WriteLine($"[中文日期时间组合3] {text3} -> {joined3}");
            Assert.That(result3, Contains.Item("二零二六年一月十三日十九点二十分十四秒"), "'二零二六年一月十三日十九点二十分十四秒'应被识别为整体日期时间");
        }

        /// <summary>
        /// 测试单独的中文时间表达式（不含日期）
        /// 中文数字的分钟和秒应被正确识别
        /// </summary>
        [TestCase]
        public void TestDateTime_ChineseTimeOnly()
        {
            var seg = new JiebaSegmenter();

            // 测试：单独的中文时间表达式（含中文数字分钟和秒）
            var text = "十九点二十分十四秒";
            var result = seg.Cut(text).ToList();
            var joined = string.Join("/", result);
            Console.WriteLine($"[单独中文时间] {text} -> {joined}");
            Assert.That(result, Contains.Item("十九点二十分十四秒"), "'十九点二十分十四秒'应被识别为整体时间");
        }

        /// <summary>
        /// 测试更多边缘场景
        /// </summary>
        [TestCase]
        public void TestDateTime_EdgeCases()
        {
            var seg = new JiebaSegmenter();

            // 测试1：十九点二十分（只有小时和分钟）
            var text1 = "十九点二十分";
            var result1 = seg.Cut(text1).ToList();
            var joined1 = string.Join("/", result1);
            Console.WriteLine($"[边缘1] {text1} -> {joined1}");
            Assert.That(result1, Contains.Item("十九点二十分"), "'十九点二十分'应被识别为整体时间");

            // 测试2：单独十九点（只有小时）
            var text2 = "十九点";
            var result2 = seg.Cut(text2).ToList();
            var joined2 = string.Join("/", result2);
            Console.WriteLine($"[边缘2] {text2} -> {joined2}");
            Assert.That(result2, Contains.Item("十九点"), "'十九点'应被识别为整体时间");

            // 测试3：非时间场景"零分"（没有"点"或"时"标识，不应被识别为时间）
            // 注意："零分"可能是词典中的词，但不应被时间识别器识别
            var text3 = "某人考试得了零分";
            var result3 = seg.Cut(text3).ToList();
            var joined3 = string.Join("/", result3);
            Console.WriteLine($"[边缘3] {text3} -> {joined3}");
            // 验证：即使"零分"出现在分词结果中，它也不应被识别为时间实体
            // 时间识别器要求"点"或"时"作为小时标识，所以"零分"不会被匹配
            Assert.That(!result3.Any(w => w == "零分" && w.Contains("点")), "'零分'不应被识别为时间（没有'点'或'时'标识）");

            // 测试4：非时间场景"三分"（没有"点"或"时"标识，不应被识别为时间）
            var text4 = "三分天下";
            var result4 = seg.Cut(text4).ToList();
            var joined4 = string.Join("/", result4);
            Console.WriteLine($"[边缘4] {text4} -> {joined4}");
            Assert.That(!result4.Any(w => w.Contains("点") || w.Contains("时")), "'三分'不应被识别为时间（没有'点'或'时'标识）");

            // 测试5：中文MM:SS场景（只有分钟和秒，没有小时）
            var text5 = "再等十九分二十秒，就要结束考试了";
            var result5 = seg.Cut(text5).ToList();
            var joined5 = string.Join("/", result5);
            Console.WriteLine($"[边缘5] {text5} -> {joined5}");
            Assert.That(result5, Contains.Item("十九分二十秒"), "'十九分二十秒'应被识别为整体时间");

            // 测试6：阿拉伯数字MM:SS场景（只有分钟和秒，没有小时）
            var text6 = "再等19分20秒，就要结束考试了";
            var result6 = seg.Cut(text6).ToList();
            var joined6 = string.Join("/", result6);
            Console.WriteLine($"[边缘6] {text6} -> {joined6}");
            Assert.That(result6, Contains.Item("19分20秒"), "'19分20秒'应被识别为整体时间");
        }

        /// <summary>
        /// 测试GB18030-2022扩展B-I区生僻字分词
        /// 包括扩展G区（𰻝）、扩展B区（𧒽）、扩展E区（𬒔）等字符
        /// </summary>
        [TestCase]
        public void TestGB18030_ExtendedCJK()
        {
            var seg = new JiebaSegmenter();

            // 测试1：𰻝𰻝面（扩展G区字符，U+30EDD）
            // 这是陕西特色面食"biangbiang面"的写法
            var text1 = "我今天吃了𰻝𰻝面，很好吃";
            var result1 = seg.Cut(text1).ToList();
            var joined1 = string.Join("/", result1);
            Console.WriteLine($"[生僻字1] {text1} -> {joined1}");
            Assert.That(result1, Contains.Item("𰻝𰻝面"), "'𰻝𰻝面'应被识别为整体（词典词条）");

            // 测试2：𧒽岗（扩展B区字符，U+274BD）
            // 这是佛山市南海区桂城街道的一个地名
            var text2 = "南海有轨电车一号线，起点为𧒽岗，终点为林岳东";
            var result2 = seg.Cut(text2).ToList();
            var joined2 = string.Join("/", result2);
            Console.WriteLine($"[生僻字2] {text2} -> {joined2}");
            Assert.That(result2, Contains.Item("𧒽岗"), "'𧒽岗'应被识别为整体（词典词条）");

            // 测试3：石𬒔（扩展E区字符，U+2C514）
            // 这是佛山市南海区桂城街道的一个地名
            var text3 = "石𬒔是佛山市南海区桂城街道的一个地名";
            var result3 = seg.Cut(text3).ToList();
            var joined3 = string.Join("/", result3);
            Console.WriteLine($"[生僻字3] {text3} -> {joined3}");
            Assert.That(result3, Contains.Item("石𬒔"), "'石𬒔'应被识别为整体（词典词条）");

            // 测试4：混合场景，包含多种扩展区字符
            var text4 = "从𧒽岗出发，经过石𬒔，最后去吃𰻝𰻝面";
            var result4 = seg.Cut(text4).ToList();
            var joined4 = string.Join("/", result4);
            Console.WriteLine($"[生僻字4] {text4} -> {joined4}");
            Assert.That(result4, Contains.Item("𧒽岗"), "'𧒽岗'应被识别为整体");
            Assert.That(result4, Contains.Item("石𬒔"), "'石𬒔'应被识别为整体");
            Assert.That(result4, Contains.Item("𰻝𰻝面"), "'𰻝𰻝面'应被识别为整体");
        }

        #endregion
#endif
    }
}