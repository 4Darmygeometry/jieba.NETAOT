using System;
using System.IO;
using System.Linq;
using JiebaNet.Analyser;
using NUnit.Framework;

namespace JiebaNet.Segmenter.Tests
{
    [TestFixture]
    public class TestTfidfExtractor
    {
        private string GetFileContents(string fileName)
        {
            return File.ReadAllText(fileName);
        }

        [TestCase]
        public void TestExtractTags()
        {
            var tfidf = new TfidfExtractor();
            var text = GetFileContents(TestHelper.GetResourceFilePath("article.txt"));
            var result = tfidf.ExtractTags(text, 30);
            foreach (var tag in result)
            {
                Console.WriteLine(tag);
            }
        }

        [TestCase]
        public void TestExtractTagsWithWeights()
        {
            var tfidf = new TfidfExtractor();
            var text = GetFileContents(TestHelper.GetResourceFilePath("article.txt"));
            var result = tfidf.ExtractTagsWithWeight(text);
            foreach (var tag in result)
            {
                Console.WriteLine("({0}, {1})", tag.Word, tag.Weight);
            }
        }

        [TestCase]
        public void TestSetStopWords()
        {
            var tfidf = new TfidfExtractor();
            // Use less stopwords than default stopword list.
            tfidf.SetStopWords(TestHelper.GetResourceFilePath("stop_words_test.txt"));
            var text = GetFileContents(TestHelper.GetResourceFilePath("article.txt"));
            var result = tfidf.ExtractTags(text, 30);
            foreach (var tag in result)
            {
                Console.WriteLine(tag);
            }
        }

        [TestCase]
        public void TestExtractTagsOfSportsNews()
        {
            var tfidf = new TfidfExtractor();
            var text = GetFileContents(TestHelper.GetResourceFilePath("article_sports.txt"));
            var result = tfidf.ExtractTags(text);
            foreach (var tag in result)
            {
                Console.WriteLine(tag);
            }
        }

        [TestCase]
        public void TestExtractTagsOfSocialNews()
        {
            var tfidf = new TfidfExtractor();
            var text = GetFileContents(TestHelper.GetResourceFilePath("article_social.txt"));
            var result = tfidf.ExtractTags(text, 30);
            foreach (var tag in result)
            {
                Console.WriteLine(tag);
            }
        }

        [TestCase]
        public void TestExtractTagsWithPos()
        {
            var tfidf = new TfidfExtractor();
            var text = GetFileContents(TestHelper.GetResourceFilePath("article_social.txt"));
            var result = tfidf.ExtractTags(text, 30, Constants.NounAndVerbPos);
            foreach (var tag in result)
            {
                Console.WriteLine(tag);
            }
        }

        [TestCase]
        public void TestExtractIdioms()
        {
            var tfidf = new TfidfExtractor();
            var text = GetFileContents(TestHelper.GetResourceFilePath("article_social.txt"));
            var result = tfidf.ExtractTags(text, 50, Constants.IdiomPos);
            foreach (var tag in result)
            {
                Console.WriteLine(tag);
            }
        }

        [TestCase]
        [Category("Issue")]
        public void TestIssues()
        {
            // case 1
            var text = @"整併";
            var extractor = new TfidfExtractor();
            var keywords = extractor.ExtractTags(text, 10, Constants.NounPos);
            foreach (var keyword in keywords)
            {
                Console.WriteLine(keyword);
            }

            keywords = extractor.ExtractTags(text, 10, Constants.VerbPos);
            foreach (var keyword in keywords)
            {
                Console.WriteLine(keyword);
            }

            // case 2:
            text = "開発支援工具FLEXITE";
            keywords = extractor.ExtractTags(text, 10, Constants.NounPos);
            foreach (var keyword in keywords)
            {
                Console.WriteLine(keyword);
            }
        }

        #region 扩展区汉字和Emoji测试

        /// <summary>
        /// 测试TF-IDF提取含扩展区汉字的文本关键词
        /// 词典中已包含𰻝𰻝面、𧒽岗、石𬒔等扩展区汉字词条
        /// </summary>
        [TestCase]
        public void TestExtractTagsWithExtendedCJK()
        {
            var tfidf = new TfidfExtractor();
            var text = "从𧒽岗出发，经过石𬒔，最后去吃𰻝𰻝面，𰻝𰻝面是陕西特色面食，𧒽岗是佛山南海地名，石𬒔也是佛山南海地名";
            var result = tfidf.ExtractTags(text, 10).ToList();
            foreach (var tag in result)
            {
                Console.WriteLine($"[TF-IDF扩展区] {tag}");
            }

            // 扩展区汉字词条应被提取出来
            Assert.That(result, Contains.Item("𰻝𰻝面"), "𰻝𰻝面应被TF-IDF提取");
            Assert.That(result, Contains.Item("𧒽岗"), "𧒽岗应被TF-IDF提取");
            Assert.That(result, Contains.Item("石𬒔"), "石𬒔应被TF-IDF提取");
        }

        /// <summary>
        /// 测试TF-IDF提取含扩展区汉字的文本关键词（带权重）
        /// </summary>
        [TestCase]
        public void TestExtractTagsWithWeightWithExtendedCJK()
        {
            var tfidf = new TfidfExtractor();
            var text = "从𧒽岗出发，经过石𬒔，最后去吃𰻝𰻝面，𰻝𰻝面是陕西特色面食";
            var result = tfidf.ExtractTagsWithWeight(text, 10).ToList();
            foreach (var tag in result)
            {
                Console.WriteLine($"[TF-IDF扩展区权重] {tag.Word}: {tag.Weight}");
            }

            // 扩展区汉字词条应有权重值
            var extendedWords = result.Where(t => t.Word == "𰻝𰻝面" || t.Word == "𧒽岗" || t.Word == "石𬒔").ToList();
            Assert.That(extendedWords.Count, Is.GreaterThan(0), "应提取到扩展区汉字词条");
            foreach (var word in extendedWords)
            {
                Assert.That(word.Weight, Is.GreaterThan(0), $"{word.Word}的权重应大于0");
            }
        }

        /// <summary>
        /// 测试TF-IDF提取含emoji的文本关键词
        /// emoji在分词后作为独立词出现，string.Length为2（代理对），不会被长度过滤
        /// 但emoji出现次数少且IDF使用默认中位数，权重较低，通常不会出现在前N个关键词中
        /// </summary>
        [TestCase]
        public void TestExtractTagsWithEmoji()
        {
            var tfidf = new TfidfExtractor();
            var text = "今天天气真好😀，适合出去爬山😊，大家都很开心🤣，天气真好啊天气真好";
            var result = tfidf.ExtractTags(text, 10).ToList();
            foreach (var tag in result)
            {
                Console.WriteLine($"[TF-IDF+Emoji] {tag}");
            }

            // 中文关键词应正常提取
            Assert.That(result, Contains.Item("天气"), "中文关键词应正常提取");
        }

        /// <summary>
        /// 测试TF-IDF提取含扩展区汉字和emoji混合文本的关键词
        /// </summary>
        [TestCase]
        public void TestExtractTagsWithExtendedCJKAndEmoji()
        {
            var tfidf = new TfidfExtractor();
            var text = "去𧒽岗吃𰻝𰻝面😀，𰻝𰻝面太好吃了😊，𧒽岗的风景也不错🤣，推荐大家去𧒽岗";
            var result = tfidf.ExtractTags(text, 10).ToList();
            foreach (var tag in result)
            {
                Console.WriteLine($"[TF-IDF混合] {tag}");
            }

            // 扩展区汉字词条应被提取
            Assert.That(result, Contains.Item("𰻝𰻝面"), "𰻝𰻝面应被提取");
            Assert.That(result, Contains.Item("𧒽岗"), "𧒽岗应被提取");
        }

        #endregion
    }
}