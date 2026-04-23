using JiebaNet.Analyser;
using JiebaNet.Segmenter;
using JiebaNet.Segmenter.PosSeg;
using System;
using System.Collections.Generic;
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
        allPassed &= TestComplexEmojiSegment();
        allPassed &= TestTraditionalChineseSegment();

#if AOTBA
        allPassed &= TestLcut();
        allPassed &= TestLcutForSearch();
        allPassed &= TestTokenizer();
        allPassed &= TestJiebaDt();
        allPassed &= TestTokenizerIndependentDict();
#endif

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

    static bool TestComplexEmojiSegment()
    {
        Console.WriteLine("[测试] 复杂Emoji分词（ZWJ序列、变体选择符、肤色修饰）...");
        try
        {
            var segmenter = new JiebaSegmenter();
            
            // 测试ZWJ序列
            var zwjText = "这是👨‍👨‍👧家庭";
            var zwjResult = segmenter.Cut(zwjText).ToList();
            var zwjJoined = string.Join("/", zwjResult);
            Console.WriteLine($"  ZWJ序列: {zwjText} -> {zwjJoined}");
            
            // 测试变体选择符
            var vsText = "今天看了▶︎视频";
            var vsResult = segmenter.Cut(vsText).ToList();
            var vsJoined = string.Join("/", vsResult);
            Console.WriteLine($"  变体选择符: {vsText} -> {vsJoined}");
            
            // 测试肤色修饰
            var skinText = "他是👨🏻‍⚕️医生";
            var skinResult = segmenter.Cut(skinText).ToList();
            var skinJoined = string.Join("/", skinResult);
            Console.WriteLine($"  肤色修饰: {skinText} -> {skinJoined}");
            
            // 测试国旗emoji
            var flagText = "我爱🇨🇳中国";
            var flagResult = segmenter.Cut(flagText).ToList();
            var flagJoined = string.Join("/", flagResult);
            Console.WriteLine($"  国旗emoji: {flagText} -> {flagJoined}");
            
            // 检查复杂emoji是否被完整保留
            var allPassed = true;
            
            // ZWJ序列应该作为整体保留
            if (!zwjResult.Contains("👨‍👨‍👧"))
            {
                Console.WriteLine("  警告: ZWJ序列未被完整保留");
                allPassed = false;
            }
            
            // 变体选择符emoji应该作为整体保留
            if (!vsResult.Contains("▶︎"))
            {
                Console.WriteLine("  警告: 变体选择符emoji未被完整保留");
                allPassed = false;
            }
            
            // 肤色修饰emoji应该作为整体保留
            if (!skinResult.Contains("👨🏻‍⚕️"))
            {
                Console.WriteLine("  警告: 肤色修饰emoji未被完整保留");
                allPassed = false;
            }
            
            // 国旗emoji应该作为整体保留
            if (!flagResult.Contains("🇨🇳"))
            {
                Console.WriteLine("  警告: 国旗emoji未被完整保留");
                allPassed = false;
            }
            
            if (allPassed)
            {
                Console.WriteLine("  通过 ✓");
                return true;
            }
            Console.WriteLine("  部分通过 ⚠");
            return false; //AOT模式的目标框架可以自动识别新表情包，不用查表。部分通过即为失败
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

#if AOTBA
    static bool TestLcut()
    {
        Console.WriteLine("[测试] lcut 直接返回 List<string>...");
        try
        {
            var seg = new JiebaSegmenter();
            var text = "我来到北京清华大学";
            var words = seg.Lcut(text);
            var joined = string.Join("/", words);
            Console.WriteLine($"  结果: {joined}");
            if (words is List<string> && words.Contains("清华大学"))
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

    static bool TestLcutForSearch()
    {
        Console.WriteLine("[测试] lcut_for_search 直接返回 List<string>...");
        try
        {
            var seg = new JiebaSegmenter();
            var text = "小明硕士毕业于中国科学院计算所";
            var words = seg.LcutForSearch(text);
            var joined = string.Join("/", words);
            Console.WriteLine($"  结果: {joined}");
            if (words is List<string> && words.Contains("中国"))
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

    static bool TestTokenizer()
    {
        Console.WriteLine("[测试] Tokenizer 自定义分词器...");
        try
        {
            var tokenizer = new Tokenizer();
            var text = "我来到北京清华大学";
            var words = tokenizer.Lcut(text);
            var joined = string.Join("/", words);
            Console.WriteLine($"  结果: {joined}");
            if (words.Contains("清华大学"))
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

    static bool TestJiebaDt()
    {
        Console.WriteLine("[测试] Jieba.Dt 默认分词器...");
        try
        {
            var text = "我来到北京清华大学";
            var words = Jieba.Lcut(text);
            var joined = string.Join("/", words);
            Console.WriteLine($"  结果: {joined}");
            if (words.Contains("清华大学"))
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

    static bool TestTokenizerIndependentDict()
    {
        Console.WriteLine("[测试] Tokenizer 独立词典...");
        try
        {
            var tokenizer1 = new Tokenizer();
            var tokenizer2 = new Tokenizer();
            var text = "小明最近在学习机器学习";

            tokenizer1.AddWord("机器学习");
            var words1 = tokenizer1.Lcut(text);
            var words2 = tokenizer2.Lcut(text);

            Console.WriteLine($"  tokenizer1: {string.Join("/", words1)}");
            Console.WriteLine($"  tokenizer2: {string.Join("/", words2)}");

            if (words1.Contains("机器学习") && !words2.Contains("机器学习"))
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
#endif
}
