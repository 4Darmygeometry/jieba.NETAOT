using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JiebaNet.Segmenter.Common;
using NUnit.Framework;

namespace JiebaNet.Segmenter.Tests.Common
{
    [TestFixture]
    public class TestCounter
    {
        [Test]
        public void TestCreateEmpty()
        {
            var counter = new Counter<int>();
            Assert.That(counter.Count, Is.EqualTo(0));
            Assert.That(counter.Total, Is.EqualTo(0));

            counter[2] = 10;
            Assert.That(counter.Count, Is.EqualTo(1));
            Assert.That(counter.Total, Is.EqualTo(10));
            Assert.That(counter[2], Is.EqualTo(10));
        }

        [Test]
        public void TestCreateWithEnumerable()
        {
            var source = "gallahad";
            var counter = new Counter<char>(source);
            Assert.That(counter.Total, Is.EqualTo(source.Length));

            Assert.That(counter['d'], Is.EqualTo(1));
            Assert.That(counter['a'], Is.EqualTo(3));
        }

        [Test]
        public void TestElements()
        {
            var source = "gallahad";
            var counter = new Counter<char>(source);
            var elements = counter.Elements;
            Assert.That(elements.Count(), Is.EqualTo(5));
        }

        [Test]
        public void TestAddWithEnumerable()
        {
            var counter = new Counter<char>("which");
            Assert.That(counter['h'], Is.EqualTo(2));
            counter.Add("witch");
            Assert.That(counter['h'], Is.EqualTo(3));
        }

        [Test]
        public void TestAddWithCounter()
        {
            var counter = new Counter<char>("which");
            var counter2 = new Counter<char>("witch");
            counter.Add(counter2);
            Assert.That(counter['h'], Is.EqualTo(3));
        }

        [Test]
        public void TestSubtractWithEnumerable()
        {
            var counter = new Counter<char>("which");
            Assert.That(counter['h'], Is.EqualTo(2));
            counter.Subtract("witch");
            Assert.That(counter['h'], Is.EqualTo(1));
            Assert.That(counter['w'], Is.EqualTo(0));
        }

        [Test]
        public void TestSubtractWithCounter()
        {
            var counter = new Counter<char>("which");
            var counter2 = new Counter<char>("witch");
            counter.Subtract(counter2);
            Assert.That(counter['h'], Is.EqualTo(1));
            Assert.That(counter['w'], Is.EqualTo(0));
        }

        [Test]
        public void TestUnion()
        {
            var counter1 = new Counter<char>("abbb");
            Assert.That(counter1.Count, Is.EqualTo(2));

            var counter2 = new Counter<char>("bcc");
            var counter = counter1.Union(counter2);
            Assert.That(counter.Count, Is.EqualTo(3));
            Assert.That(counter.Total, Is.EqualTo(6));
            Assert.That(counter['b'], Is.EqualTo(3));
            Assert.That(counter['c'], Is.EqualTo(2));
        }

        [Test]
        public void TestContains()
        {
            var counter = new Counter<char>("which");
            Assert.That(counter.Contains('w'), Is.True);
            Assert.That(counter.Contains('t'), Is.False);
        }

        [Test]
        public void TestRemove()
        {
            var counter = new Counter<char>("which");
            counter['t'] = 0;
            Assert.That(counter.Contains('t'), Is.True);

            counter.Remove('t');
            Assert.That(counter.Contains('t'), Is.False);
        }

        [Test]
        public void TestClear()
        {
            var counter = new Counter<char>("which");
            Assert.That(counter.Total, Is.EqualTo(5));

            counter.Clear();
            Assert.That(counter.Count, Is.EqualTo(0));
            Assert.That(counter.Total, Is.EqualTo(0));
        }

        [Test]
        public void TestMostCommon()
        {
            var counter = new Counter<char>("abcdeabcdabcaba");
            var top3 = counter.MostCommon(3).ToList();
            Assert.That(top3.First().Key, Is.EqualTo('a'));
            Assert.That(top3.First().Value, Is.EqualTo(5));
            Assert.That(top3.Last().Key, Is.EqualTo('c'));
            Assert.That(top3.Last().Value, Is.EqualTo(3));

            var all = counter.MostCommon().ToList();
            Assert.That(all.First().Key, Is.EqualTo('a'));
            Assert.That(all.First().Value, Is.EqualTo(5));
            Assert.That(all.Last().Key, Is.EqualTo('e'));
            Assert.That(all.Last().Value, Is.EqualTo(1));

            var none = counter.MostCommon(0).ToList();
            Assert.That(none.Count, Is.EqualTo(0));
        }

        #region Counter<char> BMP限制验证

        /// <summary>
        /// 测试Counter&lt;char&gt;对扩展区汉字的错误行为（代理对被拆分）
        /// 这验证了Counter&lt;char&gt;仅支持BMP的文档说明
        /// </summary>
        [Test]
        public void TestCounterCharWithExtendedCJK_ShouldSplitSurrogatePairs()
        {
            // 𧒽 = U+274BD，扩展B区字符，由代理对 D85D DCBD 表示
            var text = "𧒽岗";
            var counter = new Counter<char>(text);

            // Counter<char>会将代理对拆成两个char分别计数
            // 𧒽 → 高代理(0xD85D) + 低代理(0xDCBD)
            // 岗 → 单个BMP字符
            // 所以总计数应为3（2个代理char + 1个BMP char）
            Assert.That(counter.Total, Is.EqualTo(3), "Counter<char>将代理对拆分为两个char，总计数应为3");

            // 高代理和低代理各出现1次
            Assert.That(counter['\uD85D'], Is.EqualTo(1), "高代理应出现1次");
            Assert.That(counter['\uDCBD'], Is.EqualTo(1), "低代理应出现1次");

            // 岗是BMP字符，正常计数
            Assert.That(counter['岗'], Is.EqualTo(1), "BMP字符'岗'应正常计数");
        }

        /// <summary>
        /// 测试Counter&lt;char&gt;对emoji的错误行为（代理对被拆分）
        /// 这验证了Counter&lt;char&gt;仅支持BMP的文档说明
        /// </summary>
        [Test]
        public void TestCounterCharWithEmoji_ShouldSplitSurrogatePairs()
        {
            // 😀 = U+1F600 → 高代理D83D + 低代理DE00
            // 😊 = U+1F60A → 高代理D83D + 低代理DE0A
            // 🤣 = U+1F923 → 高代理D83E + 低代理DD23
            var text = "😀😊🤣";
            var counter = new Counter<char>(text);

            // 3个emoji各占2个char，共6个char
            Assert.That(counter.Total, Is.EqualTo(6), "Counter<char>将3个emoji拆分为6个代理char");

            // 😀和😊共享高代理0xD83D，🤣使用高代理0xD83E
            Assert.That(counter['\uD83D'], Is.EqualTo(2), "😀和😊共享同一高代理0xD83D");
            Assert.That(counter['\uD83E'], Is.EqualTo(1), "🤣使用高代理0xD83E");
        }

        #endregion

        #region Counter<string> 配合分词器的Emoji过滤测试

        /// <summary>
        /// 测试Counter&lt;string&gt;默认模式（过滤emoji）
        /// 默认模式countEmoji为false，Counter后期过滤掉emoji词频
        /// </summary>
        [Test]
        public void TestCounterStringDefaultModeNoEmoji()
        {
            var seg = new JiebaSegmenter();
            var s = "从𧒽岗出发去吃𰻝𰻝面，经过石𬒔，今天😀很开心😊笑死了🤣";
            var freqs = new Counter<string>(seg.Cut(s));

            // 扩展区汉字词条应正确统计
            Assert.That(freqs.Contains("𰻝𰻝面"), Is.True, "应包含𰻝𰻝面");
            Assert.That(freqs.Contains("𧒽岗"), Is.True, "应包含𧒽岗");
            Assert.That(freqs.Contains("石𬒔"), Is.True, "应包含石𬒔");

            // 默认模式过滤emoji，emoji不应出现在词频结果中
            Assert.That(freqs.Contains("😀"), Is.False, "默认模式应过滤😀");
            Assert.That(freqs.Contains("😊"), Is.False, "默认模式应过滤😊");
            Assert.That(freqs.Contains("🤣"), Is.False, "默认模式应过滤🤣");

            foreach (var pair in freqs.MostCommon(10))
            {
                Console.WriteLine($"[默认模式] {pair.Key}: {pair.Value}");
            }
        }

        /// <summary>
        /// 测试Counter&lt;string&gt;的countEmoji模式（保留emoji词频）
        /// countEmoji为true时，Counter不过滤emoji，保留emoji词频
        /// </summary>
        [Test]
        public void TestCounterStringWithCountEmoji()
        {
            var seg = new JiebaSegmenter();
            var s = "从𧒽岗出发去吃𰻝𰻝面，经过石𬒔，今天😀很开心😊笑死了🤣";
            var freqs = new Counter<string>(seg.Cut(s), countEmoji: true);

            // 扩展区汉字词条应正确统计
            Assert.That(freqs.Contains("𰻝𰻝面"), Is.True, "应包含𰻝𰻝面");
            Assert.That(freqs.Contains("𧒽岗"), Is.True, "应包含𧒽岗");
            Assert.That(freqs.Contains("石𬒔"), Is.True, "应包含石𬒔");

            // countEmoji模式下emoji应被保留
            Assert.That(freqs.Contains("😀"), Is.True, "应包含😀");
            Assert.That(freqs.Contains("😊"), Is.True, "应包含😊");
            Assert.That(freqs.Contains("🤣"), Is.True, "应包含🤣");

            foreach (var pair in freqs.MostCommon(17))
            {
                Console.WriteLine($"[CountEmoji模式] {pair.Key}: {pair.Value}");
            }
        }

        /// <summary>
        /// 测试Counter&lt;string&gt;配合分词器可正确处理扩展区汉字
        /// 验证 new Counter&lt;string&gt;(seg.Cut(s)) 对扩展区汉字文本均有效
        /// </summary>
        [Test]
        public void TestCounterStringWithSegmenterHandlesAllText()
        {
            var seg = new JiebaSegmenter();
            var s = "从𧒽岗出发，经过石𬒔，最后去吃𰻝𰻝面，𰻝𰻝面是陕西特色面食";
            var freqs = new Counter<string>(seg.Cut(s));

            // 分词器已正确处理扩展区汉字词条，Counter<string>直接统计分词结果即可
            Assert.That(freqs.Contains("𰻝𰻝面"), Is.True, "应包含𰻝𰻝面");
            Assert.That(freqs.Contains("𧒽岗"), Is.True, "应包含𧒽岗");
            Assert.That(freqs.Contains("石𬒔"), Is.True, "应包含石𬒔");

            foreach (var pair in freqs.MostCommon(5))
            {
                Console.WriteLine($"[Counter<string>+分词器] {pair.Key}: {pair.Value}");
            }
        }

        #endregion
    }
}
