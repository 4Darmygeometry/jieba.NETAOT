using System;
using System.IO;
using System.Linq;
using JiebaNet.Analyser;
using NUnit.Framework;

namespace JiebaNet.Segmenter.Tests
{
    [TestFixture]
    public class TestTextRankExtractor
    {
        [TestCase]
        public void TestTextRankExtractorWithWeight()
        {
            var s =  "此外，公司拟对全资子公司吉林欧亚置业有限公司增资4.3亿元，增资后，吉林欧亚置业注册资本由7000万元增加到5亿元。吉林欧亚置业主要经营范围为房地产开发及百货零售等业务。目前在建吉林欧亚城市商业综合体项目 2013年，实现营业收入0万元，实现净利润-139.13万元。";
            var extractor = new TextRankExtractor();
            var result = extractor.ExtractTagsWithWeight(s);
            foreach (var tag in result)
            {
                Console.WriteLine("({0}, {1})", tag.Word, tag.Weight);
            }
        }

        [TestCase]
        public void TestTextRankExtractorWithoutWeights()
        {
            var s = "此外，公司拟对全资子公司吉林欧亚置业有限公司增资4.3亿元，增资后，吉林欧亚置业注册资本由7000万元增加到5亿元。吉林欧亚置业主要经营范围为房地产开发及百货零售等业务。目前在建吉林欧亚城市商业综合体项目 2013年，实现营业收入0万元，实现净利润-139.13万元。";
            var extractor = new TextRankExtractor();
            var result = extractor.ExtractTags(s);
            Assert.That(result, Contains.Item("吉林"));

            result = extractor.ExtractTags(s, allowPos: new []{ "n" });
            Assert.That(result, Is.Not.Contains("吉林"));
            Assert.That(result, Is.Not.Contains("实现"));
        }

        #region 扩展区汉字和Emoji测试

        /// <summary>
        /// 测试TextRank提取含扩展区汉字的文本关键词
        /// 词典中已包含𰻝𰻝面、𧒽岗、石𬒔等扩展区汉字词条
        /// </summary>
        [TestCase]
        public void TestTextRankWithExtendedCJK()
        {
            var extractor = new TextRankExtractor();
            var text = "从𧒽岗出发，经过石𬒔，最后去吃𰻝𰻝面，𰻝𰻝面是陕西特色面食，𧒽岗是佛山南海地名，石𬒔也是佛山南海地名";
            var result = extractor.ExtractTags(text, 10).ToList();
            foreach (var tag in result)
            {
                Console.WriteLine($"[TextRank扩展区] {tag}");
            }

            // 扩展区汉字词条应被提取出来
            Assert.That(result, Contains.Item("𰻝𰻝面"), "𰻝𰻝面应被TextRank提取");
            Assert.That(result, Contains.Item("𧒽岗"), "𧒽岗应被TextRank提取");
            Assert.That(result, Contains.Item("石𬒔"), "石𬒔应被TextRank提取");
        }

        /// <summary>
        /// 测试TextRank提取含扩展区汉字的文本关键词（带权重）
        /// </summary>
        [TestCase]
        public void TestTextRankWithWeightWithExtendedCJK()
        {
            var extractor = new TextRankExtractor();
            var text = "从𧒽岗出发，经过石𬒔，最后去吃𰻝𰻝面，𰻝𰻝面是陕西特色面食";
            var result = extractor.ExtractTagsWithWeight(text, 10).ToList();
            foreach (var tag in result)
            {
                Console.WriteLine($"[TextRank扩展区权重] {tag.Word}: {tag.Weight}");
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
        /// 测试TextRank提取含emoji的文本关键词
        /// </summary>
        [TestCase]
        public void TestTextRankWithEmoji()
        {
            var extractor = new TextRankExtractor();
            var text = "今天天气真好😀，适合出去爬山😊，大家都很开心🤣，天气真好啊天气真好";
            var result = extractor.ExtractTags(text, 10).ToList();
            foreach (var tag in result)
            {
                Console.WriteLine($"[TextRank+Emoji] {tag}");
            }

            // 中文关键词应正常提取
            Assert.That(result, Contains.Item("天气"), "中文关键词应正常提取");
        }

        /// <summary>
        /// 测试TextRank提取含扩展区汉字和emoji混合文本的关键词
        /// </summary>
        [TestCase]
        public void TestTextRankWithExtendedCJKAndEmoji()
        {
            var extractor = new TextRankExtractor();
            var text = "去𧒽岗吃𰻝𰻝面😀，𰻝𰻝面太好吃了😊，𧒽岗的风景也不错🤣，推荐大家去𧒽岗";
            var result = extractor.ExtractTags(text, 10).ToList();
            foreach (var tag in result)
            {
                Console.WriteLine($"[TextRank混合] {tag}");
            }

            // 扩展区汉字词条应被提取
            Assert.That(result, Contains.Item("𰻝𰻝面"), "𰻝𰻝面应被提取");
            Assert.That(result, Contains.Item("𧒽岗"), "𧒽岗应被提取");
        }

        #endregion
    }
}