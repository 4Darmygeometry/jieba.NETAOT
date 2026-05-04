using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using JiebaNet.Segmenter.Common;

namespace JiebaNet.Segmenter.Tests
{
    [TestFixture]
    public class TestKeywordProcessor
    {
        private KeywordProcessor GetSimpleProcessor()
        {
            var kp = new KeywordProcessor();
            kp.AddKeywords(new []{".NET Core", "Java", "C语言", "字典 tree", "CET-4", "网络 编程"});
            return kp;
        }
        
        [TestCase]
        public void TestCreateProcessor()
        {
            var kp = new KeywordProcessor();
            kp.AddKeyword("Big Apple", cleanName: "New York");
            
            Assert.That(kp.CaseSensitive, Is.False);
            Assert.That(kp.Contains("Big"), Is.False);
            Assert.That(kp.Contains("Big Apple"), Is.True);
        }
        
        [TestCase]
        public void TestRemoveKeyword()
        {
            var kp = new KeywordProcessor();
            kp.AddKeyword(".net core");
            kp.AddKeyword("C# 8.0");
            kp.AddKeywords(new []{"C# 7.0", "C# 8.0"});
            
            var keywords = kp.ExtractKeywords("I am learning .net core and c# 8.0");
            var expected = new List<string> { ".net core", "C# 8.0"};
            Assert.That(keywords, Is.EqualTo(expected));
            
            Assert.That(kp.Contains("C# 8.0"), Is.True);
            kp.RemoveKeyword("C# 8.0");
            Assert.That(kp.Contains("C# 8.0"), Is.False);
            keywords = kp.ExtractKeywords("I am learning .net core and c# 8.0");
            expected = new List<string> { ".net core"};
            Assert.That(keywords, Is.EqualTo(expected));
        }

        [TestCase]
        public void TestExtract()
        {
            var kp = new KeywordProcessor();
            kp.AddKeywords(new []{"Big Apple", "Bay Area"});
            var keywords = kp.ExtractKeywords("I love Big Apple and Bay Area.");
            var expected = new List<string> { "Big Apple", "Bay Area"};
            Assert.That(keywords, Is.EqualTo(expected));
        }
        
        [TestCase]
        public void TestExtractSpans()
        {
            var kp = new KeywordProcessor();
            kp.AddKeywords(new []{"Big Apple", "Bay Area"});
            var keywords = kp.ExtractKeywordSpans("I love Big Apple and Bay Area.");
            var expected = new List<TextSpan> { new TextSpan("Big Apple", 7, 16), new TextSpan("Bay Area", 21, 29)};
            Assert.That(keywords, Is.EqualTo(expected));
        }
        
        [TestCase]
        public void TestExtractNone()
        {
            var kp = GetSimpleProcessor();
            
            var keywords = kp.ExtractKeywords("疾风知劲草。");
            Assert.That(keywords.IsEmpty(), Is.True);
        }
        
        [TestCase]
        public void TestExtractBounds()
        {
            var kp = GetSimpleProcessor();
            
            var keywords = kp.ExtractKeywords(".net core.");
            var expected = new List<string> { ".NET Core"};
            Assert.That(keywords, Is.EqualTo(expected));
            
            keywords = kp.ExtractKeywords(".net core");
            expected = new List<string> { ".NET Core"};
            Assert.That(keywords, Is.EqualTo(expected));
        }
        
        [TestCase]
        public void TestExtractMixed()
        {
            var kp = GetSimpleProcessor();
            
            var keywords = kp.ExtractKeywords("你需要通过cet-4考试，学习c语言、.NET core、网络 编程、JavaScript，掌握字典 tree的用法");
            // Java is not extracted.
            var expected = new List<string> { "CET-4", "C语言", ".NET Core", "网络 编程", "字典 tree"};
            Assert.That(keywords, Is.EqualTo(expected));
        }
        
        [TestCase]
        public void TestExtractMixedRaw()
        {
            var kp = GetSimpleProcessor();
            
            var keywords = 
                kp.ExtractKeywords("你需要通过cet-4考试，学习c语言、.NET core、网络 编程、JavaScript，掌握字典 tree的用法", raw: true);
            // Java is not extracted.
            var expected = new List<string> { "cet-4", "c语言", ".NET core", "网络 编程", "字典 tree"};
            Assert.That(keywords, Is.EqualTo(expected));
        }

        #region 扩展区汉字和Emoji测试

        /// <summary>
        /// 测试KeywordProcessor提取含扩展区汉字的关键词
        /// KeywordTrie按char逐字构建，代理对会被拆成两个trie节点，但查找路径一致
        /// 扩展区汉字不在NonWordBoundries中，被视为词边界，不影响匹配
        /// </summary>
        [TestCase]
        public void TestExtractExtendedCJKKeywords()
        {
            var kp = new KeywordProcessor();
            kp.AddKeywords(new[] { "𧒽岗", "石𬒔", "𰻝𰻝面" });

            // 测试提取扩展区汉字关键词
            var keywords = kp.ExtractKeywords("从𧒽岗出发，经过石𬒔，最后去吃𰻝𰻝面").ToList();
            foreach (var kw in keywords)
            {
                Console.WriteLine($"[KeywordProcessor扩展区] {kw}");
            }

            Assert.That(keywords, Contains.Item("𧒽岗"), "𧒽岗应被提取");
            Assert.That(keywords, Contains.Item("石𬒔"), "石𬒔应被提取");
            Assert.That(keywords, Contains.Item("𰻝𰻝面"), "𰻝𰻝面应被提取");
        }

        /// <summary>
        /// 测试KeywordProcessor提取含扩展区汉字关键词的文本位置
        /// </summary>
        [TestCase]
        public void TestExtractExtendedCJKKeywordSpans()
        {
            var kp = new KeywordProcessor();
            kp.AddKeywords(new[] { "𧒽岗", "𰻝𰻝面" });

            var spans = kp.ExtractKeywordSpans("去𧒽岗吃𰻝𰻝面").ToList();
            foreach (var span in spans)
            {
                Console.WriteLine($"[KeywordProcessor扩展区Span] text={span.Text}, start={span.Start}, end={span.End}");
            }

            Assert.That(spans.Count, Is.EqualTo(2), "应提取2个关键词");
            Assert.That(spans[0].Text, Is.EqualTo("𧒽岗"));
            Assert.That(spans[1].Text, Is.EqualTo("𰻝𰻝面"));
        }

        /// <summary>
        /// 测试KeywordProcessor对扩展区汉字关键词的Contains和Remove操作
        /// </summary>
        [TestCase]
        public void TestContainsAndRemoveExtendedCJKKeywords()
        {
            var kp = new KeywordProcessor();
            kp.AddKeywords(new[] { "𧒽岗", "石𬒔", "𰻝𰻝面" });

            Assert.That(kp.Contains("𧒽岗"), Is.True, "应包含𧒽岗");
            Assert.That(kp.Contains("石𬒔"), Is.True, "应包含石𬒔");
            Assert.That(kp.Contains("𰻝𰻝面"), Is.True, "应包含𰻝𰻝面");

            kp.RemoveKeyword("石𬒔");
            Assert.That(kp.Contains("石𬒔"), Is.False, "移除后不应包含石𬒔");

            var keywords = kp.ExtractKeywords("经过石𬒔去𧒽岗吃𰻝𰻝面").ToList();
            Assert.That(keywords, Does.Not.Contains("石𬒔"), "移除后石𬒔不应被提取");
            Assert.That(keywords, Contains.Item("𧒽岗"), "𧒽岗仍应被提取");
            Assert.That(keywords, Contains.Item("𰻝𰻝面"), "𰻝𰻝面仍应被提取");
        }

        /// <summary>
        /// 测试KeywordProcessor提取含emoji的关键词
        /// emoji不在NonWordBoundries中，被视为词边界
        /// </summary>
        [TestCase]
        public void TestExtractEmojiKeywords()
        {
            var kp = new KeywordProcessor();
            kp.AddKeywords(new[] { "😀", "😊", "🤣" });

            var keywords = kp.ExtractKeywords("今天😀很开心😊笑死了🤣").ToList();
            foreach (var kw in keywords)
            {
                Console.WriteLine($"[KeywordProcessor+Emoji] {kw}");
            }

            Assert.That(keywords, Contains.Item("😀"), "😀应被提取");
            Assert.That(keywords, Contains.Item("😊"), "😊应被提取");
            Assert.That(keywords, Contains.Item("🤣"), "🤣应被提取");
        }

        /// <summary>
        /// 测试KeywordProcessor提取含emoji关键词的文本位置
        /// </summary>
        [TestCase]
        public void TestExtractEmojiKeywordSpans()
        {
            var kp = new KeywordProcessor();
            kp.AddKeywords(new[] { "😀", "🤣" });

            var spans = kp.ExtractKeywordSpans("今天😀笑死了🤣").ToList();
            foreach (var span in spans)
            {
                Console.WriteLine($"[KeywordProcessor+Emoji Span] text={span.Text}, start={span.Start}, end={span.End}");
            }

            Assert.That(spans.Count, Is.EqualTo(2), "应提取2个emoji关键词");
            Assert.That(spans[0].Text, Is.EqualTo("😀"));
            Assert.That(spans[1].Text, Is.EqualTo("🤣"));
        }

        /// <summary>
        /// 测试KeywordProcessor提取混合扩展区汉字和emoji的关键词
        /// </summary>
        [TestCase]
        public void TestExtractMixedExtendedCJKAndEmojiKeywords()
        {
            var kp = new KeywordProcessor();
            kp.AddKeywords(new[] { "𧒽岗", "𰻝𰻝面", "😀", "😊" });

            var keywords = kp.ExtractKeywords("去𧒽岗吃𰻝𰻝面😀很开心😊").ToList();
            foreach (var kw in keywords)
            {
                Console.WriteLine($"[KeywordProcessor混合] {kw}");
            }

            Assert.That(keywords, Contains.Item("𧒽岗"), "𧒽岗应被提取");
            Assert.That(keywords, Contains.Item("𰻝𰻝面"), "𰻝𰻝面应被提取");
            Assert.That(keywords, Contains.Item("😀"), "😀应被提取");
            Assert.That(keywords, Contains.Item("😊"), "😊应被提取");
        }

        /// <summary>
        /// 测试KeywordProcessor对emoji关键词的Contains和Remove操作
        /// </summary>
        [TestCase]
        public void TestContainsAndRemoveEmojiKeywords()
        {
            var kp = new KeywordProcessor();
            kp.AddKeywords(new[] { "😀", "😊", "🤣" });

            Assert.That(kp.Contains("😀"), Is.True);
            Assert.That(kp.Contains("😊"), Is.True);
            Assert.That(kp.Contains("🤣"), Is.True);

            kp.RemoveKeyword("😊");
            Assert.That(kp.Contains("😊"), Is.False, "移除后不应包含😊");

            var keywords = kp.ExtractKeywords("😀😊🤣").ToList();
            Assert.That(keywords, Does.Not.Contains("😊"), "移除后😊不应被提取");
            Assert.That(keywords, Contains.Item("😀"), "😀仍应被提取");
            Assert.That(keywords, Contains.Item("🤣"), "🤣仍应被提取");
        }

        /// <summary>
        /// 测试KeywordProcessor提取含cleanName的扩展区汉字关键词
        /// </summary>
        [TestCase]
        public void TestExtractExtendedCJKWithCleanName()
        {
            var kp = new KeywordProcessor();
            kp.AddKeyword("𰻝𰻝面", cleanName: "biangbiang面");
            kp.AddKeyword("𧒽岗", cleanName: "佛山地名");

            var keywords = kp.ExtractKeywords("去吃𰻝𰻝面，经过𧒽岗").ToList();
            Assert.That(keywords, Contains.Item("biangbiang面"), "应返回cleanName");
            Assert.That(keywords, Contains.Item("佛山地名"), "应返回cleanName");
        }

        #endregion
    }
}
