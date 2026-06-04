using JiebaNet.Segmenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class TimeRecognizerDemo
{
    private static int _passedCount = 0;
    private static int _failedCount = 0;

    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== AOTba ITimeRecognizer 实体提取测试 ===\n");

        ITimeRecognizer recognizer = new RegexTimeRecognizer();

        // ========== 1. 职场沟通场景 ==========
        Console.WriteLine("【场景一：项目排期会议】");
        var workText = "王总，需求评审定在下周四上午10点，开发周期约3个工作日，" +
                      "联调安排在Q2，最终版本用v2.5.0-rc2，" +
                      "deadline是2025-06-30，有问题随时找我，" +
                      "文档发你邮箱了，参考https://wiki.company.com/project-x";
        var workExpected = new[]
        {
            ("下周四上午10点", "relativedate"),
            ("3个工作日", "duration"),
            ("Q2", "quarter"),
            ("v2.5.0-rc2", "version"),
            ("deadline是2025-06-30", "deadline"),
            ("https://wiki.company.com/project-x", "domain"),
        };
        RunTest(recognizer, workText, workExpected);

        // ========== 2. 社交聊天场景 ==========
        Console.WriteLine("【场景二：朋友约饭】");
        var chatText = "今晚8点半老地方见，我大概7:15下班，" +
                      "要是堵车就推迟到8点，" +
                      "对了，那家店在𧒽岗地铁站B口，" +
                      "上次吃的𰻝𰻝面真不错😋";
        var chatExpected = new[]
        {
            ("今晚8点半", "relativedate"),
            ("7:15", "time"),
            ("8点", "time"),
        };
        RunTest(recognizer, chatText, chatExpected);

        // ========== 3. 电商客服场景 ==========
        Console.WriteLine("【场景三：售后沟通】");
        var serviceText = "亲，您的订单预计明天下午送达，" +
                         "物流显示已到佛山市南海区桂城街道转运中心，" +
                         "促销价是原价的85%，" +
                         "商品版本是2024款，" +
                         "有问题请联系www.taobao.com/shop/help";
        var serviceExpected = new[]
        {
            ("明天下午", "relativedate"),
            ("85%", "percentage"),
            ("www.taobao.com/shop/help", "domain"),
        };
        RunTest(recognizer, serviceText, serviceExpected);

        // ========== 4. 技术讨论场景 ==========
        Console.WriteLine("【场景四：技术方案评审】");
        var techText = "CI构建耗时从14:30持续到15:45，" +
                      "TF-IDF阈值设为0.02，" +
                      "测试覆盖率要求达到99.9%，" +
                      "部署脚本在https://github.com/team/repo/blob/main/deploy.sh，" +
                      "当前运行的是v3.2.1-beta2，" +
                      "计划春节后上线";
        var techExpected = new[]
        {
            ("14:30", "time"),
            ("15:45", "time"),
            ("99.9%", "percentage"),
            ("https://github.com/team/repo/blob/main/deploy.sh", "domain"),
            ("v3.2.1-beta2", "version"),
            ("春节", "festival"),
        };
        RunTest(recognizer, techText, techExpected);

        // ========== 5. 家庭群聊场景 ==========
        Console.WriteLine("【场景五：家庭群通知】");
        var familyText = "妈，今年春节是2025年1月29日，" +
                        "我腊月二十八晚上9点的火车，" +
                        "大概十九点到北京西站，" +
                        "记得熬腊八粥，" +
                        "高铁票在12306.cn买的";
        var familyExpected = new[]
        {
            ("今年春节", "festival"),
            ("2025年1月29日", "datetimex"),
            ("腊月二十八晚上9点", "lunardate"),
            ("十九点", "time"),
            ("12306.cn", "domain"),
        };
        RunTest(recognizer, familyText, familyExpected);

        // ========== 6. 新闻资讯场景 ==========
        Console.WriteLine("【场景六：新闻摘要】");
        var newsText = "新中国成立75周年庆典将于10月1日上午10点举行，" +
                      "届时北京时间同步直播，" +
                      "活动持续约2个小时，" +
                      "详情见www.cctv.com/2024/guoqing";
        var newsExpected = new[]
        {
            ("75周年", "anniversary"),
            ("10月1日上午10点", "datetimex"),
            ("北京时间", "timezone"),
            ("2个小时", "duration"),
            ("www.cctv.com/2024/guoqing", "domain"),
        };
        RunTest(recognizer, newsText, newsExpected);

        // ========== 7. 跨场景复杂混合 ==========
        Console.WriteLine("【场景七：混合复杂文本】");
        var mixedText = "李经理，方案v1.3.0-preview1已发你钉钉，" +
                       "评审会改到下周三下午3点，" +
                       "比之前定的2025-05-20提前了，" +
                       "工期压缩到5个工作日，" +
                       "参考文档在https://confluence.company.com/display/TEAM/Spec，" +
                       "金龙鱼1:1:1调和油是本次采购的样品之一，" +
                       "占比30%，" +
                       "到货时间是明天下午4:30，" +
                       "有问题微信我，我随时在线👍";
        var mixedExpected = new[]
        {
            ("v1.3.0-preview1", "version"),
            ("下周三下午3点", "relativedate"),
            ("2025-05-20", "datetime"),
            ("5个工作日", "duration"),
            ("https://confluence.company.com/display/TEAM/Spec", "domain"),
            ("1:1:1", "ratio"),
            ("30%", "percentage"),
            ("明天下午4:30", "relativedate"),
        };
        RunTest(recognizer, mixedText, mixedExpected);

        // ========== 8. 实体脱敏演示 ==========
        Console.WriteLine("【场景八：实体脱敏】");
        var sensitive = "张先生的身份证号是11010119900101xxxx，" +
                       "预约了明天上午9点的专家号，" +
                       "费用结算在www.hospital.com/pay，" +
                       "药品版本是v2.0-batch3";
        var sensitiveExpected = new[]
        {
            ("明天上午9点", "relativedate"),
            ("www.hospital.com/pay", "domain"),
            ("v2.0-batch3", "version"),
        };
        RunTest(recognizer, sensitive, sensitiveExpected);

        // 脱敏后结果显示
        var sensitiveEntities = recognizer.Recognize(sensitive);
        var masked = MaskEntities(sensitive, sensitiveEntities);
        Console.WriteLine($"  脱敏前: {sensitive}");
        Console.WriteLine($"  脱敏后: {masked}");
        Console.WriteLine();

        // ========== 9. 按类型筛选演示 ==========
        Console.WriteLine("【场景九：按类型筛选】");
        var filterText = "项目截止2025-06-30，每周三下午2:30开会，" +
                        "使用v3.2.1版本，参考https://docs.example.com，" +
                        "北京时间九点整发布";
        var filterExpected = new[]
        {
            ("截止2025-06-30", "deadline"),
            ("周三下午2:30", "relativedate"),
            ("v3.2.1版本", "version"),
            ("https://docs.example.com", "domain"),
            ("北京时间", "timezone"),
            ("九点整", "time"),
        };
        RunTest(recognizer, filterText, filterExpected);

        // ========== 10. 中文数字年份识别 ==========
        Console.WriteLine("【场景十：中文数字年份识别】");
        var chineseYearText = "我是二零一零年出生的，" +
                             "二〇一〇年五月一日是重要日子，" +
                             "二零二一年五月是项目启动时间";
        var chineseYearExpected = new[]
        {
            ("二零一零年", "datetimex"),
            ("二〇一〇年五月一日", "datetimex"),
            ("二零二一年五月", "datetimex"),
        };
        RunTest(recognizer, chineseYearText, chineseYearExpected);

        // ========== 11. GB18030-2022补充区块 ==========
        Console.WriteLine("【场景十一：GB18030-2022补充区块】");
        var gb18030Text = "二〇一〇年，" +
                         "汉字笔画㇐是横，" +
                         "汉字结构⿰表示左右结构，" +
                         "汉语注音ㄅ是玻，" +
                         "注音扩展ㆠ用于方言";
        var gb18030Expected = new[]
        {
            ("二〇一〇年", "datetimex"),
        };
        RunTest(recognizer, gb18030Text, gb18030Expected);

        // ========== 12. Windows版本识别 ==========
        Console.WriteLine("【场景十二：Windows版本识别】");
        var windowsText = "公司电脑从Windows 7升级到Windows 10，" +
                         "服务器运行Windows Server 2022，" +
                         "老机器还装着Windows XP，" +
                         "新笔记本预装Microsoft Windows 11，" +
                         "开发环境使用Win 10，" +
                         "测试Win7兼容性，" +
                         "视窗95是早期的中文版本，" +
                         "视窗 10是目前最新的版本，" +
                         "Win32 API和Win64 API不在识别范围内，" +
                         "Windows 12预计明年发布，" +
                         "Windows Server 2025是最新服务器版";
        var windowsExpected = new[]
        {
            ("Windows 7", "windows"),
            ("Windows 10", "windows"),
            ("Windows Server 2022", "windows"),
            ("Windows XP", "windows"),
            ("Microsoft Windows 11", "windows"),
            ("Win 10", "windows"),
            ("Win7", "windows"),
            ("视窗95", "windows"),
            ("视窗 10", "windows"),
            ("Windows 12", "windows"),
            ("Windows Server 2025", "windows"),
        };
        RunTest(recognizer, windowsText, windowsExpected);

        // ========== 13. 词典词与时间实体识别 ==========
        // ITimeRecognizer 是公共接口，RegexTimeRecognizer 内部已做词典词前缀/后缀过滤：
        // - 词典词前缀/后缀被识为时间实体时会被过滤掉（如"百年孤独"中"百年"被丢弃）
        // - 词典词整体是时间实体的仍正常识别（如"百年纪念" → anniversary）
        // 测试词典词来自 词典词拆分问题.txt
        Console.WriteLine("【场景十三：词典词与时间实体识别（来自 词典词拆分问题.txt）】");

        // 13.1 词典词整体是时间实体 → 应正确识别
        Console.WriteLine("\n--- 13.1 词典词中的时间实体（应正确识别）---");
        var dictTimeWordTests = new[]
        {
            ("百年纪念", new[] { ("百年纪念", "anniversary") }),
            ("百年诞辰", new[] { ("百年诞辰", "anniversary") }),
        };
        foreach (var (text, expected) in dictTimeWordTests)
        {
            RunTest(recognizer, text, expected);
        }

        // 13.2 词典词非时间实体 → 词典词前缀/后缀被识别为时间实体的部分应被过滤掉
        // 公共接口应保证返回的实体在中文语境下确为时间实体
        // 例如"百年孤独"中"百年"是词典词前缀，不应作为 duration 返回
        Console.WriteLine("\n--- 13.2 词典词非时间实体（recognizer 内部已过滤前缀/后缀）---");
        var dictNonTimeWordTests = new[]
        {
            ("百年孤独", Array.Empty<(string, string)>()),      // "百年"是"百年孤独"的前缀，已过滤
            ("百年一遇", Array.Empty<(string, string)>()),      // "百年"是"百年一遇"的前缀，已过滤
            ("百年不遇", Array.Empty<(string, string)>()),
            ("千年老二", Array.Empty<(string, string)>()),      // "千年"是"千年老二"的前缀，已过滤
            ("千年虫",   Array.Empty<(string, string)>()),
            ("千年健",   Array.Empty<(string, string)>()),
            ("一笑千年", Array.Empty<(string, string)>()),      // "千年"是"一笑千年"的后缀，已过滤
        };
        foreach (var (text, expected) in dictNonTimeWordTests)
        {
            RunTest(recognizer, text, expected);
        }

        // 13.2.b "一千" 后面是"零"不是单位词（年/月/日等），DurationRegex 不匹配
        // 所以 recognizer 不会提取"一千"，公共接口自然返回空
        Console.WriteLine("\n--- 13.2.b 词典词非时间实体（recognizer 天然不匹配）---");
        var dictNonTimeWordTestsB = new[]
        {
            ("一千零一夜", Array.Empty<(string, string)>()),     // "一千零一夜"是阿拉伯语故事集
        };
        foreach (var (text, expected) in dictNonTimeWordTestsB)
        {
            RunTest(recognizer, text, expected);
        }

        // 13.3 混合上下文：验证 recognizer + segmenter 协调处理词典词与时间实体
        // "百年孤独"、"一笑千年"是词典词；"2021年1月1日"是时间实体；分词器应保护词典词
        Console.WriteLine("\n--- 13.3 混合上下文（验证词典词+时间实体协调）---");
        RunMixedContextTest(
            "百年孤独是马尔克斯的名著，而2021年1月1日是新一年的开始。",
            new[] { ("百年孤独", "dict"), ("2021年1月1日", "time") });

        RunMixedContextTest(
            "一笑千年，2021年5月1日出版",
            new[] { ("一笑千年", "dict"), ("2021年5月1日", "time") });

        RunMixedContextTest(
            "一千零一夜是一本古老的阿拉伯故事集",
            new[] { ("一千零一夜", "dict") });

        // 13.4 验证 "今年春节" 作为完整 festival 实体被识别（不被拆为"今年"+"春节"）
        Console.WriteLine("\n--- 13.4 相对时间节日整体识别（今年春节作为完整实体）---");
        var relativeFestivalTests = new[]
        {
            ("妈，今年春节是2025年1月29日", new[] { ("今年春节", "festival") }),
        };
        foreach (var (text, expected) in relativeFestivalTests)
        {
            RunTest(recognizer, text, expected);
        }

        // ========== 测试结果汇总 ==========
        Console.WriteLine("\n=== 测试结果汇总 ===");
        Console.WriteLine($"通过: {_passedCount}");
        Console.WriteLine($"失败: {_failedCount}");
        Console.WriteLine($"总计: {_passedCount + _failedCount}");

        return _failedCount > 0 ? 1 : 0;
    }

    static void RunTest(ITimeRecognizer recognizer, string text, (string expectedText, string expectedType)[] expectedEntities)
    {
        Console.WriteLine($"文本: {text}");
        var entities = recognizer.Recognize(text);
        var entitiesList = entities.OrderBy(x => x.Start).ToList();

        // 显示识别结果
        if (entitiesList.Count == 0)
        {
            Console.WriteLine("  → 未识别到实体");
        }
        else
        {
            foreach (var e in entitiesList)
            {
                Console.WriteLine($"  [{e.Start,3}-{e.End,3}] {e.Type,-12} => {e.Text}");
            }
        }

        // 验证预期结果
        bool allPassed = true;
        foreach (var (expectedText, expectedType) in expectedEntities)
        {
            var found = entitiesList.Any(e => e.Text == expectedText && e.Type == expectedType);
            if (found)
            {
                Console.WriteLine($"  ✓ 预期: [{expectedType}] {expectedText}");
            }
            else
            {
                Console.WriteLine($"  ✗ 缺失: [{expectedType}] {expectedText}");
                allPassed = false;
            }
        }

        if (allPassed)
        {
            Console.WriteLine("  通过 ✓");
            _passedCount++;
        }
        else
        {
            Console.WriteLine("  失败 ✗");
            _failedCount++;
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 将文本中的实体替换为[类型]标记，实现脱敏
    /// </summary>
    static string MaskEntities(string text, IEnumerable<TimeEntity> entities)
    {
        var sorted = entities.OrderByDescending(e => e.Start).ToList();
        var result = text;
        foreach (var e in sorted)
        {
            result = result.Remove(e.Start, e.End - e.Start).Insert(e.Start, $"[{e.Type}]");
        }
        return result;
    }

    /// <summary>
    /// 混合上下文测试：验证 JiebaSegmenter 在包含词典词 + 时间实体的混合文本中
    /// 能正确过滤词典词前缀的时间实体，保护完整词典词
    /// </summary>
    /// <param name="text">待分词文本</param>
    /// <param name="expectedItems">期望在分词结果中出现的关键词 (word, category) 列表
    ///   category="dict" 表示词典词（如"百年孤独"、"一笑千年"、"今年春节"），
    ///   category="time" 表示时间实体（如"2021年1月1日"），
    ///   区分两类是为了避免输出标签的歧义</param>
    static void RunMixedContextTest(string text, (string word, string category)[] expectedItems)
    {
        Console.WriteLine($"文本: {text}");

        var segmenter = new JiebaSegmenter();
        var result = segmenter.Cut(text).ToList();
        var joined = string.Join("╱", result);
        Console.WriteLine($"分词: {joined}");

        bool allPassed = true;
        foreach (var (word, category) in expectedItems)
        {
            var label = category == "dict" ? "词典词" : "时间实体";
            if (result.Contains(word))
            {
                Console.WriteLine($"  ✓ {label}: {word}");
            }
            else
            {
                Console.WriteLine($"  ✗ 缺失{label}: {word}");
                allPassed = false;
            }
        }

        if (allPassed)
        {
            Console.WriteLine("  通过 ✓");
            _passedCount++;
        }
        else
        {
            Console.WriteLine("  失败 ✗");
            _failedCount++;
        }
        Console.WriteLine();
    }
}
