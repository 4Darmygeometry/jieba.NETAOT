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

        Console.WriteLine("=== AOTba AOT 兼容性测试 ===");
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
        allPassed &= TestDateTimeSegment();
        allPassed &= TestDateTimePosSegment();
        allPassed &= TestLcut();
        allPassed &= TestLcutForSearch();
        allPassed &= TestTokenizer();
        allPassed &= TestJiebaDt();
        allPassed &= TestTokenizerIndependentDict();
        allPassed &= TestHyphenatedWords();
        allPassed &= TestDomainNames();
        allPassed &= TestGB18030ExtendedCJK();
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
            var joined = string.Join("╱", result);
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
            var joined = string.Join("╱", result);
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
            var joined = string.Join("╱", result);
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
            var joined = string.Join("╱", result);
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
            var joined = string.Join("╱", result);
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
            var joined = string.Join("╱", result);
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
            var joined = string.Join("╱", result);
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
            var zwjJoined = string.Join("╱", zwjResult);
            Console.WriteLine($"  ZWJ序列: {zwjText} -> {zwjJoined}");
            
            // 测试变体选择符
            var vsText = "今天看了▶︎视频";
            var vsResult = segmenter.Cut(vsText).ToList();
            var vsJoined = string.Join("╱", vsResult);
            Console.WriteLine($"  变体选择符: {vsText} -> {vsJoined}");
            
            // 测试肤色修饰
            var skinText = "他是👨🏻‍⚕️医生";
            var skinResult = segmenter.Cut(skinText).ToList();
            var skinJoined = string.Join("╱", skinResult);
            Console.WriteLine($"  肤色修饰: {skinText} -> {skinJoined}");
            
            // 测试国旗emoji
            var flagText = "我爱🇨🇳中国";
            var flagResult = segmenter.Cut(flagText).ToList();
            var flagJoined = string.Join("╱", flagResult);
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
            var joined = string.Join("╱", result);
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
    static bool TestDateTimeSegment()
    {
        Console.WriteLine("[测试] 日期时间比值版本号分词...");
        try
        {
            var segmenter = new JiebaSegmenter();

            // 测试1：时间格式 4:50（Issue #1）
            var text1 = "今天4:50某某某领了一只记号笔";
            var result1 = segmenter.Cut(text1).ToList();
            var joined1 = string.Join("╱", result1);
            Console.WriteLine($"  测试1: {text1}");
            Console.WriteLine($"  结果: {joined1}");
            if (!result1.Contains("今天4:50"))
            {
                Console.WriteLine("  失败 ✗ '今天4:50'未被正确识别为整体时间");
                return false;
            }

            // 测试2：ISO日期时间格式（Issue #2）
            var text2 = "会议时间是2021-01-01 09:00:00";
            var result2 = segmenter.Cut(text2).ToList();
            var joined2 = string.Join("╱", result2);
            Console.WriteLine($"  测试2: {text2}");
            Console.WriteLine($"  结果: {joined2}");
            if (!result2.Contains("2021-01-01 09:00:00") && !result2.Contains("2021-01-01"))
            {
                Console.WriteLine("  失败 ✗ ISO日期时间未被正确识别");
                return false;
            }

            // 测试3：中文日期
            var text3 = "2021年1月1日是元旦";
            var result3 = segmenter.Cut(text3).ToList();
            var joined3 = string.Join("╱", result3);
            Console.WriteLine($"  测试3: {text3}");
            Console.WriteLine($"  结果: {joined3}");
            if (!result3.Contains("2021年1月1日"))
            {
                Console.WriteLine("  失败 ✗ 中文日期未被正确识别");
                return false;
            }

            // 测试4：节日
            var text4 = "春节是中国的传统节日";
            var result4 = segmenter.Cut(text4).ToList();
            var joined4 = string.Join("╱", result4);
            Console.WriteLine($"  测试4: {text4}");
            Console.WriteLine($"  结果: {joined4}");
            if (!result4.Contains("春节"))
            {
                Console.WriteLine("  失败 ✗ 节日未被正确识别");
                return false;
            }

            // 测试5：相对时间
            var text5 = "明天下午3点开会";
            var result5 = segmenter.Cut(text5).ToList();
            var joined5 = string.Join("╱", result5);
            Console.WriteLine($"  测试5: {text5}");
            Console.WriteLine($"  结果: {joined5}");
            // 检查相对时间和时间是否被识别
            var hasTime = result5.Any(w => w.Contains("明天") || w.Contains("下午") || w.Contains("3点"));
            if (!hasTime)
            {
                Console.WriteLine("  失败 ✗ 相对时间未被正确识别");
                return false;
            }

            // 测试6：比值格式
            var text6 = "金龙鱼1:1:1调和油";
            var result6 = segmenter.Cut(text6).ToList();
            var joined6 = string.Join("╱", result6);
            Console.WriteLine($"  测试6: {text6}");
            Console.WriteLine($"  结果: {joined6}");
            if (!result6.Contains("1:1:1"))
            {
                Console.WriteLine("  失败 ✗ 比值'1:1:1'未被正确识别");
                return false;
            }

            // 测试7：比值
            var text7 = "比值是100:31";
            var result7 = segmenter.Cut(text7).ToList();
            var joined7 = string.Join("╱", result7);
            Console.WriteLine($"  测试7: {text7}");
            Console.WriteLine($"  结果: {joined7}");
            if (!result7.Contains("100:31"))
            {
                Console.WriteLine("  失败 ✗ 比值'100:31'未被正确识别");
                return false;
            }

            // 测试8：毫秒时间格式
            var text8 = "毫秒时间14:30:00.123";
            var result8 = segmenter.Cut(text8).ToList();
            var joined8 = string.Join("╱", result8);
            Console.WriteLine($"  测试8: {text8}");
            Console.WriteLine($"  结果: {joined8}");
            if (!result8.Contains("14:30:00.123"))
            {
                Console.WriteLine("  失败 ✗ 毫秒时间'14:30:00.123'未被正确识别");
                return false;
            }

            // 测试9：小数比值
            var text9 = "黄金比例1:1.618";
            var result9 = segmenter.Cut(text9).ToList();
            var joined9 = string.Join("╱", result9);
            Console.WriteLine($"  测试9: {text9}");
            Console.WriteLine($"  结果: {joined9}");
            if (!result9.Contains("1:1.618"))
            {
                Console.WriteLine("  失败 ✗ 小数比值'1:1.618'未被正确识别");
                return false;
            }

            // 测试10：中文时间表达"八点整"
            var text10 = "现在是北京时间八点整";
            var result10 = segmenter.Cut(text10).ToList();
            var joined10 = string.Join("╱", result10);
            Console.WriteLine($"  测试10: {text10}");
            Console.WriteLine($"  结果: {joined10}");
            if (!result10.Contains("八点整"))
            {
                Console.WriteLine("  失败 ✗ 中文时间'八点整'未被正确识别");
                return false;
            }

            // 测试11：中文时间表达"上午六点整"
            var text11 = "会议在上午六点整开始";
            var result11 = segmenter.Cut(text11).ToList();
            var joined11 = string.Join("╱", result11);
            Console.WriteLine($"  测试11: {text11}");
            Console.WriteLine($"  结果: {joined11}");
            if (!result11.Contains("上午六点整"))
            {
                Console.WriteLine("  失败 ✗ 中文时间'上午六点整'未被正确识别");
                return false;
            }

            // 测试12：版本号格式
            var text12 = "当前版本是v1.0.1";
            var result12 = segmenter.Cut(text12).ToList();
            var joined12 = string.Join("╱", result12);
            Console.WriteLine($"  测试12: {text12}");
            Console.WriteLine($"  结果: {joined12}");
            if (!result12.Contains("v1.0.1"))
            {
                Console.WriteLine("  失败 ✗ 版本号'v1.0.1'未被正确识别");
                return false;
            }

            // 测试13：版本号（无v前缀）
            var text13 = "软件版本1.0.1已发布";
            var result13 = segmenter.Cut(text13).ToList();
            var joined13 = string.Join("╱", result13);
            Console.WriteLine($"  测试13: {text13}");
            Console.WriteLine($"  结果: {joined13}");
            if (!result13.Contains("1.0.1"))
            {
                Console.WriteLine("  失败 ✗ 版本号'1.0.1'未被正确识别");
                return false;
            }

            // 测试14：版本号（带预览标签）
            var text14 = "这是3.2-preview1版本";
            var result14 = segmenter.Cut(text14).ToList();
            var joined14 = string.Join("╱", result14);
            Console.WriteLine($"  测试14: {text14}");
            Console.WriteLine($"  结果: {joined14}");
            if (!result14.Contains("3.2-preview1"))
            {
                Console.WriteLine("  失败 ✗ 版本号'3.2-preview1'未被正确识别");
                return false;
            }

            // 测试15：版本号（带rc标签）
            var text15 = "发布候选版本4.1.2-rc1";
            var result15 = segmenter.Cut(text15).ToList();
            var joined15 = string.Join("╱", result15);
            Console.WriteLine($"  测试15: {text15}");
            Console.WriteLine($"  结果: {joined15}");
            if (!result15.Contains("4.1.2-rc1"))
            {
                Console.WriteLine("  失败 ✗ 版本号'4.1.2-rc1'未被正确识别");
                return false;
            }

            // 测试16：版本号（带alpha标签）
            var text16 = "这是2.1-alpha1测试版";
            var result16 = segmenter.Cut(text16).ToList();
            var joined16 = string.Join("╱", result16);
            Console.WriteLine($"  测试16: {text16}");
            Console.WriteLine($"  结果: {joined16}");
            if (!result16.Contains("2.1-alpha1"))
            {
                Console.WriteLine("  失败 ✗ 版本号'2.1-alpha1'未被正确识别");
                return false;
            }

            // 测试17：版本号（带beta标签）
            var text17 = "当前是6.3-beta2版本";
            var result17 = segmenter.Cut(text17).ToList();
            var joined17 = string.Join("╱", result17);
            Console.WriteLine($"  测试17: {text17}");
            Console.WriteLine($"  结果: {joined17}");
            if (!result17.Contains("6.3-beta2"))
            {
                Console.WriteLine("  失败 ✗ 版本号'6.3-beta2'未被正确识别");
                return false;
            }

            // 测试18：中文日期时间组合（阿拉伯数字日期+阿拉伯数字时间）
            var text18 = "2026年1月13日19点03分14秒";
            var result18 = segmenter.Cut(text18).ToList();
            var joined18 = string.Join("╱", result18);
            Console.WriteLine($"  测试18: {text18}");
            Console.WriteLine($"  结果: {joined18}");
            if (!result18.Contains("2026年1月13日19点03分14秒"))
            {
                Console.WriteLine("  失败 ✗ '2026年1月13日19点03分14秒'未被正确识别为整体日期时间");
                return false;
            }

            // 测试19：中文日期时间组合（中文数字日期+中文数字时间，含零）
            var text19 = "二零二六年一月十三日十九点零三分十四秒";
            var result19 = segmenter.Cut(text19).ToList();
            var joined19 = string.Join("╱", result19);
            Console.WriteLine($"  测试19: {text19}");
            Console.WriteLine($"  结果: {joined19}");
            if (!result19.Contains("二零二六年一月十三日十九点零三分十四秒"))
            {
                Console.WriteLine("  失败 ✗ '二零二六年一月十三日十九点零三分十四秒'未被正确识别为整体日期时间");
                return false;
            }

            // 测试20：中文日期时间组合（中文数字日期+中文数字时间，含二十）
            var text20 = "二零二六年一月十三日十九点二十分十四秒";
            var result20 = segmenter.Cut(text20).ToList();
            var joined20 = string.Join("╱", result20);
            Console.WriteLine($"  测试20: {text20}");
            Console.WriteLine($"  结果: {joined20}");
            if (!result20.Contains("二零二六年一月十三日十九点二十分十四秒"))
            {
                Console.WriteLine("  失败 ✗ '二零二六年一月十三日十九点二十分十四秒'未被正确识别为整体日期时间");
                return false;
            }

            // 测试21：单独的中文时间表达式（不含日期，含中文数字分钟和秒）
            var text21 = "十九点二十分十四秒";
            var result21 = segmenter.Cut(text21).ToList();
            var joined21 = string.Join("╱", result21);
            Console.WriteLine($"  测试21: {text21}");
            Console.WriteLine($"  结果: {joined21}");
            if (!result21.Contains("十九点二十分十四秒"))
            {
                Console.WriteLine("  失败 ✗ '十九点二十分十四秒'未被正确识别为整体时间");
                return false;
            }

            // 测试22：十九点二十分（只有小时和分钟）
            var text22 = "十九点二十分";
            var result22 = segmenter.Cut(text22).ToList();
            var joined22 = string.Join("╱", result22);
            Console.WriteLine($"  测试22: {text22}");
            Console.WriteLine($"  结果: {joined22}");
            if (!result22.Contains("十九点二十分"))
            {
                Console.WriteLine("  失败 ✗ '十九点二十分'未被正确识别为整体时间");
                return false;
            }

            // 测试23：单独十九点（只有小时）
            var text23 = "十九点";
            var result23 = segmenter.Cut(text23).ToList();
            var joined23 = string.Join("╱", result23);
            Console.WriteLine($"  测试23: {text23}");
            Console.WriteLine($"  结果: {joined23}");
            if (!result23.Contains("十九点"))
            {
                Console.WriteLine("  失败 ✗ '十九点'未被正确识别为整体时间");
                return false;
            }

            // 测试24：非时间场景"零分"
            var text24 = "某人考试得了零分";
            var result24 = segmenter.Cut(text24).ToList();
            var joined24 = string.Join("╱", result24);
            Console.WriteLine($"  测试24: {text24}");
            Console.WriteLine($"  结果: {joined24}");
            // "零分"不应被识别为时间（没有"点"或"时"标识）
            if (result24.Any(w => w == "零分" && w.Contains("点")))
            {
                Console.WriteLine("  失败 ✗ '零分'不应被识别为时间");
                return false;
            }

            // 测试25：非时间场景"三分天下"
            var text25 = "三分天下";
            var result25 = segmenter.Cut(text25).ToList();
            var joined25 = string.Join("╱", result25);
            Console.WriteLine($"  测试25: {text25}");
            Console.WriteLine($"  结果: {joined25}");
            // "三分"不应被识别为时间（没有"点"或"时"标识）
            if (result25.Any(w => w.Contains("点") || w.Contains("时")))
            {
                Console.WriteLine("  失败 ✗ '三分'不应被识别为时间");
                return false;
            }

            // 测试26：中文MM:SS场景（只有分钟和秒，没有小时）
            var text26 = "再等十九分二十秒，就要结束考试了";
            var result26 = segmenter.Cut(text26).ToList();
            var joined26 = string.Join("╱", result26);
            Console.WriteLine($"  测试26: {text26}");
            Console.WriteLine($"  结果: {joined26}");
            if (!result26.Contains("十九分二十秒"))
            {
                Console.WriteLine("  失败 ✗ '十九分二十秒'未被正确识别为整体时间");
                return false;
            }

            // 测试27：阿拉伯数字MM:SS场景（只有分钟和秒，没有小时）
            var text27 = "再等19分20秒，就要结束考试了";
            var result27 = segmenter.Cut(text27).ToList();
            var joined27 = string.Join("╱", result27);
            Console.WriteLine($"  测试27: {text27}");
            Console.WriteLine($"  结果: {joined27}");
            if (!result27.Contains("19分20秒"))
            {
                Console.WriteLine("  失败 ✗ '19分20秒'未被正确识别为整体时间");
                return false;
            }

            Console.WriteLine("  通过 ✓");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }

    static bool TestDateTimePosSegment()
    {
        Console.WriteLine("[测试] 日期时间词性标注...");
        try
        {
            var posSeg = new PosSegmenter();

            // 测试1：时间格式 4:50
            var text1 = "今天4:50某某某领了一只记号笔";
            var result1 = posSeg.Cut(text1).ToList();
            var joined1 = string.Join("╱", result1);
            Console.WriteLine($"  测试1: {text1}");
            Console.WriteLine($"  结果: {joined1}");

            // 检查今天4:50是否被识别为时间词性（t）
            var hasTimeTag = result1.Any(p => p.Word.Contains("今天4:50") && p.Flag == "t");
            if (!hasTimeTag)
            {
                Console.WriteLine("  失败 ✗ '今天4:50'未被正确标记为时间词性");
                return false;
            }

            // 测试2：比值格式词性标注
            var text2 = "比值是100:31";
            var result2 = posSeg.Cut(text2).ToList();
            var joined2 = string.Join("╱", result2);
            Console.WriteLine($"  测试2: {text2}");
            Console.WriteLine($"  结果: {joined2}");

            // 检查100:31是否被识别为比值词性（n）
            var hasRatioTag = result2.Any(p => p.Word == "100:31" && p.Flag == "n");
            if (!hasRatioTag)
            {
                Console.WriteLine("  失败 ✗ '100:31'未被正确标记为比值词性（应为n）");
                return false;
            }

            // 测试3：时间格式词性标注
            var text3 = "时间是14:30";
            var result3 = posSeg.Cut(text3).ToList();
            var joined3 = string.Join("╱", result3);
            Console.WriteLine($"  测试3: {text3}");
            Console.WriteLine($"  结果: {joined3}");

            // 检查14:30是否被识别为时间词性（t）
            var hasTimeTag2 = result3.Any(p => p.Word == "14:30" && p.Flag == "t");
            if (!hasTimeTag2)
            {
                Console.WriteLine("  失败 ✗ '14:30'未被正确标记为时间词性（应为t）");
                return false;
            }

            Console.WriteLine("  通过 ✓");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }
    static bool TestLcut()
    {
        Console.WriteLine("[测试] lcut 直接返回 List<string>...");
        try
        {
            var seg = new JiebaSegmenter();
            var text = "我来到北京清华大学";
            var words = seg.Lcut(text);
            var joined = string.Join("╱", words);
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
            var joined = string.Join("╱", words);
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
            var joined = string.Join("╱", words);
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
            var joined = string.Join("╱", words);
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

            Console.WriteLine($"  tokenizer1: {string.Join("╱", words1)}");
            Console.WriteLine($"  tokenizer2: {string.Join("╱", words2)}");

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

    static bool TestHyphenatedWords()
    {
        Console.WriteLine("[测试] 连字符╱下划线连接单词分词...");
        try
        {
            var segmenter = new JiebaSegmenter();

            // 测试1：TF-IDF
            var text1 = "TF-IDF识别方法";
            var result1 = segmenter.Cut(text1).ToList();
            var joined1 = string.Join("╱", result1);
            Console.WriteLine($"  测试1: {text1}");
            Console.WriteLine($"  结果: {joined1}");
            if (!result1.Contains("TF-IDF"))
            {
                Console.WriteLine("  失败 ✗ 'TF-IDF'未被正确识别为整体");
                return false;
            }

            // 测试2：下划线连接
            var text2 = "word1_word2_word3";
            var result2 = segmenter.Cut(text2).ToList();
            var joined2 = string.Join("╱", result2);
            Console.WriteLine($"  测试2: {text2}");
            Console.WriteLine($"  结果: {joined2}");
            if (!result2.Contains("word1_word2_word3"))
            {
                Console.WriteLine("  失败 ✗ 'word1_word2_word3'未被正确识别为整体");
                return false;
            }

            // 测试3：连字符连接
            var text3 = "hello-world";
            var result3 = segmenter.Cut(text3).ToList();
            var joined3 = string.Join("╱", result3);
            Console.WriteLine($"  测试3: {text3}");
            Console.WriteLine($"  结果: {joined3}");
            if (!result3.Contains("hello-world"))
            {
                Console.WriteLine("  失败 ✗ 'hello-world'未被正确识别为整体");
                return false;
            }

            // 测试4：多个下划线
            var text4 = "test_case_example";
            var result4 = segmenter.Cut(text4).ToList();
            var joined4 = string.Join("╱", result4);
            Console.WriteLine($"  测试4: {text4}");
            Console.WriteLine($"  结果: {joined4}");
            if (!result4.Contains("test_case_example"))
            {
                Console.WriteLine("  失败 ✗ 'test_case_example'未被正确识别为整体");
                return false;
            }

            Console.WriteLine("  通过 ✓");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }

    static bool TestDomainNames()
    {
        Console.WriteLine("[测试] 域名╱URL分词...");
        try
        {
            var segmenter = new JiebaSegmenter();

            // 测试1：完整URL带https
            var text1 = "https://gitee.com/JTsamsde/AOTba";
            var result1 = segmenter.Cut(text1).ToList();
            var joined1 = string.Join("╱", result1);
            Console.WriteLine($"  测试1: {text1}");
            Console.WriteLine($"  结果: {joined1}");
            if (!result1.Contains("https://gitee.com/JTsamsde/AOTba"))
            {
                Console.WriteLine("  失败 ✗ 完整URL未被正确识别");
                return false;
            }

            // 测试2：完整URL带http
            var text2 = "http://www.baidu.com/search?q=test";
            var result2 = segmenter.Cut(text2).ToList();
            var joined2 = string.Join("╱", result2);
            Console.WriteLine($"  测试2: {text2}");
            Console.WriteLine($"  结果: {joined2}");
            if (!result2.Contains("http://www.baidu.com/search?q=test"))
            {
                Console.WriteLine("  失败 ✗ 带查询参数的URL未被正确识别");
                return false;
            }

            // 测试3：简写域名
            var text3 = "gitee.com";
            var result3 = segmenter.Cut(text3).ToList();
            var joined3 = string.Join("╱", result3);
            Console.WriteLine($"  测试3: {text3}");
            Console.WriteLine($"  结果: {joined3}");
            if (!result3.Contains("gitee.com"))
            {
                Console.WriteLine("  失败 ✗ 简写域名未被正确识别");
                return false;
            }

            // 测试4：简写域名带路径
            var text4 = "gitee.com/JTsamsde/AOTba";
            var result4 = segmenter.Cut(text4).ToList();
            var joined4 = string.Join("╱", result4);
            Console.WriteLine($"  测试4: {text4}");
            Console.WriteLine($"  结果: {joined4}");
            if (!result4.Contains("gitee.com/JTsamsde/AOTba"))
            {
                Console.WriteLine("  失败 ✗ 简写域名带路径未被正确识别");
                return false;
            }

            // 测试5：中文环境中的URL
            var text5 = "访问https://github.com查看代码";
            var result5 = segmenter.Cut(text5).ToList();
            var joined5 = string.Join("╱", result5);
            Console.WriteLine($"  测试5: {text5}");
            Console.WriteLine($"  结果: {joined5}");
            if (!result5.Contains("https://github.com"))
            {
                Console.WriteLine("  失败 ✗ 中文环境中的URL未被正确识别");
                return false;
            }

            // 测试6：中文环境中的简写域名带路径
            var text6 = "访问gitee.com/JTsamsde/AOTba查看代码";
            var result6 = segmenter.Cut(text6).ToList();
            var joined6 = string.Join("╱", result6);
            Console.WriteLine($"  测试6: {text6}");
            Console.WriteLine($"  结果: {joined6}");
            if (!result6.Contains("gitee.com/JTsamsde/AOTba"))
            {
                Console.WriteLine("  失败 ✗ 中文环境中的简写域名带路径未被正确识别");
                return false;
            }

            // 测试7：www域名
            var text7 = "www.baidu.com";
            var result7 = segmenter.Cut(text7).ToList();
            var joined7 = string.Join("╱", result7);
            Console.WriteLine($"  测试7: {text7}");
            Console.WriteLine($"  结果: {joined7}");
            if (!result7.Contains("www.baidu.com"))
            {
                Console.WriteLine("  失败 ✗ www域名未被正确识别");
                return false;
            }

            // 测试8：nuget.org
            var text8 = "nuget.org";
            var result8 = segmenter.Cut(text8).ToList();
            var joined8 = string.Join("╱", result8);
            Console.WriteLine($"  测试8: {text8}");
            Console.WriteLine($"  结果: {joined8}");
            if (!result8.Contains("nuget.org"))
            {
                Console.WriteLine("  失败 ✗ nuget.org未被正确识别");
                return false;
            }

            Console.WriteLine("  通过 ✓");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }

    static bool TestGB18030ExtendedCJK()
    {
        Console.WriteLine("[测试] GB18030-2022扩展B-I区生僻字分词...");
        try
        {
            var segmenter = new JiebaSegmenter();

            // 测试1：𰻝𰻝面（扩展G区字符，U+30EDD）
            var text1 = "我今天吃了𰻝𰻝面，很好吃";
            var result1 = segmenter.Cut(text1).ToList();
            var joined1 = string.Join("╱", result1);
            Console.WriteLine($"  测试1: {text1}");
            Console.WriteLine($"  结果: {joined1}");
            if (!result1.Contains("𰻝𰻝面"))
            {
                Console.WriteLine("  失败 ✗ '𰻝𰻝面'未被正确识别");
                return false;
            }

            // 测试2：𧒽岗（扩展B区字符，U+274BD）
            var text2 = "南海有轨电车一号线，起点为𧒽岗，终点为林岳东";
            var result2 = segmenter.Cut(text2).ToList();
            var joined2 = string.Join("╱", result2);
            Console.WriteLine($"  测试2: {text2}");
            Console.WriteLine($"  结果: {joined2}");
            if (!result2.Contains("𧒽岗"))
            {
                Console.WriteLine("  失败 ✗ '𧒽岗'未被正确识别");
                return false;
            }

            // 测试3：石𬒔（扩展E区字符，U+2C514）
            var text3 = "石𬒔是佛山市南海区桂城街道的一个地名";
            var result3 = segmenter.Cut(text3).ToList();
            var joined3 = string.Join("╱", result3);
            Console.WriteLine($"  测试3: {text3}");
            Console.WriteLine($"  结果: {joined3}");
            if (!result3.Contains("石𬒔"))
            {
                Console.WriteLine("  失败 ✗ '石𬒔'未被正确识别");
                return false;
            }

            // 测试4：混合场景
            var text4 = "从𧒽岗出发，经过石𬒔，最后去吃𰻝𰻝面";
            var result4 = segmenter.Cut(text4).ToList();
            var joined4 = string.Join("╱", result4);
            Console.WriteLine($"  测试4: {text4}");
            Console.WriteLine($"  结果: {joined4}");
            if (!result4.Contains("𧒽岗") || !result4.Contains("石𬒔") || !result4.Contains("𰻝𰻝面"))
            {
                Console.WriteLine("  失败 ✗ 混合场景生僻字未被正确识别");
                return false;
            }

            Console.WriteLine("  通过 ✓");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  异常: {ex.Message}");
            return false;
        }
    }
#endif
}
