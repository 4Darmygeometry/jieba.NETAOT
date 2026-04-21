using System;
using System.Collections.Generic;
using System.Text;
using JiebaNet.Segmenter;

namespace JiebaNet.NetFrameworkTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            Console.WriteLine("=== jieba.NETAOT .NET Framework 4.8 测试 ===");
            Console.WriteLine();

            var segmenter = new JiebaSegmenter();

            Console.WriteLine("[测试] 精确模式分词...");
            var segments = segmenter.Cut("我来到北京清华大学");
            Console.WriteLine($"  结果: {string.Join("/", segments)}");
            Console.WriteLine("  通过 ✓");

            Console.WriteLine("[测试] 全模式分词...");
            segments = segmenter.Cut("我来到北京清华大学", cutAll: true);
            Console.WriteLine($"  结果: {string.Join("/", segments)}");
            Console.WriteLine("  通过 ✓");

            Console.WriteLine("[测试] Emoji分词...");
            string emojiText = "今天天气真好😀明天去爬山🎉";
            segments = segmenter.Cut(emojiText);
            string emojiResult = string.Join("/", segments);
            Console.WriteLine($"  输入: {emojiText}");
            Console.WriteLine($"  结果: {emojiResult}");
            bool emojiPass = emojiResult.Contains("😀") && emojiResult.Contains("🎉");
            Console.WriteLine(emojiPass ? "  通过 ✓" : "  失败 ✗");

            Console.WriteLine("[测试] 繁体中文分词...");
            string hantText = "我來到北京清華大學";
            segments = segmenter.Cut(hantText);
            Console.WriteLine($"  输入: {hantText}");
            Console.WriteLine($"  结果: {string.Join("/", segments)}");
            Console.WriteLine("  通过 ✓");

            Console.WriteLine();
            Console.WriteLine("=== 所有测试完成！ ===");
        }
    }
}
