using JiebaNet.Segmenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class TimeRecognizerDemo
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== AOTba ITimeRecognizer 实体提取演示 ===\n");

        ITimeRecognizer recognizer = new RegexTimeRecognizer();

        // ========== 1. 职场沟通场景 ==========
        Console.WriteLine("【场景一：项目排期会议】");
        var workText = "王总，需求评审定在下周四上午10点，开发周期约3个工作日，" +
                      "联调安排在Q2，最终版本用v2.5.0-rc2，" +
                      "deadline是2025-06-30，有问题随时找我，" +
                      "文档发你邮箱了，参考https://wiki.company.com/project-x";
        ExtractAndShow(recognizer, workText);

        // ========== 2. 社交聊天场景 ==========
        Console.WriteLine("【场景二：朋友约饭】");
        var chatText = "今晚8点半老地方见，我大概7:15下班，" +
                      "要是堵车就推迟到8点，" +
                      "对了，那家店在𧒽岗地铁站B口，" +
                      "上次吃的𰻝𰻝面真不错😋";
        ExtractAndShow(recognizer, chatText);

        // ========== 3. 电商客服场景 ==========
        Console.WriteLine("【场景三：售后沟通】");
        var serviceText = "亲，您的订单预计明天下午送达，" +
                         "物流显示已到佛山市南海区桂城街道转运中心，" +
                         "促销价是原价的85%，" +
                         "商品版本是2024款，" +
                         "有问题请联系www.taobao.com/shop/help";
        ExtractAndShow(recognizer, serviceText);

        // ========== 4. 技术讨论场景 ==========
        Console.WriteLine("【场景四：技术方案评审】");
        var techText = "CI构建耗时从14:30持续到15:45，" +
                      "TF-IDF阈值设为0.02，" +
                      "测试覆盖率要求达到99.9%，" +
                      "部署脚本在https://github.com/team/repo/blob/main/deploy.sh，" +
                      "当前运行的是v3.2.1-beta2，" +
                      "计划春节后上线";
        ExtractAndShow(recognizer, techText);

        // ========== 5. 家庭群聊场景 ==========
        Console.WriteLine("【场景五：家庭群通知】");
        var familyText = "妈，今年春节是2025年1月29日，" +
                        "我腊月二十八晚上9点的火车，" +
                        "大概十九点到北京西站，" +
                        "记得熬腊八粥，" +
                        "高铁票在12306.cn买的";
        ExtractAndShow(recognizer, familyText);

        // ========== 6. 新闻资讯场景 ==========
        Console.WriteLine("【场景六：新闻摘要】");
        var newsText = "新中国成立75周年庆典将于10月1日上午10点举行，" +
                      "届时北京时间同步直播，" +
                      "活动持续约2个小时，" +
                      "详情见www.cctv.com/2024/guoqing";
        ExtractAndShow(recognizer, newsText);

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
        ExtractAndShow(recognizer, mixedText);

        // ========== 8. 实体脱敏演示 ==========
        Console.WriteLine("=== 实体脱敏演示 ===\n");
        var sensitive = "张先生的身份证号是11010119900101xxxx，" +
                       "预约了明天上午9点的专家号，" +
                       "费用结算在www.hospital.com/pay，" +
                       "药品版本是v2.0-batch3";
        Console.WriteLine($"原文: {sensitive}");
        var entities = recognizer.Recognize(sensitive);
        var masked = MaskEntities(sensitive, entities);
        Console.WriteLine($"脱敏: {masked}\n");

        // ========== 9. 按类型筛选演示 ==========
        Console.WriteLine("=== 按类型筛选：仅提取时间实体 ===\n");
        var filterText = "项目截止2025-06-30，每周三下午2:30开会，" +
                        "使用v3.2.1版本，参考https://docs.example.com，" +
                        "北京时间九点整发布";
        var all = recognizer.Recognize(filterText);
        var timeOnly = all.Where(e =>
            e.Type is "datetime" or "time" or "relativedate" or
                      "timerange" or "deadline" or "timezone" or "weekday"
        ).OrderBy(e => e.Start);

        Console.WriteLine($"文本: {filterText}");
        foreach (var e in timeOnly)
        {
            Console.WriteLine($"  [{e.Start}-{e.End}] {e.Type,-12} => {e.Text}");
        }
    }

    static void ExtractAndShow(ITimeRecognizer recognizer, string text)
    {
        Console.WriteLine($"文本: {text}");
        var entities = recognizer.Recognize(text);

        if (entities.Count == 0)
        {
            Console.WriteLine("  → 未识别到实体");
        }
        else
        {
            foreach (var e in entities.OrderBy(x => x.Start))
            {
                Console.WriteLine($"  [{e.Start,3}-{e.End,3}] {e.Type,-12} => {e.Text}");
            }
        }
        Console.WriteLine();
    }

    static string MaskEntities(string text, List<TimeEntity> entities)
    {
        var sb = new System.Text.StringBuilder(text);
        // 倒序替换避免索引偏移
        foreach (var e in entities.OrderByDescending(x => x.Start))
        {
            sb.Remove(e.Start, e.End - e.Start);
            sb.Insert(e.Start, $"[{e.Type.ToUpper()}]");
        }
        return sb.ToString();
    }
}