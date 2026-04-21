using System;
using System.Collections.Generic;
using System.Text;

namespace JiebaNet.Segmenter.Common
{
    /// <summary>
    /// Unicode Rune辅助类，用于正确处理emoji等Unicode字符
    /// 解决UTF-16代理对导致的字符拆分问题
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
        /// </summary>
        public static int GetRuneCount(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // .NET Core 3.0+ 和 .NET 5+ 内置Rune支持
            // 使用StringInfo来计算正确的字符数
            return new System.Globalization.StringInfo(text).LengthInTextElements;
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
    }
}
