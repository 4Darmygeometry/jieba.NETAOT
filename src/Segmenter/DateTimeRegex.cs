// 所有框架都使用正则表达式识别日期时间

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JiebaNet.Segmenter
{
    /// <summary>
    /// 基于正则表达式的时间识别器
    /// 所有框架统一使用
    /// </summary>
    public class RegexTimeRecognizer : ITimeRecognizer
    {
        // ========== 简繁通用字符（不含方括号，使用时在外层加[]） ==========
        private const string C_节 = "节節";
        private const string C_阳 = "阳陽";
        private const string C_点 = "点點";
        private const string C_时 = "时時";
        private const string C_钟 = "钟鐘";
        private const string C_后 = "后後";
        private const string C_周 = "周週";
        private const string C_这 = "这這";
        private const string C_个 = "个個";
        private const string C_礼 = "礼禮";
        private const string C_双 = "双雙";
        private const string C_内 = "内內";
        private const string C_腊 = "腊臘";
        private const string C_闰 = "闰閏";
        private const string C_龙 = "龙龍";
        private const string C_马 = "马馬";
        private const string C_鸡 = "鸡雞";
        private const string C_猪 = "猪豬";
        private const string C_国 = "国國";
        private const string C_庆 = "庆慶";
        private const string C_劳 = "劳勞";
        private const string C_动 = "动動";
        private const string C_儿 = "儿兒";
        private const string C_妇 = "妇婦";
        private const string C_师 = "师師";
        private const string C_亲 = "亲親";
        private const string C_圣 = "圣聖";
        private const string C_诞 = "诞誕";
        private const string C_兰 = "兰蘭";
        private const string C_黄 = "黄黃";
        private const string C_晓 = "晓曉";
        private const string C_现 = "现現";
        private const string C_当 = "当當";
        private const string C_间 = "间間";
        private const string C_纪 = "纪紀";
        private const string C_续 = "续續";
        private const string C_长 = "长長";
        private const string C_费 = "费費";
        private const string C_占 = "占佔";
        private const string C_经 = "经經";
        private const string C_历 = "历歷";
        private const string C_过 = "过過";
        private const string C_将 = "将將";
        private const string C_为 = "为為";
        private const string C_约 = "约約";
        private const string C_学 = "学學";
        private const string C_属 = "属屬";
        private const string C_种 = "种種";
        private const string C_季 = "季";
        private const string C_东 = "东東";
        private const string C_汉 = "汉漢";
        private const string C_谷 = "谷穀";
        private const string C_满 = "满滿";
        private const string C_处 = "处處";
        private const string C_纽 = "纽紐";
        private const string C_伦 = "伦倫";
        private const string C_华 = "华華";
        private const string C_寿 = "寿壽";
        private const string C_财 = "财財";
        // ========== 简繁通用词语（使用|分隔，直接用于正则） ==========
        private const string C_惊蛰 = "惊蛰|驚蟄";
        private const string C_软件 = "软件|軟體";
        private const string C_系统 = "系统|系統";
        private const string C_固件 = "固件|韌體";
        private const string C_应用 = "应用|應用";
        private const string C_程序 = "程序|程式";
        private const string C_产品 = "产品|產品";
        private const string C_当前 = "当前|當前";
        private const string C_旧 = "旧|舊";
        private const string C_稳定 = "稳定|穩定";
        private const string C_测试 = "测试|測試";
        private const string C_开发 = "开发|開發";
        private const string C_预览 = "预览|預覽";
        private const string C_候选 = "候选|候選";
        // ========== 版本号共有部分 ==========
        // 版本号核心（如1.0、3.2.1）
        private const string V_Core = @"\d+\.\d+(?:\.\d+)?";
        // 预发布标签（如-alpha1、-beta2、-rc1）
        private const string V_Prerelease = @"(?:-(?:alpha|beta|rc|preview|pre|dev|snapshot|release|build|hotfix|patch|major|minor|final|batch)\d*)?";
        // 中文后缀（如版本、版、旧版本）- 使用静态只读字段因为引用了非常量属性
        private static readonly string V_ChineseSuffix = @"(?:" + GB18030_2022.ChineseQuantifierPattern + @"*?版(?:本?))";

        // ========== 1. ISO 日期时间 ==========
        // 匹配格式：2021-01-01, 2021/01/01, 2021-01-01 09:00:00, 2021-01-01T09:00:00Z 等
        private static readonly Regex DateTimeIsoRegex = new(
            @"(?:^|(?<![\d\-/\.]))(?<year>\d{4})(?<sep>[-/\.])(?<month>\d{1,2})\k<sep>(?<day>\d{1,2})" +
            @"(?:\s+(?<hour>\d{1,2}):(?<minute>\d{2})(?::(?<second>\d{2}))?)?(?![\d\-/\.])" +
            @"|(?:^|(?<![\d\-/\.T]))\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:?\d{2})?(?![\d\-/\.T])",
            RegexOptions.Compiled);

        // ========== 2. 中文日期时间（支持纯日期与日期时间组合） ==========
        // 匹配格式：
        // - 纯日期：2021年1月1日, 二〇二一年一月一日, 12月25日
        // - 日期+时间：2026年1月13日19点03分14秒、二零二六年一月十三日十九点零三分十四秒
        // - 日期+上下午+时间：10月1日上午10点、2026年4月30日晚上9点
        // 注意：日/号是可选的，时间部分也是可选的
        // 注意：中文数字的分钟和秒需要包含零，如"零三分"、"十四秒"
        // 注意：时间部分前可加上午/下午/晚上/凌晨等时间段修饰
        private static readonly Regex DateTimeChineseRegex = new(
            @"(?:^|(?<![\d一二三四五六七八九十〇零]))" +
            @"(?:(?<year>\d{4}|[一二三四五六七八九十〇零]{4})年)?" +
            @"(?<month>\d{1,2}|[一二三四五六七八九十]{1,3})月" +
            @"(?<day>\d{1,2}|[一二三四五六七八九十]{1,3})[日号號]?" +
            @"(?:\s*(?<ampm>上午|下午|早上|晚上|凌晨|[" + C_后 + @"]午|傍晚|黄昏)?\s*" +
            @"(?<hour>\d{1,2}|[一二三四五六七八九十零]{1,3})[" + C_点 + C_时 + @"]" +
            @"(?:\s*(?<minute>\d{1,2}|[一二三四五六七八九十零]{1,3}|半|一刻|三刻|整)[分]?)?" +
            @"(?:\s*(?<second>\d{1,2}|[一二三四五六七八九十零]{1,3})秒)?)?" +
            @"(?![\d一二三四五六七八九十〇零])",
            RegexOptions.Compiled);

        // ========== 3. 农历 ==========
        // 注意：农历日期后可跟时间段+时间（如"腊月二十八晚上9点"）
        private static readonly Regex LunarRegex = new(
            @"(?:农历|阴历|農曆)?" +
            @"(?:[甲乙丙丁戊己庚辛壬癸][子丑寅卯辰巳午未申酉戌亥][" + C_时 + @"])?" +
            @"(?:[鼠牛虎兔" + C_龙 + "蛇" + C_马 + "羊猴" + C_鸡 + "狗" + C_猪 + "][" + C_时 + @"])?" +
            @"(?<lunar>[" + C_闰 + @"]?[正一二三四五六七八九十冬" + C_腊 + "]{1,2}月" +
            @"(?:初[一二三四五六七八九十]|三十|[廿二]十[一二三四五六七八九]?|十[一二三四五六七八九十]?|[廿二][一二三四五六七八九十]))" +
            @"[日号號]?" +
            @"(?:\s*(?<ampm>上午|下午|早上|晚上|凌晨|[" + C_后 + @"]午|傍晚|黄昏)?\s*" +
            @"(?<hour>\d{1,2}|[一二三四五六七八九十零]{1,3})[" + C_点 + C_时 + @"]" +
            @"(?:\s*(?<minute>\d{1,2}|[一二三四五六七八九十零]{1,3}|半|一刻|三刻|整)[分]?)?" +
            @"(?:\s*(?<second>\d{1,2}|[一二三四五六七八九十零]{1,3})秒)?)?",
            RegexOptions.Compiled);

        // ========== 4. 节日 ==========
        // 注意：传统节日和现代节日统一用C_节后缀合并
        // 注意：七夕、小年、除夕、元旦可带"节"也可不带
        // 注意：清明节是节日，清明是节气（在SolarTermRegex中）
        // 注意：使用单字C变量支持繁体（如重陽節、臘八節、婦女節等）
        private static readonly Regex FestivalRegex = new(
            @"(?:春|元宵|端午|中秋|重[" + C_阳 + @"]|[" + C_腊 + @"]八|情人|[" + C_妇 + @"]女|[" + C_劳 + @"][" + C_动 + @"]|[" + C_儿 + @"]童|教[" + C_师 + @"]|[" + C_国 + @"][" + C_庆 + @"]|[" + C_圣 + @"][" + C_诞 + @"]|感恩|母[" + C_亲 + @"]|父[" + C_亲 + @"])[" + C_节 + @"]|清明[" + C_节 + @"]|(?:七夕|小年|除夕|元旦)(?:[" + C_节 + @"])?",
            RegexOptions.Compiled);

        // ========== 5. 节气 ==========
        // 注意：清明是节气，清明节是节日（在FestivalRegex中）
        private static readonly Regex SolarTermRegex = new(
            @"立春|雨水|" + C_惊蛰 + @"|春分|清明|[" + C_谷 + @"]雨|" +
            @"立夏|小[" + C_满 + @"]|芒[" + C_种 + @"]|夏至|小暑|大暑|" +
            @"立秋|[" + C_处 + @"]暑|白露|秋分|寒露|霜降|立冬|小雪|大雪|冬至|小寒|大寒",
            RegexOptions.Compiled);

        // ========== 6. 时间（严格格式：HH:MM:SS.ffff、HH:MM:SS、HH:MM，时分秒60进制） ==========
        // 格式1：HH:MM:SS.ffff（如 14:30:00.123）
        // 格式2：HH:MM:SS（如 14:30:00）
        // 格式3：HH:MM（如 4:50, 14:30）
        // 格式4：中文时间表达（如 上午9点, 下午3点半, 晚上8点30分, 凌晨2点，上午六点整，十九点二十分十四秒）
        // 格式5：中文分钟秒格式（只有分钟和秒，没有小时，如 十九分二十秒、19分20秒）
        // 注意：小时0-23（1-2位），分钟必须2位补0（00-59），秒必须2位补0（00-59），毫秒任意位数
        // 注意：分钟/秒不补0的视为比值而非时间（如1:1:1是比值，1:01:01才是时间）
        // 注意：负向前瞻需要排除小数点，确保"14:30"后面不跟小数点（除非是毫秒部分）
        // 注意：中文数字的分钟和秒需要包含零，如"零三分"、"十四秒"、"二十分"
        // 注意：分钟秒格式需要排除"三分天下"、"零分"等非时间场景
        private static readonly Regex TimeRegex = new(
            // 格式1：HH:MM:SS.ffff（分钟秒必须2位补0）
            @"(?:^|(?<![\d.]))" +
            @"(?<hour>[01]?\d|2[0-3]):(?<minute>[0-5]\d):(?<second>[0-5]\d)(?:\.(?<millisecond>\d+))" +
            @"(?![\d.])|" +
            // 格式2：HH:MM:SS（分钟秒必须2位补0）
            @"(?:^|(?<![\d.]))" +
            @"(?<hour>[01]?\d|2[0-3]):(?<minute>[0-5]\d):(?<second>[0-5]\d)" +
            @"(?![\d.])|" +
            // 格式3：HH:MM（分钟必须2位补0）
            @"(?:^|(?<![\d.]))" +
            @"(?<hour>[01]?\d|2[0-3]):(?<minute>[0-5]\d)" +
            @"(?![\d.:])|" +
            // 格式4：中文时间表达（小时+分钟+秒 或 小时+分钟 或 小时）
            @"(?<ampm>上午|下午|早上|晚上|凌晨|[" + C_后 + @"]午|傍晚|黄昏)?\s*" +
            @"(?<hour>\d{1,2}|[一二三四五六七八九十]{1,3})[" + C_点 + C_时 + @"]" +
            @"(?:\s*(?<minute>\d{1,2}|[一二三四五六七八九十零]{1,3}|半|一刻|三刻|整)[分]?)?(?:\s*(?<second>\d{1,2}|[一二三四五六七八九十零]{1,3})秒)?" +
            @"(?![一二三四五六七八九十零])|" +
            // 格式5：分钟秒格式（只有分钟和秒，没有小时）
            @"(?<![" + C_点 + C_时 + @"])" +
            @"(?<minute>\d{1,2}|[一二三四五六七八九十]{1,2}|[一二三四五]十[一二三四五六七八九]?)分" +
            @"(?<second>\d{1,2}|[一二三四五六七八九十]{1,2}|[一二三四五]十[一二三四五六七八九]?)秒" +
            @"(?![一二三四五六七八九十零分秒\d])",
            RegexOptions.Compiled);

        // ========== 6.5 比值（任意数字:数字格式，支持小数） ==========
        // 格式：数字:数字 或 数字:数字:数字（如 100:31, 3:1, 1:1:1, 1:1.618）
        // 注意：比值和时间格式相同，但比值不限制数字范围，支持小数
        // 注意：负向前瞻需要排除小数点，确保"1:1.618"完整匹配而非"1:1"
        private static readonly Regex RatioRegex = new(
            @"(?:^|(?<![\d.]))" +
            @"(?<num1>\d+(?:\.\d+)?):(?<num2>\d+(?:\.\d+)?)(?::(?<num3>\d+(?:\.\d+)?))?" +
            @"(?![\d.])",
            RegexOptions.Compiled);

        // ========== 7. 相对时间（支持组合：明天下午3点、昨天上午9点、今天晚上8点、今天4:50、今晚8点半、明天下午等） ==========
        // 注意：星期模式中'周'必须后跟星期几数字，避免'周年'中的'周'被误匹配
        // 注意：支持"今晚8点半"、"明早7点"等含早晚的组合
        // 注意：支持"明天下午"、"今天上午"等只有上下午没有具体时间点的组合
        private static readonly Regex RelativeRegex = new(
            @"(?:[" + C_这 + @"]天|今天|明天|[" + C_后 + @"]天|昨天|前天|大前天|大[" + C_后 + @"]天|" +
            @"今晚|明晚|昨晚|今早|明早|昨早|" +
            @"[" + C_现 + @"]在|目前|[" + C_当 + @"]前|此刻|[" + C_这 + @"][" + C_时 + @"]|" +
            @"(?:上|本|[" + C_这 + @"]|下)?(?:星期|[" + C_礼 + @"]拜)[一二三四五六日天]?|" +
            @"(?:上|本|[" + C_这 + @"]|下)?[" + C_周 + @"][一二三四五六日天]|" +
            @"(?:上|本|[" + C_这 + @"]|下)[" + C_个 + @"]?月|" +
            @"(?:大?[前[" + C_这 + @"]今明[" + C_后 + @"]]年))" +
            // 可选组合时间：支持三种格式
            // 格式1：中文时间（如"明天下午3点"、"昨天上午9点"、"今天上午六点整"、"今晚8点半"）
            @"(?:\s*(?:上午|下午|早上|晚上|凌晨|[" + C_后 + @"]午|傍晚|黄昏)?\s*(?:\d{1,2}|[一二三四五六七八九十]{1,3})[" + C_点 + C_时 + @"]" +
            @"(?:\s*(?:\d{1,2}|半|一刻|三刻|整)[分]?)?(?:\s*\d{1,2}秒)?" +
            // 格式2：数字时间（如"今天4:50"、"明天14:30:00"）
            @"|\s*(?:上午|下午|早上|晚上|凌晨|[" + C_后 + @"]午|傍晚|黄昏)?\s*\d{1,2}:\d{2}(?::\d{2})?" +
            // 格式3：只有上下午/早晚（如"明天下午"、"今天上午"、"今晚"），不需要具体时间点
            @"|\s*(?:上午|下午|早上|晚上|凌晨|[" + C_后 + @"]午|傍晚|黄昏))?",
            RegexOptions.Compiled);

        // ========== 8. 持续时间 ==========
        // 注意：'周'单位需要排除'周年'（纪念日），避免DurationRegex抢占AnniversaryRegex的匹配
        // 注意：'年'单位需要排除后面跟'第X季度'的情况，避免DurationRegex抢占QuarterRegex的匹配
        private static readonly Regex DurationRegex = new(
            @"(?:\d+(?:\.\d+)?|[一二三四五六七八九十百千]+)\s*" +
            @"(?:年(?!第?[一二三四1234][" + C_季 + C_节 + @"]度|Q[1234])|[" + C_个 + @"]个月|月|[" + C_周 + @"](?!年)|天|日|[" + C_个 + @"]?小[" + C_时 + @"]|分[" + C_钟 + @"]|秒[" + C_钟 + @"]|毫秒|[" + C_个 + @"]?工作日)",
            RegexOptions.Compiled);

        // ========== 9. 模糊时间 ==========
        private static readonly Regex FuzzyRegex = new(
            @"(?:上|[" + C_这 + @"]|本)世[" + C_纪 + @"]\d{0,4}年代?|年初|年中|年末|月底|月初|上旬|中旬|下旬|" +
            @"工作日|[" + C_周 + @"]末|[" + C_双 + @"]休日|[" + C_礼 + @"]拜天|[" + C_周 + @"][" + C_内 + @"]" +
            @"|非[" + C_周 + @"]末|[" + C_节 + @"]假日|平[" + C_时 + @"]|平日|非[" + C_节 + @"]假日|" +
            @"大清早|一大早|大早|上午[" + C_时 + @"]分|中午[" + C_时 + @"]分|午[" + C_后 + @"][" + C_时 + @"]分|下午[" + C_时 + @"]分|傍晚[" + C_时 + @"]分|晚上[" + C_时 + @"]分|深夜[" + C_时 + @"]分|半夜三更",
            RegexOptions.Compiled);

        // ========== 10. 时间范围 ==========
        private static readonly Regex RangeRegex = new(
            @"(?:從从|自)?\s*" +
            @"(?<start>\d{1,2}:\d{2}(?::\d{2})?|\d{4}[-/年]\d{1,2}[-/月]\d{1,2}[日号號]?|今天|明天|昨天|[上下]午\d{1,2}[" + C_点 + C_时 + @"])" +
            @"\s*(?:到|至|~|-|—|→|直到)\s*" +
            @"(?<end>\d{1,2}:\d{2}(?::\d{2})?|\d{4}[-/年]\d{1,2}[-/月]\d{1,2}[日号號]?|今天|明天|昨天|[上下]午\d{1,2}[" + C_点 + C_时 + @"])",
            RegexOptions.Compiled);

        // ========== 11. 干支+生肖 ==========
        // 注意：生肖必须跟"时"或"年"，避免单独的"猴"等被误识别
        private static readonly Regex TraditionalRegex = new(
            @"[甲乙丙丁戊己庚辛壬癸][子丑寅卯辰巳午未申酉戌亥][" + C_时 + @"]?|" +
            @"[鼠牛虎兔" + C_龙 + "蛇" + C_马 + "羊猴" + C_鸡 + "狗" + C_猪 + "](?:[" + C_时 + @"]|年)|属[鼠牛虎兔" + C_龙 + "蛇" + C_马 + "羊猴" + C_鸡 + "狗" + C_猪 + @"]",
            RegexOptions.Compiled);

        // ========== 12. 截止时间 ==========
        private static readonly Regex DeadlineRegex = new(
            @"(?:截止|截至|Deadline|DDL|deadline|最后期限|期限|到期|[" + C_过 + @"]期|失效)\s*(?:日期|[" + C_时 + @"][" + C_间 + @"])?\s*" +
            @"(?::|：|是|为|在|到|至)?\s*" +
            @"(?<date>\d{4}[-/年]\d{1,2}[-/月]\d{1,2}[日号號]?|\d{1,2}[-/月]\d{1,2}[日号號]?|今天|明天|后天|下周[一二三四五六日天]?|[上下]个月\d{1,2}[日号號]?)",
            RegexOptions.Compiled);

        // ========== 13. 季度 ==========
        // 注意：支持'季度'（季）和'节度'（节/節），'2024年第一季度'和'2024年Q1'应作为整体匹配
        private static readonly Regex QuarterRegex = new(
            @"(?<year>\d{4})?年第?(?<quarter>[一二三四1234])[" + C_季 + C_节 + @"]度?|" +
            @"(?<year>\d{4})?年Q(?<quarter>[1234])|" +
            @"Q(?<quarter>[1234])|" +
            @"第?(?<quarter>[一二三四1234])[" + C_季 + C_节 + @"]度|" +
            @"第[一二三四1234][" + C_财 + @"][" + C_季 + C_节 + @"]",
            RegexOptions.Compiled);

        // ========== 14. 星期 ==========
        // 注意：英文星期名称需要添加单词边界，避免匹配到其他单词的一部分（如"GitHub"中的"tHu"被匹配为"Thu"）
        private static readonly Regex WeekdayRegex = new(
            @"(?:星期|[" + C_礼 + @"]拜|[" + C_周 + @"])(?<day>[一二三四五六日天1234567])|" +
            @"\b(?<day>Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday|Mon|Tue|Wed|Thu|Fri|Sat|Sun)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ========== 15. 时区 ==========
        // 注意：'北京时间'需完整匹配'北京+时间/時間'，避免被分词器拆成'北京时/间'
        // 注意：城市名统一用C_时C_间后缀合并
        // 注意：东西区合并，支持中文数字和阿拉伯数字（如东八区、东8区、西12区）
        private static readonly Regex TimezoneRegex = new(
            @"UTC[+-]\d{1,2}(?::?\d{2})?|GMT[+-]?\d{1,2}?|CST|EST|PST|MST|JST|IST|CET|" +
            @"(?:北京|[" + C_东 + @"]京|[" + C_纽 + @"][" + C_约 + @"]|[" + C_伦 + @"]敦|巴黎|悉尼|莫斯科)[" + C_时 + @"][" + C_间 + @"]?|" +
            @"[" + C_东 + @"|西](?:[一二三四五六七八九十]{1,2}|\d{1,2})区",
            RegexOptions.Compiled);

        // ========== 16. 纪念日 ==========
        // 匹配"数字+周年/岁/寿"等纪念日表达式
        // 精确模式下"60周年"作为整体，搜索引擎模式下分开为"60"和"周年"
        private static readonly Regex AnniversaryRegex = new(
            @"(?<num>\d+|[一二三四五六七八九十百]+)\s*" +
            @"(?<unit>岁|周岁|虚岁|周年|年[" + C_纪 + @"]念|年[" + C_庆 + @"]|年[" + C_诞 + @"]辰|年忌辰|年祭|[" + C_华 + @"][" + C_诞 + @"]|[" + C_寿 + @"]辰|大[" + C_寿 + @"])",
            RegexOptions.Compiled);

        // ========== 17. 朝代 ==========
        // 注意：所有朝代名称都需要后跟'朝'或'代'才识别，避免与普通词汇冲突
        // 例如：商鞅变法（商）、隋唐演义（隋、唐）、汉字（汉）等
        private static readonly Regex DynastyRegex = new(
            @"(?:夏|商|秦|[" + C_汉 + @"]|三[" + C_国 + @"]|晋|南北朝|隋|唐|五代|周|宋|元|明|清|民[" + C_国 + @"])(?:朝|代)",
            RegexOptions.Compiled);

        // ========== 18. 百分比 ==========
        // 格式：数字% 或 数字.数字%（如 5%, 99.99%, 100.5%）
        // 注意：百分比优先级高于版本号，确保"99.99%"被识别为百分比而非版本号
        private static readonly Regex PercentageRegex = new(
            @"\d+(?:\.\d+)?%",
            RegexOptions.Compiled);

        // ========== 19. 版本号 ==========
        // 格式：v1.0.1、1.0.1、3.2-preview1、4.1.2-rc1、2.1-alpha1、6.3-beta2
        // 注意：版本号至少两个数字部分，可带预发布标签
        // 注意：版本号后面不能跟"%"（百分比）或其他非版本号字符
        // 注意：纯数字如1.0、2.0、2.10不应被识别为版本号，除非有上下文：
        //   - 前置上下文：版本1.0、版本号2.0
        //   - 后置上下文：1.0版本、3.0版、5.2旧版本、2.1老版本、2.2xx版本
        //   - v/V前缀：v1.0.1、V2.0
        // 注意：v/V前缀的版本号后面可跟中文上下文（如v3.2.1版本），需整体匹配
        // 注意：中文部分使用ChineseQuantifierPattern匹配不定长中文（含GB18030-2022扩展B-I区生僻字）
        // 注意：使用V_Core、V_Prerelease、V_ChineseSuffix常量避免重复
        private static readonly Regex VersionRegex = new(
            // 模式1：v/V前缀版本号（中文上下文可选）
            @"(?:v|V)" + V_Core + V_Prerelease + V_ChineseSuffix + @"?" +
            @"(?![\d.%])" +
            // 模式2：无前缀版本号（必须有中文上下文）
            @"|" + V_Core + V_Prerelease + V_ChineseSuffix +
            @"(?![\d.%])" +
            // 模式3：前置固定中文上下文+版本号（如版本1.0、候选版本4.1.2-rc1）
            // 注意：前置上下文只能匹配固定词汇，不能无限贪婪匹配任意中文字符
            @"|(?:" +
            @"版本(?:号?|名称|标识)|" +
            @"(?:" + C_软件 + @"|" + C_系统 + @"|" + C_固件 + @"|" + C_应用 + @"|" + C_程序 + @"|" + C_产品 + @"|" +
            C_当前 + @"|最新|[" + C_旧 + @"]|老|新|" + C_稳定 + @"|" + C_测试 + @"|" + C_开发 + @"|" + C_预览 + @"|" + C_候选 + @"|" +
            @"(?:alpha|beta|rc|release))版本" +
            @")" + V_Core + V_Prerelease +
            @"(?![\d.%])",
            RegexOptions.Compiled);

        // ========== 20. 域名/URL ==========
        // 格式：https://gitee.com/JTsamsde/AOTba、http://www.baidu.com/search?q=test
        //       www.example.com/path、example.com/page、sub.example.com、nuget.org
        // 注意：域名至少包含一个点号，顶级域名至少2个字母
        // 注意：完整URL包含可选的协议前缀（https://或http://）和可选的路径部分
        // 注意：路径部分以/开头，直到遇到空格或中文字符为止
        // 注意：域名边界：前面是中文、空格等非英文数字字符即为左边界
        // 注意：域名长度限制：每个段最大63字符，总长度最大253字符（正则限制每段，总长度在代码验证）
        // 注意：域名中大小写没有区分
        // 注意：使用负向前瞻和后瞻来确保边界正确，支持中文边界
        // 注意：中文范围使用GB18030-2022标准（基本区至扩展I区）
        // 注意：路径部分排除空格、BMP中文和CJK代理对高位（扩展B-I区）
        // 注意：路径部分排除中文标点（如逗号、句号、顿号等），避免域名末尾误包含标点
        private static readonly Regex DomainRegex = new(
            @"(?<![a-zA-Z0-9])(?:https?://)?(?:[a-zA-Z0-9](?:[-a-zA-Z0-9]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}(?:/[^\s\uD840-\uD87F\uD880-\uD888" + GB18030_2022.ChineseRangeForCharClass + @"，。、；：！？""''）】》]*)?(?![a-zA-Z0-9])",
            RegexOptions.Compiled);

        // ========== 21. 连字符/下划线连接的单词 ==========
        // 格式：TF-IDF、word1_word2_word3、hello-world、test_case_example
        // 注意：至少包含一个连字符或下划线，且至少有两个单词部分
        // 注意：每个单词部分只能是字母或数字（不包括中文）
        // 注意：每个单词部分至少需要2个字符，排除单字母缩写（如P-R、A-B）
        // 注意：使用负向前瞻和后瞻来确保边界正确，支持中文边界
        private static readonly Regex HyphenatedWordRegex = new(
            @"(?<![a-zA-Z0-9])[a-zA-Z0-9]{2,}(?:[-_][a-zA-Z0-9]{2,})+(?![a-zA-Z0-9])",
            RegexOptions.Compiled);

        // 优先级数组（按优先级从高到低排列）
        // 相对时间组合（如"明天下午3点"）优先级高于单独的时间（如"下午3点"）
        // 时间格式优先级高于比值格式，确保"14:30"被识别为时间而非比值
        private static readonly (Regex regex, string type, int priority)[] Patterns = new[]
        {
            (DateTimeIsoRegex, "datetime", 100),
            (RangeRegex, "timerange", 95),
            (DeadlineRegex, "deadline", 90),
            (DateTimeChineseRegex, "datetimex", 87),
            (LunarRegex, "lunardate", 80),
            (TraditionalRegex, "traditional", 75),
            (DynastyRegex, "dynasty", 70),
            (FestivalRegex, "festival", 65),
            (SolarTermRegex, "solarterm", 60),
            (RelativeRegex, "relativedate", 55),
            (TimeRegex, "time", 50),
            (RatioRegex, "ratio", 48),
            (DurationRegex, "duration", 45),
            (FuzzyRegex, "fuzzydate", 40),
            (QuarterRegex, "quarter", 35),
            (WeekdayRegex, "weekday", 30),
            (TimezoneRegex, "timezone", 25),
            (AnniversaryRegex, "anniversary", 20),
            (PercentageRegex, "percentage", 18),
            (VersionRegex, "version", 15),
            (DomainRegex, "domain", 10),
            (HyphenatedWordRegex, "hyphenated", 5),
        };

        /// <summary>
        /// 识别文本中的日期时间实体
        /// </summary>
        /// <param name="text">待识别文本</param>
        /// <returns>识别出的时间实体列表，按位置排序</returns>
        public List<TimeEntity> Recognize(string text)
        {
            var results = new List<TimeEntity>();

            foreach (var (regex, type, priority) in Patterns)
            {
                foreach (Match match in regex.Matches(text))
                {
                    if (match.Length == 0) continue;

                    // 检查是否与已有结果重叠
                    bool overlaps = false;
                    foreach (var existing in results)
                    {
                        if (match.Index < existing.End && match.Index + match.Length > existing.Start)
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    if (overlaps) continue;

                    results.Add(new TimeEntity(match.Value, match.Index, match.Index + match.Length, type));
                }
            }

            // 按位置排序，同一位置按长度降序（优先保留更长的匹配）
            results.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : (b.End - b.Start).CompareTo(a.End - a.Start));
            
            // 去除被包含的子区间
            var filtered = new List<TimeEntity>();
            foreach (var r in results)
            {
                bool contained = false;
                foreach (var f in filtered)
                {
                    if (r.Start >= f.Start && r.End <= f.End) { contained = true; break; }
                }
                if (!contained) filtered.Add(r);
            }

            return filtered;
        }
    }
}
