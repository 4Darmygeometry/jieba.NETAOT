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
        private const string C_惊 = "惊驚";
        private const string C_季 = "季";
        private const string C_蛰 = "蛰蟄";
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

        // ========== 1. ISO 日期时间 ==========
        // 匹配格式：2021-01-01, 2021/01/01, 2021-01-01 09:00:00, 2021-01-01T09:00:00Z 等
        private static readonly Regex DateTimeIsoRegex = new(
            @"(?:^|(?<![\d\-/\.]))(?<year>\d{4})(?<sep>[-/\.])(?<month>\d{1,2})\k<sep>(?<day>\d{1,2})" +
            @"(?:\s+(?<hour>\d{1,2}):(?<minute>\d{2})(?::(?<second>\d{2}))?)?(?![\d\-/\.])" +
            @"|(?:^|(?<![\d\-/\.T]))\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:?\d{2})?(?![\d\-/\.T])",
            RegexOptions.Compiled);

        // ========== 2. 中文日期 ==========
        // 匹配格式：2021年1月1日, 二〇二一年一月一日, 12月25日
        private static readonly Regex DateChineseRegex = new(
            @"(?:^|(?<!\d))(?:(?<year>\d{4}|[一二三四五六七八九十〇零]{4})[年])?" +
            @"(?<month>\d{1,2}|[一二三四五六七八九十]{1,3})月" +
            @"(?<day>\d{1,2}|[一二三四五六七八九十]{1,3})[日号號]?(?!\d)",
            RegexOptions.Compiled);

        // ========== 3. 农历 ==========
        private static readonly Regex LunarRegex = new(
            @"(?:农历|阴历|農曆)?" +
            @"(?:[甲乙丙丁戊己庚辛壬癸][子丑寅卯辰巳午未申酉戌亥][" + C_时 + @"])?" +
            @"(?:[鼠牛虎兔" + C_龙 + "蛇" + C_马 + "羊猴" + C_鸡 + "狗" + C_猪 + "][" + C_时 + @"])?" +
            @"(?<lunar>[" + C_闰 + @"]?[正一二三四五六七八九十冬" + C_腊 + "]{1,2}月" +
            @"(?:初[一二三四五六七八九十]|十[一二三四五六七八九十]|[廿二][一二三四五六七八九十]|三十))" +
            @"[日号號]?",
            RegexOptions.Compiled);

        // ========== 4. 节日 ==========
        private static readonly Regex FestivalRegex = new(
            @"春[" + C_节 + @"]|元宵[" + C_节 + @"]|端午[" + C_节 + @"]|七夕|中秋[" + C_节 + 
            @"]|重[" + C_阳 + @"][" + C_节 + @"]|[" + C_腊 + @"]八[" + C_节 + @"]|小年|除夕|" +
            @"元旦|情人[" + C_节 + @"]|[" + C_妇 + @"]女[" + C_节 + @"]|[" + C_劳 + @"][" + C_动 + @"][" + C_节 + 
            @"]|[" + C_儿 + @"]童[" + C_节 + @"]|教[" + C_师 + @"][" + C_节 + @"]|[" + C_国 + @"][" + C_庆 + @"][" + C_节 + 
            @"]|[" + C_圣 + @"][" + C_诞 + @"][" + C_节 + @"]|感恩[" + C_节 + @"]|母[" + C_亲 + @"][" + C_节 + @"]|父[" + C_亲 + @"][" + C_节 + @"]",
            RegexOptions.Compiled);

        // ========== 5. 节气 ==========
        private static readonly Regex SolarTermRegex = new(
            @"立春|雨水|[" + C_惊 + @"][" + C_蛰 + @"]|春分|清明|[" + C_谷 + @"]雨" +
            @"|立夏|小[" + C_满 + @"]|芒[" + C_种 + @"]|夏至|小暑|大暑|" +
            @"立秋|[" + C_处 + @"]暑|白露|秋分|寒露|霜降|立冬|小雪|大雪|冬至|小寒|大寒",
            RegexOptions.Compiled);

        // ========== 6. 时间（严格格式：HH:MM:SS.ffff、HH:MM:SS、HH:MM，时分秒60进制） ==========
        // 格式1：HH:MM:SS.ffff（如 14:30:00.123）
        // 格式2：HH:MM:SS（如 14:30:00）
        // 格式3：HH:MM（如 4:50, 14:30）
        // 格式4：中文时间表达（如 上午9点, 下午3点半, 晚上8点30分, 凌晨2点，上午六点整）
        // 注意：小时0-23，分钟0-59，秒0-59，毫秒任意位数
        // 注意：负向前瞻需要排除小数点，确保"14:30"后面不跟小数点（除非是毫秒部分）
        private static readonly Regex TimeRegex = new(
            @"(?:^|(?<![\d.]))" +
            @"(?<hour>[01]?\d|2[0-3]):(?<minute>[0-5]?\d)(?::(?<second>[0-5]?\d)(?:\.(?<millisecond>\d+))?)?" +
            @"(?![\d.])|" +
            @"(?<ampm>上午|下午|早上|晚上|凌晨|[" + C_后 + @"]午|傍晚|黄昏)?\s*" +
            @"(?<hour>\d{1,2}|[一二三四五六七八九十]{1,3})[" + C_点 + C_时 + @"]" +
            @"(?:\s*(?<minute>\d{1,2}|半|一刻|三刻|整)[分]?)?(?:\s*(?<second>\d{1,2})秒)?",
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

        // ========== 7. 相对时间（支持组合：明天下午3点、昨天上午9点、今天晚上8点、今天4:50等） ==========
        // 注意：星期模式中'周'必须后跟星期几数字，避免'周年'中的'周'被误匹配
        private static readonly Regex RelativeRegex = new(
            @"(?:[" + C_这 + @"]天|今天|明天|[" + C_后 + @"]天|昨天|前天|大前天|大[" + C_后 + @"]天|" +
            @"[" + C_现 + @"]在|目前|[" + C_当 + @"]前|此刻|[" + C_这 + @"][" + C_时 + @"]|" +
            @"(?:上|本|[" + C_这 + @"]|下)?(?:星期|[" + C_礼 + @"]拜)[一二三四五六日天]?|" +
            @"(?:上|本|[" + C_这 + @"]|下)?[" + C_周 + @"][一二三四五六日天]|" +
            @"(?:上|本|[" + C_这 + @"]|下)[" + C_个 + @"]?月|" +
            @"(?:大?[前[" + C_这 + @"]今明[" + C_后 + @"]]年))" +
            // 可选组合时间：支持两种格式
            // 格式1：中文时间（如"明天下午3点"、"昨天上午9点"、"今天上午六点整"）
            @"(?:\s*(?:上午|下午|早上|晚上|凌晨|[" + C_后 + @"]午|傍晚|黄昏)?\s*(?:\d{1,2}|[一二三四五六七八九十]{1,3})[" + C_点 + C_时 + @"]" +
            @"(?:\s*(?:\d{1,2}|半|一刻|三刻|整)[分]?)?(?:\s*\d{1,2}秒)?" +
            // 格式2：数字时间（如"今天4:50"、"明天14:30:00"）
            @"|\s*(?:上午|下午|早上|晚上|凌晨|[" + C_后 + @"]午|傍晚|黄昏)?\s*\d{1,2}:\d{2}(?::\d{2})?)?",
            RegexOptions.Compiled);

        // ========== 8. 持续时间 ==========
        // 注意：'周'单位需要排除'周年'（纪念日），避免DurationRegex抢占AnniversaryRegex的匹配
        // 注意：'年'单位需要排除后面跟'第X季度'的情况，避免DurationRegex抢占QuarterRegex的匹配
        private static readonly Regex DurationRegex = new(
            @"(?:\d+(?:\.\d+)?|[一二三四五六七八九十百千]+)\s*" +
            @"(?:年(?!第?[一二三四1234][" + C_季 + C_节 + @"]度|Q[1234])|[" + C_个 + @"]个月|月|[" + C_周 + @"](?!年)|天|日|[" + C_个 + @"]?小[" + C_时 + @"]|分[" + C_钟 + @"]|秒[" + C_钟 + @"]|毫秒|[" + C_个 + @"]个工作日)",
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
        private static readonly Regex WeekdayRegex = new(
            @"(?:星期|[" + C_礼 + @"]拜|[" + C_周 + @"])(?<day>[一二三四五六日天1234567])|" +
            @"(?<day>Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday|Mon|Tue|Wed|Thu|Fri|Sat|Sun)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ========== 15. 时区 ==========
        // 注意：'北京时间'需完整匹配'北京+时间/時間'，避免被分词器拆成'北京时/间'
        private static readonly Regex TimezoneRegex = new(
            @"UTC[+-]\d{1,2}(?::?\d{2})?|GMT[+-]?\d{1,2}?|CST|EST|PST|MST|JST|IST|CET|" +
            @"北京[" + C_时 + @"][" + C_间 + @"]?|[" + C_东 + @"]京[" + C_时 + @"][" + C_间 + @"]?|[" + C_纽 + @"][" + C_约 + @"][" + C_时 + @"][" + C_间 + @"]?|[" + C_伦 + @"]敦[" + C_时 + @"][" + C_间 + @"]?|巴黎[" + C_时 + @"][" + C_间 + @"]?|悉尼[" + C_时 + @"][" + C_间 + @"]?|莫斯科[" + C_时 + @"][" + C_间 + @"]?|" +
            @"[" + C_东 + @"][一二三四五六七八九十]{1,2}区|西[一二三四五六七八九十]{1,2}区",
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

        // ========== 18. 版本号 ==========
        // 格式：v1.0.1、1.0.1、3.2-preview1、4.1.2-rc1、2.1-alpha1、6.3-beta2
        // 注意：版本号至少两个数字部分，可带预发布标签
        private static readonly Regex VersionRegex = new(
            @"(?:v|V)?" +
            @"\d+\.\d+(?:\.\d+)?" +
            @"(?:-(?:alpha|beta|rc|preview|pre|dev|snapshot|release|build|hotfix|patch|major|minor|final)\d*)?",
            RegexOptions.Compiled);

        // 优先级数组（按优先级从高到低排列）
        // 相对时间组合（如"明天下午3点"）优先级高于单独的时间（如"下午3点"）
        // 时间格式优先级高于比值格式，确保"14:30"被识别为时间而非比值
        private static readonly (Regex regex, string type, int priority)[] Patterns = new[]
        {
            (DateTimeIsoRegex, "datetime", 100),
            (RangeRegex, "timerange", 95),
            (DeadlineRegex, "deadline", 90),
            (DateChineseRegex, "date", 85),
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
            (VersionRegex, "version", 15),
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
