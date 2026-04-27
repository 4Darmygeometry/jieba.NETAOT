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
            ("2025-06-30", "datetime"),
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
            ("春节", "festival"),
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

        // ========== 9. 按类型筛选演示 ==========
        Console.WriteLine("【场景九：按类型筛选】");
        var filterText = "项目截止2025-06-30，每周三下午2:30开会，" +
                        "使用v3.2.1版本，参考https://docs.example.com，" +
                        "北京时间九点整发布";
        var filterExpected = new[]
        {
            ("2025-06-30", "datetime"),
            ("周三下午2:30", "relativedate"),
            ("v3.2.1版本", "version"),
            ("https://docs.example.com", "domain"),
            ("北京时间", "timezone"),
            ("九点整", "time"),
        };
        RunTest(recognizer, filterText, filterExpected);

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
}
