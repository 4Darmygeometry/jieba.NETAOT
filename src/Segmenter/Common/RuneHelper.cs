using System;
using System.Collections.Generic;
using System.Text;

namespace JiebaNet.Segmenter.Common
{
    /// <summary>
    /// Unicode Rune辅助类，用于正确处理emoji等Unicode字符
    /// 解决UTF-16代理对导致的字符拆分问题
    /// 使用System.Text.Rune进行高性能Unicode处理
    /// </summary>
    public static class RuneHelper
    {
        /// <summary>
        /// 检查字符是否为UTF-16高代理项
        /// </summary>
        public static bool IsHighSurrogate(char c)
        {
            return char.IsHighSurrogate(c);
        }

        /// <summary>
        /// 检查字符是否为UTF-16低代理项
        /// </summary>
        public static bool IsLowSurrogate(char c)
        {
            return char.IsLowSurrogate(c);
        }

        /// <summary>
        /// 检查指定位置的字符是否为emoji（代理对）
        /// </summary>
        public static bool IsEmojiAt(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
                return false;

            // 检查是否为高代理项（emoji的第一个char）
            return char.IsHighSurrogate(text[index]);
        }

        /// <summary>
        /// 获取字符串中的Rune数量（正确的字符数量）
        /// 使用System.Text.Rune进行高效计算
        /// </summary>
        public static int GetRuneCount(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

#if NETSTANDARD2_0 || NET48
            // 对于旧框架，使用StringInfo计算
            return new System.Globalization.StringInfo(text).LengthInTextElements;
#else
            // .NET Core 3.0+ 使用EnumerateRunes计算
            var count = 0;
            foreach (var _ in text.EnumerateRunes())
            {
                count++;
            }
            return count;
#endif
        }

        /// <summary>
        /// 获取字符串中的Rune数量（使用Span版本）
        /// </summary>
        public static int GetRuneCount(ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
                return 0;

#if NETSTANDARD2_0 || NET48
            // 对于旧框架，使用StringInfo计算
            return new System.Globalization.StringInfo(text.ToString()).LengthInTextElements;
#else
            // .NET Core 3.0+ 使用EnumerateRunes计算
            var count = 0;
            foreach (var _ in text.EnumerateRunes())
            {
                count++;
            }
            return count;
#endif
        }

        /// <summary>
        /// 将字符串分割为Rune列表
        /// 每个Rune代表一个完整的Unicode字符（包括emoji）
        /// </summary>
        public static List<string> SplitToRunes(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text))
                return result;

#if NETSTANDARD2_0 || NET48
            // 对于旧框架，手动处理代理对
            var i = 0;
            while (i < text.Length)
            {
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    // 代理对，作为一个整体处理
                    result.Add(text.Substring(i, 2));
                    i += 2;
                }
                else if (char.IsHighSurrogate(text[i]))
                {
                    // 单独的高代理项（不完整），作为单个字符处理
                    result.Add(text[i].ToString());
                    i++;
                }
                else
                {
                    // 普通字符
                    result.Add(text[i].ToString());
                    i++;
                }
            }
#else
            // .NET Core 3.0+ 使用Rune进行高效处理
            foreach (var rune in text.EnumerateRunes())
            {
                if (rune != Rune.ReplacementChar)
                {
                    result.Add(rune.ToString());
                }
                else
                {
                    // 处理无效的Rune
                    result.Add("�");
                }
            }
#endif

            return result;
        }

        /// <summary>
        /// 将Span分割为Rune列表（高性能版本）
        /// </summary>
        public static List<string> SplitToRunes(ReadOnlySpan<char> text)
        {
            var result = new List<string>();
            if (text.IsEmpty)
                return result;

#if NETSTANDARD2_0 || NET48
            // 对于旧框架，手动处理代理对
            var i = 0;
            while (i < text.Length)
            {
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    // 代理对，作为一个整体处理
                    result.Add(text.Slice(i, 2).ToString());
                    i += 2;
                }
                else if (char.IsHighSurrogate(text[i]))
                {
                    // 单独的高代理项（不完整），作为单个字符处理
                    result.Add(text[i].ToString());
                    i++;
                }
                else
                {
                    // 普通字符
                    result.Add(text[i].ToString());
                    i++;
                }
            }
#else
            // .NET Core 3.0+ 使用Rune进行高效处理
            foreach (var rune in text.EnumerateRunes())
            {
                if (rune != Rune.ReplacementChar)
                {
                    result.Add(rune.ToString());
                }
                else
                {
                    // 处理无效的Rune
                    result.Add("�");
                }
            }
#endif

            return result;
        }

        /// <summary>
        /// 获取指定Rune索引对应的char起始位置
        /// </summary>
        public static int GetCharIndexFromRuneIndex(string text, int runeIndex)
        {
            if (string.IsNullOrEmpty(text) || runeIndex < 0)
                return -1;

            var currentRuneIndex = 0;
            var charIndex = 0;

            while (charIndex < text.Length && currentRuneIndex < runeIndex)
            {
                if (char.IsHighSurrogate(text[charIndex]) && charIndex + 1 < text.Length && char.IsLowSurrogate(text[charIndex + 1]))
                {
                    charIndex += 2;
                }
                else
                {
                    charIndex++;
                }
                currentRuneIndex++;
            }

            return charIndex;
        }

        /// <summary>
        /// 获取指定char索引对应的Rune索引
        /// </summary>
        public static int GetRuneIndexFromCharIndex(string text, int charIndex)
        {
            if (string.IsNullOrEmpty(text) || charIndex < 0 || charIndex > text.Length)
                return -1;

            var runeIndex = 0;
            var currentCharIndex = 0;

            while (currentCharIndex < charIndex)
            {
                if (char.IsHighSurrogate(text[currentCharIndex]) && currentCharIndex + 1 < text.Length && char.IsLowSurrogate(text[currentCharIndex + 1]))
                {
                    currentCharIndex += 2;
                }
                else
                {
                    currentCharIndex++;
                }
                runeIndex++;
            }

            return runeIndex;
        }

        /// <summary>
        /// 尝试从指定位置获取Rune
        /// </summary>
        /// <param name="text">源字符串</param>
        /// <param name="index">起始位置</param>
        /// <param name="rune">输出的Rune</param>
        /// <returns>消耗的char数量（1或2），如果失败返回-1</returns>
        public static int TryGetRuneAt(string text, int index, out Rune rune)
        {
            if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
            {
                rune = default;
                return -1;
            }

#if NETSTANDARD2_0 || NET48
            // 对于旧框架，手动处理
            if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
            {
                rune = new Rune(text[index], text[index + 1]);
                return 2;
            }
            else if (!char.IsSurrogate(text[index]))
            {
                rune = new Rune(text[index]);
                return 1;
            }
            else
            {
                rune = Rune.ReplacementChar;
                return 1;
            }
#else
            // .NET Core 3.0+ 使用内置方法
            if (Rune.TryGetRuneAt(text, index, out rune))
            {
                return rune.IsBmp ? 1 : 2;
            }
            return -1;
#endif
        }

        /// <summary>
        /// 检查Rune是否为emoji
        /// </summary>
        public static bool IsEmojiRune(Rune rune)
        {
            var value = rune.Value;
            // 常见emoji范围
            return (value >= 0x1F600 && value <= 0x1F64F) ||  // 表情符号
                   (value >= 0x1F300 && value <= 0x1F5FF) ||  // 杂项符号和象形文字
                   (value >= 0x1F680 && value <= 0x1F6FF) ||  // 交通和地图符号
                   (value >= 0x1F1E0 && value <= 0x1F1FF) ||  // 旗帜
                   (value >= 0x2600 && value <= 0x27BF) ||    // 杂项符号
                   (value >= 0xFE00 && value <= 0xFE0F);      // 变体选择符
        }

        /// <summary>
        /// 将字符串分割为Grapheme Cluster列表（推荐用于emoji处理）
        /// 每个Grapheme Cluster代表一个用户感知的字符
        /// 正确处理ZWJ序列、变体选择符、肤色修饰符等复杂emoji
        /// </summary>
        public static List<string> SplitToGraphemes(string text)
        {
            return GraphemeClusterHelper.SplitToGraphemes(text);
        }

        /// <summary>
        /// 将Span分割为Grapheme Cluster列表（推荐用于emoji处理）
        /// </summary>
        public static List<string> SplitToGraphemes(ReadOnlySpan<char> text)
        {
            return GraphemeClusterHelper.SplitToGraphemes(text);
        }

        /// <summary>
        /// 获取字符串中的Grapheme Cluster数量（正确的用户感知字符数量）
        /// </summary>
        public static int GetGraphemeCount(string text)
        {
            return GraphemeClusterHelper.GetGraphemeCount(text);
        }

        /// <summary>
        /// 检查Grapheme Cluster是否为emoji
        /// </summary>
        public static bool IsEmojiGrapheme(string grapheme)
        {
            return GraphemeClusterHelper.IsEmojiGrapheme(grapheme);
        }
    }
}
