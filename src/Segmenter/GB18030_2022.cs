using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace JiebaNet.Segmenter
{
    /// <summary>
    /// GB18030-2022 中文字符范围定义（含第1号修改单）
    /// 提供统一的中文字符判断接口，支持到扩展I区
    /// </summary>
    /// <remarks>
    /// GB18030-2022 第1号修改单强制要求中文处理支持以下CJK统一汉字区：
    /// - 基本区（含补区）：0x4E00-0x9FFF
    /// - 扩展A区（含补区）：0x3400-0x4DBF
    /// - 扩展B区（含补区）：0x20000-0x2A6DF
    /// - 扩展C区（含补区）：0x2A700-0x2B73F
    /// - 扩展D区：0x2B740-0x2B81F
    /// - 扩展E区（含补区）：0x2B820-0x2CEAF
    /// - 扩展F区：0x2CEB0-0x2EBEF
    /// - 扩展G区：0x30000-0x3134A
    /// - 扩展H区：0x31350-0x323AF
    /// - 扩展I区：0x2EBF0-0x2EE5D（公安人名生僻字为主）
    /// </remarks>
    public static class GB18030_2022
    {
        /// <summary>
        /// CJK统一汉字范围定义
        /// 元组格式：(名称, 起始码点, 结束码点)
        /// </summary>
        public static readonly (string Name, int Start, int End)[] CJK_RANGES =
        {
            ("CJK Unified Ideographs (基本区含补区)", 0x4E00, 0x9FFF),
            ("CJK Extension A (扩展A含补区)", 0x3400, 0x4DBF),
            ("CJK Extension B (扩展B含补区)", 0x20000, 0x2A6DF),
            ("CJK Extension C (扩展C含补区)", 0x2A700, 0x2B73F),
            ("CJK Extension D (扩展D)", 0x2B740, 0x2B81F),
            ("CJK Extension E (扩展E含补区)", 0x2B820, 0x2CEAF),
            ("CJK Extension F (扩展F)", 0x2CEB0, 0x2EBEF),
            ("CJK Extension G (扩展G)", 0x30000, 0x3134A),
            ("CJK Extension H (扩展H)", 0x31350, 0x323AF),
            ("CJK Extension I (扩展I，公安人名生僻字为主)", 0x2EBF0, 0x2EE5D),
        };

        /// <summary>
        /// 基本多文种平面（BMP）内的CJK范围
        /// 包括：基本区和扩展A区
        /// </summary>
        private const char CJK_BASIC_START = '\u4E00';
        private const char CJK_BASIC_END = '\u9FFF';
        private const char CJK_EXT_A_START = '\u3400';
        private const char CJK_EXT_A_END = '\u4DBF';

        /// <summary>
        /// 判断字符是否为中文字符（支持GB18030-2022全部CJK范围）
        /// </summary>
        /// <param name="c">要判断的字符</param>
        /// <returns>如果是中文字符返回true，否则返回false</returns>
        /// <remarks>
        /// 注意：对于扩展B-I区的字符，单个char无法表示，
        /// 需要使用代理对。此方法仅检查BMP内的字符（基本区和扩展A区）。
        /// 对于代理对字符，请使用IsChineseCharacter(string, int)方法。
        /// </remarks>
        public static bool IsChineseCharacter(char c)
        {
            // 基本区：0x4E00-0x9FFF
            if (c >= CJK_BASIC_START && c <= CJK_BASIC_END)
                return true;

            // 扩展A区：0x3400-0x4DBF
            if (c >= CJK_EXT_A_START && c <= CJK_EXT_A_END)
                return true;

            return false;
        }

        /// <summary>
        /// 判断字符串中指定位置是否为中文字符（支持GB18030-2022全部CJK范围）
        /// </summary>
        /// <param name="text">包含字符的字符串</param>
        /// <param name="index">字符位置索引</param>
        /// <returns>如果是中文字符返回true，否则返回false</returns>
        /// <remarks>
        /// 此方法支持代理对，可以正确识别扩展B-I区的字符。
        /// </remarks>
        public static bool IsChineseCharacter(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
                return false;

            char c = text[index];

            // 基本区：0x4E00-0x9FFF
            if (c >= CJK_BASIC_START && c <= CJK_BASIC_END)
                return true;

            // 扩展A区：0x3400-0x4DBF
            if (c >= CJK_EXT_A_START && c <= CJK_EXT_A_END)
                return true;

            // 检查代理对（扩展B-I区）
            if (char.IsHighSurrogate(c) && index + 1 < text.Length)
            {
                char lowSurrogate = text[index + 1];
                if (char.IsLowSurrogate(lowSurrogate))
                {
                    int codePoint = char.ConvertToUtf32(c, lowSurrogate);
                    // 扩展B区：0x20000-0x2A6DF
                    if (codePoint >= 0x20000 && codePoint <= 0x2A6DF)
                        return true;
                    // 扩展C区：0x2A700-0x2B73F
                    if (codePoint >= 0x2A700 && codePoint <= 0x2B73F)
                        return true;
                    // 扩展D区：0x2B740-0x2B81F
                    if (codePoint >= 0x2B740 && codePoint <= 0x2B81F)
                        return true;
                    // 扩展E区：0x2B820-0x2CEAF
                    if (codePoint >= 0x2B820 && codePoint <= 0x2CEAF)
                        return true;
                    // 扩展F区：0x2CEB0-0x2EBEF
                    if (codePoint >= 0x2CEB0 && codePoint <= 0x2EBEF)
                        return true;
                    // 扩展I区：0x2EBF0-0x2EE5D（公安人名生僻字为主）
                    if (codePoint >= 0x2EBF0 && codePoint <= 0x2EE5D)
                        return true;
                    // 扩展G区：0x30000-0x3134A
                    if (codePoint >= 0x30000 && codePoint <= 0x3134A)
                        return true;
                    // 扩展H区：0x31350-0x323AF
                    if (codePoint >= 0x31350 && codePoint <= 0x323AF)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断码点是否为中文字符（支持GB18030-2022全部CJK范围）
        /// </summary>
        /// <param name="codePoint">Unicode码点</param>
        /// <returns>如果是中文字符返回true，否则返回false</returns>
        public static bool IsChineseCodePoint(int codePoint)
        {
            // 基本区：0x4E00-0x9FFF
            if (codePoint >= 0x4E00 && codePoint <= 0x9FFF)
                return true;

            // 扩展A区：0x3400-0x4DBF
            if (codePoint >= 0x3400 && codePoint <= 0x4DBF)
                return true;

            // 扩展B区：0x20000-0x2A6DF
            if (codePoint >= 0x20000 && codePoint <= 0x2A6DF)
                return true;

            // 扩展C区：0x2A700-0x2B73F
            if (codePoint >= 0x2A700 && codePoint <= 0x2B73F)
                return true;

            // 扩展D区：0x2B740-0x2B81F
            if (codePoint >= 0x2B740 && codePoint <= 0x2B81F)
                return true;

            // 扩展E区：0x2B820-0x2CEAF
            if (codePoint >= 0x2B820 && codePoint <= 0x2CEAF)
                return true;

            // 扩展F区：0x2CEB0-0x2EBEF
            if (codePoint >= 0x2CEB0 && codePoint <= 0x2EBEF)
                return true;

            // 扩展I区：0x2EBF0-0x2EE5D（公安人名生僻字为主）
            if (codePoint >= 0x2EBF0 && codePoint <= 0x2EE5D)
                return true;

            // 扩展G区：0x30000-0x3134A
            if (codePoint >= 0x30000 && codePoint <= 0x3134A)
                return true;

            // 扩展H区：0x31350-0x323AF
            if (codePoint >= 0x31350 && codePoint <= 0x323AF)
                return true;

            return false;
        }

        /// <summary>
        /// 判断字符是否为CJK代理对的高位代理
        /// </summary>
        /// <param name="c">要判断的字符</param>
        /// <returns>如果是CJK代理对的高位代理返回true，否则返回false</returns>
        /// <remarks>
        /// CJK扩展B-I区使用代理对表示：
        /// - 扩展B-F区：高位代理 0xD840-0xD87F
        /// - 扩展I区：高位代理 0xD87A-0xD87B
        /// - 扩展G-H区：高位代理 0xD880-0xD888
        /// </remarks>
        public static bool IsCJKHighSurrogate(char c)
        {
            // 扩展B-F区和I区：0xD840-0xD87F
            if (c >= '\uD840' && c <= '\uD87F')
                return true;
            // 扩展G-H区：0xD880-0xD888
            if (c >= '\uD880' && c <= '\uD888')
                return true;
            return false;
        }

        /// <summary>
        /// 获取中文字符正则表达式模式字符串（仅BMP范围，用于char级别匹配）
        /// </summary>
        /// <remarks>
        /// 包含基本区和扩展A区，适用于单字符匹配场景。
        /// 格式：[\u3400-\u4DBF\u4E00-\u9FFF]
        /// </remarks>
        public static string ChineseCharPattern => @"[\u3400-\u4DBF\u4E00-\u9FFF]";

        /// <summary>
        /// 获取中文字符正则表达式模式字符串（完整范围，用于字符串匹配）
        /// </summary>
        /// <remarks>
        /// 包含基本区和扩展A-I区，适用于字符串匹配场景。
        /// 注意：扩展B-I区使用代理对表示。
        /// 代理对范围说明：
        /// - 扩展B-F区：\uD840-\uD87F 高位代理
        /// - 扩展I区：\uD87A-\uD87B 高位代理（0x2EBF0-0x2EE5D）
        /// - 扩展G-H区：\uD880-\uD888 高位代理（0x30000-0x323AF）
        /// 格式：[\u3400-\u4DBF\u4E00-\u9FFF]|[\uD840-\uD87B][\uDC00-\uDFFF]|[\uD880-\uD888][\uDC00-\uDFFF]
        /// </remarks>
        public static string ChineseFullPattern =>
            @"[\u3400-\u4DBF\u4E00-\u9FFF]|[\uD840-\uD87B][\uDC00-\uDFFF]|[\uD880-\uD888][\uDC00-\uDFFF]";

        /// <summary>
        /// 获取中文字符正则表达式（仅BMP范围，已编译）
        /// </summary>
        public static Regex ChineseCharRegex { get; } = new(ChineseCharPattern, RegexOptions.Compiled);

        /// <summary>
        /// 获取中文字符正则表达式（完整范围，已编译）
        /// </summary>
        public static Regex ChineseFullRegex { get; } = new(ChineseFullPattern, RegexOptions.Compiled);

        /// <summary>
        /// 获取中文字符序列正则表达式模式字符串（仅BMP范围）
        /// </summary>
        /// <remarks>
        /// 匹配一个或多个连续的中文字符（基本区和扩展A区）。
        /// 格式：[\u3400-\u4DBF\u4E00-\u9FFF]+
        /// </remarks>
        public static string ChineseSequencePattern => @"[\u3400-\u4DBF\u4E00-\u9FFF]+";

        /// <summary>
        /// 获取中文字符序列正则表达式（仅BMP范围，已编译）
        /// </summary>
        public static Regex ChineseSequenceRegex { get; } = new(ChineseSequencePattern, RegexOptions.Compiled);

        /// <summary>
        /// 获取非中文字符正则表达式模式字符串（仅BMP范围）
        /// </summary>
        /// <remarks>
        /// 匹配不在基本区和扩展A区的字符。
        /// </remarks>
        public static string NonChineseCharPattern => @"[^\u3400-\u4DBF\u4E00-\u9FFF]";

        /// <summary>
        /// 获取非中文字符正则表达式（仅BMP范围，已编译）
        /// </summary>
        public static Regex NonChineseCharRegex { get; } = new(NonChineseCharPattern, RegexOptions.Compiled);

        /// <summary>
        /// 获取用于正则表达式字符类中的中文范围字符串
        /// </summary>
        /// <remarks>
        /// 用于构建自定义正则表达式时嵌入中文范围。
        /// 格式：\u3400-\u4DBF\u4E00-\u9FFF
        /// 注意：仅包含BMP范围（基本区和扩展A区），不包含代理对。
        /// </remarks>
        public static string ChineseRangeForCharClass => @"\u3400-\u4DBF\u4E00-\u9FFF";

        /// <summary>
        /// 获取包含代理对的中文正则表达式模式（用于分割文本）
        /// </summary>
        /// <remarks>
        /// 用于匹配中文块（包括代理对），用于文本分割场景。
        /// 使用非捕获组(?:...)避免Split方法产生额外的捕获组内容。
        /// 格式：(?:[\u3400-\u4DBF\u4E00-\u9FFF]|[\uD840-\uD87B][\uDC00-\uDFFF]|[\uD880-\uD888][\uDC00-\uDFFF])+
        /// </remarks>
        public static string ChineseBlockPattern =>
            @"(?:[\u3400-\u4DBF\u4E00-\u9FFF]|[\uD840-\uD87B][\uDC00-\uDFFF]|[\uD880-\uD888][\uDC00-\uDFFF])+";

        /// <summary>
        /// 获取中文块正则表达式（已编译）
        /// </summary>
        public static Regex ChineseBlockRegex { get; } = new(ChineseBlockPattern, RegexOptions.Compiled);

        /// <summary>
        /// 获取中文+字母数字混合块正则表达式模式
        /// </summary>
        /// <remarks>
        /// 用于匹配中文+字母数字混合块（包括代理对），用于精确模式分词。
        /// 使用非捕获组(?:...)避免Split方法产生额外的捕获组内容。
        /// 格式：(?:[\u3400-\u4DBF\u4E00-\u9FFF]|[\uD840-\uD87B][\uDC00-\uDFFF]|[\uD880-\uD888][\uDC00-\uDFFF]|a-zA-Z0-9+#&\._%·\-)+
        /// </remarks>
        public static string ChineseMixedBlockPattern =>
            @"(?:[\u3400-\u4DBF\u4E00-\u9FFF]|[\uD840-\uD87B][\uDC00-\uDFFF]|[\uD880-\uD888][\uDC00-\uDFFF]|[a-zA-Z0-9+#&\._%·\-])+";

        /// <summary>
        /// 获取中文+字母数字混合块正则表达式（已编译）
        /// </summary>
        public static Regex ChineseMixedBlockRegex { get; } = new(ChineseMixedBlockPattern, RegexOptions.Compiled);

        /// <summary>
        /// 将文本分割成中文块和非中文块
        /// </summary>
        /// <param name="text">要分割的文本</param>
        /// <returns>分割后的文本块列表</returns>
        /// <remarks>
        /// 此方法正确处理代理对，将扩展B-I区的字符作为中文块的一部分。
        /// 每个块标记是否为中文块。
        /// </remarks>
        public static List<(string Text, bool IsChinese)> SplitText(string text)
        {
            var result = new List<(string Text, bool IsChinese)>();
            if (string.IsNullOrEmpty(text))
                return result;

            var i = 0;
            while (i < text.Length)
            {
                var isChinese = IsChineseCharacter(text, i);
                var start = i;

                // 收集相同类型的字符
                while (i < text.Length)
                {
                    var currentIsChinese = IsChineseCharacter(text, i);
                    if (currentIsChinese != isChinese)
                        break;

                    // 如果是代理对，跳过低位代理
                    if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                        i += 2;
                    else
                        i++;
                }

                result.Add((text.Substring(start, i - start), isChinese));
            }

            return result;
        }

        /// <summary>
        /// 将文本分割成中文+字母数字混合块和非混合块
        /// </summary>
        /// <param name="text">要分割的文本</param>
        /// <returns>分割后的文本块列表</returns>
        /// <remarks>
        /// 此方法正确处理代理对，将扩展B-I区的字符作为中文块的一部分。
        /// 中文+字母数字混合块包括：中文、字母、数字、+#&\._%·-
        /// </remarks>
        public static List<(string Text, bool IsMixed)> SplitTextMixed(string text)
        {
            var result = new List<(string Text, bool IsMixed)>();
            if (string.IsNullOrEmpty(text))
                return result;

            var i = 0;
            while (i < text.Length)
            {
                var isMixed = IsMixedCharacter(text, i);
                var start = i;

                // 收集相同类型的字符
                while (i < text.Length)
                {
                    var currentIsMixed = IsMixedCharacter(text, i);
                    if (currentIsMixed != isMixed)
                        break;

                    // 如果是代理对，跳过低位代理
                    if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                        i += 2;
                    else
                        i++;
                }

                result.Add((text.Substring(start, i - start), isMixed));
            }

            return result;
        }

        /// <summary>
        /// 判断字符串中指定位置是否为混合字符（中文、字母、数字、+#&\._%·-）
        /// </summary>
        /// <param name="text">包含字符的字符串</param>
        /// <param name="index">字符位置索引</param>
        /// <returns>如果是混合字符返回true，否则返回false</returns>
        private static bool IsMixedCharacter(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
                return false;

            // 先检查是否为中文字符
            if (IsChineseCharacter(text, index))
                return true;

            char c = text[index];

            // 检查是否为字母、数字或特殊字符
            if (char.IsLetterOrDigit(c))
                return true;

            // 检查特殊字符：+#&\._%·-
            if (c == '+' || c == '#' || c == '&' || c == '.' || c == '_' || c == '%' || c == '·' || c == '-')
                return true;

            return false;
        }
    }
}
