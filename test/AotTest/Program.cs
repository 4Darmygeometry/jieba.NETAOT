using JiebaNet.Analyser;
using JiebaNet.Segmenter;
using JiebaNet.Segmenter.PosSeg;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace JiebaNet.AotTest;

class Program
{
    static int Main(string[] args)
    {
        var allPassed = true;

        Console.OutputEncoding = Encoding.UTF8;

        Console.WriteLine("=== jieba.NET AOT 兼容性测试 ===");
        Console.WriteLine();

        allPassed &= TestCut();
        allPassed &= TestCutAll();
        allPassed &= TestCutForSearch();
        allPassed &= TestPosSegment();
        allPassed &= TestTfidfExtractor();
        allPassed &= TestTextRankExtractor();
        allPassed &= TestTokenize();
        allPassed &= TestEmojiSegment();
        allPassed &= TestTraditionalChineseSegment();

        Console.WriteLine();
        if (allPassed)
        {
            Console.WriteLine("=== 所有AOT测试通过！ ===");
            return 0;
        }
        else
        {
            Console.WriteLine("=== 有AOT测试失败！ ===");
            return 1;
        }
    }

    static bool TestCut()
    {
        Console.WriteLine("[测试] 精确模式分词...");
        try
        {
            var segmenter = new JiebaSegmenter();
            var result = segmenter.Cut("我来到北京清华大学");
            var joined = string.Join("/", result);
            Console.WriteLine($"  结果: {joined}");
            if (joined.Contains("清华大学") && joined.Contains("来到"))
            {
                Console.WriteLine("  通过 ✓");
                return true;
            }
            Console.WriteLine($"  失败 ✗ 期望包含'清华大学'和'来到'");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }

    static bool TestCutAll()
    {
        Console.WriteLine("[测试] 全模式分词...");
        try
        {
            var segmenter = new JiebaSegmenter();
            var result = segmenter.Cut("我来到北京清华大学", cutAll: true);
            var joined = string.Join("/", result);
            Console.WriteLine($"  结果: {joined}");
            if (joined.Contains("清华") && joined.Contains("大学"))
            {
                Console.WriteLine("  通过 ✓");
                return true;
            }
            Console.WriteLine("  失败 ✗");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }

    static bool TestCutForSearch()
    {
        Console.WriteLine("[测试] 搜索引擎模式分词...");
        try
        {
            var segmenter = new JiebaSegmenter();
            var result = segmenter.CutForSearch("小明硕士毕业于中国科学院计算所，后在日本京都大学深造");
            var joined = string.Join("/", result);
            Console.WriteLine($"  结果: {joined}");
            if (joined.Contains("中国") && joined.Contains("科学") && joined.Contains("学院"))
            {
                Console.WriteLine("  通过 ✓");
                return true;
            }
            Console.WriteLine("  失败 ✗");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }

    static bool TestPosSegment()
    {
        Console.WriteLine("[测试] 词性标注...");
        try
        {
            var posSeg = new PosSegmenter();
            var result = posSeg.Cut("我爱北京天安门");
            var joined = string.Join("/", result);
            Console.WriteLine($"  结果: {joined}");
            if (joined.Contains("北京") && joined.Contains("天安门"))
            {
                Console.WriteLine("  通过 ✓");
                return true;
            }
            Console.WriteLine("  失败 ✗");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }

    static bool TestTfidfExtractor()
    {
        Console.WriteLine("[测试] TF-IDF关键词提取...");
        try
        {
            var extractor = new TfidfExtractor();
            var result = extractor.ExtractTags("此外，公司拟对全资子公司吉林欧亚置业有限公司增资4.3亿元，三亚欧亚置业有限公司增资2.2亿元", 5);
            var joined = string.Join("/", result);
            Console.WriteLine($"  结果: {joined}");
            if (joined.Contains("欧亚") && joined.Contains("置业"))
            {
                Console.WriteLine("  通过 ✓");
                return true;
            }
            Console.WriteLine("  失败 ✗");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }

    static bool TestTextRankExtractor()
    {
        Console.WriteLine("[测试] TextRank关键词提取...");
        try
        {
            var extractor = new TextRankExtractor();
            var result = extractor.ExtractTags("此外，公司拟对全资子公司吉林欧亚置业有限公司增资4.3亿元，三亚欧亚置业有限公司增资2.2亿元", 5);
            var joined = string.Join("/", result);
            Console.WriteLine($"  结果: {joined}");
            if (joined.Length > 0)
            {
                Console.WriteLine("  通过 ✓");
                return true;
            }
            Console.WriteLine("  失败 ✗");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }

    static bool TestTokenize()
    {
        Console.WriteLine("[测试] 分词Tokenize...");
        try
        {
            var segmenter = new JiebaSegmenter();
            var result = segmenter.Tokenize("南京市长江大桥").ToList();
            Console.WriteLine($"  结果: {string.Join(", ", result.Select(t => $"{t.Word}[{t.StartIndex},{t.EndIndex}]"))}");
            if (result.Count > 0)
            {
                Console.WriteLine("  通过 ✓");
                return true;
            }
            Console.WriteLine("  失败 ✗");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }

    static bool TestEmojiSegment()
    {
        Console.WriteLine("[测试] Emoji分词...");
        try
        {
            var segmenter = new JiebaSegmenter();
            // 测试包含emoji的文本
            var text = "今天天气真好😀明天去爬山🎉";
            var result = segmenter.Cut(text).ToList();
            var joined = string.Join("/", result);
            Console.WriteLine($"  输入: {text}");
            Console.WriteLine($"  结果: {joined}");
            // 检查emoji是否被正确识别为独立词
            // 使用字符检查而非字符串包含检查，避免编码问题
            var hasEmoji = result.Any(w => w == "😀" || w == "🎉");
            if (hasEmoji)
            {
                Console.WriteLine("  通过 ✓");
                return true;
            }
            // 输出分词结果用于调试
            Console.WriteLine($"  分词结果数量: {result.Count}");
            Console.WriteLine("  失败 ✗ emoji未被正确识别");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }

    static bool TestTraditionalChineseSegment()
    {
        Console.WriteLine("[测试] 繁体中文分词...");
        try
        {
            var segmenter = new JiebaSegmenter();
            // 测试繁体中文文本
            var text = "我來到北京清華大學";
            var result = segmenter.Cut(text);
            var joined = string.Join("/", result);
            Console.WriteLine($"  输入: {text}");
            Console.WriteLine($"  结果: {joined}");
            // 检查繁体中文词汇是否被正确识别
            if (joined.Contains("清華大學") || joined.Contains("清華") || joined.Contains("大學"))
            {
                Console.WriteLine("  通过 ✓");
                return true;
            }
            Console.WriteLine("  失败 ✗ 繁体中文未被正确识别");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }
}
