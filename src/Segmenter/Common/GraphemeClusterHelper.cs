using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace JiebaNet.Segmenter.Common
{
    /// <summary>
    /// Unicode Grapheme Cluster（字形簇）辅助类
    /// 用于正确处理复杂emoji序列，包括ZWJ、变体选择符、肤色修饰符等
    /// </summary>
    public static class GraphemeClusterHelper
    {
        /// <summary>
        /// 将字符串分割为Grapheme Cluster列表
        /// 每个Grapheme Cluster代表一个用户感知的字符（包括复杂emoji序列）
        /// </summary>
        public static List<string> SplitToGraphemes(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text))
                return result;

            var enumerator = StringInfo.GetTextElementEnumerator(text);
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.GetTextElement());
            }

            return result;
        }

        /// <summary>
        /// 将Span分割为Grapheme Cluster列表
        /// </summary>
        public static List<string> SplitToGraphemes(ReadOnlySpan<char> text)
        {
            return SplitToGraphemes(text.ToString());
        }

        /// <summary>
        /// 获取字符串中的Grapheme Cluster数量
        /// </summary>
        public static int GetGraphemeCount(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            return new StringInfo(text).LengthInTextElements;
        }

        /// <summary>
        /// 获取指定Grapheme索引对应的char起始位置
        /// </summary>
        public static int GetCharIndexFromGraphemeIndex(string text, int graphemeIndex)
        {
            if (string.IsNullOrEmpty(text) || graphemeIndex < 0)
                return -1;

            var enumerator = StringInfo.GetTextElementEnumerator(text);
            var currentIndex = 0;
            
            while (enumerator.MoveNext())
            {
                if (currentIndex == graphemeIndex)
                {
                    return enumerator.ElementIndex;
                }
                currentIndex++;
            }

            return text.Length;
        }

        /// <summary>
        /// 检查Grapheme Cluster是否为emoji
        /// 通过检查是否包含emoji范围的字符来判断
        /// </summary>
        public static bool IsEmojiGrapheme(string grapheme)
        {
            if (string.IsNullOrEmpty(grapheme))
                return false;

            // 检查是否包含emoji范围的字符
            foreach (var rune in grapheme.EnumerateRunes())
            {
                if (IsEmojiRune(rune))
                    return true;
            }

            // 检查是否是变体选择符或ZWJ序列
            if (grapheme.IndexOf('\uFE0F') >= 0 ||  // 变体选择符-16 (emoji样式)
                grapheme.IndexOf('\uFE0E') >= 0 ||  // 变体选择符-15 (文本样式)
                grapheme.IndexOf('\u200D') >= 0)   // ZWJ
            {
                // 如果包含这些字符，检查前面是否有emoji基础字符
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查Rune是否为emoji基础字符
        /// </summary>
        private static bool IsEmojiRune(Rune rune)
        {
            var value = rune.Value;
            
            // Emoji范围
            return (value >= 0x1F600 && value <= 0x1F64F) ||  // 表情符号
                   (value >= 0x1F300 && value <= 0x1F5FF) ||  // 杂项符号和象形文字
                   (value >= 0x1F680 && value <= 0x1F6FF) ||  // 交通和地图符号
                   (value >= 0x1F1E0 && value <= 0x1F1FF) ||  // 旗帜 (区域指示符号)
                   (value >= 0x1F900 && value <= 0x1F9FF) ||  // 补充符号和象形文字
                   (value >= 0x1FA00 && value <= 0x1FA6F) ||  // 国际象棋符号
                   (value >= 0x1FA70 && value <= 0x1FAFF) ||  // 符号和象形文字扩展-A
                   (value >= 0x2600 && value <= 0x27BF) ||    // 杂项符号
                   (value >= 0x2700 && value <= 0x27BF) ||    // Dingbats
                   (value >= 0x2300 && value <= 0x23FF) ||    // 杂项技术符号
                   (value >= 0x2B50 && value <= 0x2B55) ||    // 星星等
                   value == 0x231A ||  // 手表
                   value == 0x231B ||  // 沙漏
                   value == 0x23E9 ||  // 快进
                   value == 0x23EA ||  // 快退
                   value == 0x23EB ||  // 快进向上
                   value == 0x23EC ||  // 快退向下
                   value == 0x23F0 ||  // 闹钟
                   value == 0x23F3 ||  // 沙漏流动
                   value == 0x25AA ||  // 小黑方块
                   value == 0x25AB ||  // 小白方块
                   value == 0x25B6 ||  // 播放按钮
                   value == 0x25C0 ||  // 反向播放按钮
                   value == 0x25FB ||  // 中等白色方块
                   value == 0x25FC ||  // 中等黑色方块
                   value == 0x25FD ||  // 中等偏小白色方块
                   value == 0x25FE ||  // 中等偏小黑色方块
                   value == 0x2614 ||  // 雨伞带水滴
                   value == 0x2615 ||  // 热饮
                   value == 0x2648 ||  // 白羊座
                   value == 0x2649 ||  // 金牛座
                   value == 0x264A ||  // 双子座
                   value == 0x264B ||  // 巨蟹座
                   value == 0x264C ||  // 狮子座
                   value == 0x264D ||  // 处女座
                   value == 0x264E ||  // 天秤座
                   value == 0x264F ||  // 天蝎座
                   value == 0x2650 ||  // 射手座
                   value == 0x2651 ||  // 摩羯座
                   value == 0x2652 ||  // 水瓶座
                   value == 0x2653 ||  // 双鱼座
                   value == 0x267F ||  // 轮椅符号
                   value == 0x2693 ||  // 锚
                   value == 0x26A1 ||  // 高压
                   value == 0x26AA ||  // 中等白色圆
                   value == 0x26AB ||  // 中等黑色圆
                   value == 0x26BD ||  // 足球
                   value == 0x26BE ||  // 棒球
                   value == 0x26C4 ||  // 雪人
                   value == 0x26C5 ||  // 太阳和云
                   value == 0x26CE ||  // 蛇夫座
                   value == 0x26D4 ||  // 禁止进入
                   value == 0x26EA ||  // 教堂
                   value == 0x26F2 ||  // 喷泉
                   value == 0x26F3 ||  // 高尔夫洞
                   value == 0x26F5 ||  // 帆船
                   value == 0x26FA ||  // 帐篷
                   value == 0x26FD ||  // 加油站
                   value == 0x2702 ||  // 剪刀
                   value == 0x2705 ||  // 勾选标记
                   value == 0x2708 ||  // 飞机
                   value == 0x2709 ||  // 信封
                   value == 0x270A ||  // 握拳
                   value == 0x270B ||  // 举手
                   value == 0x270C ||  // 胜利手势
                   value == 0x270D ||  // 写字手势
                   value == 0x270F ||  // 铅笔
                   value == 0x2712 ||  // 黑色钢笔
                   value == 0x2714 ||  // 重勾选标记
                   value == 0x2716 ||  // 重乘号
                   value == 0x271D ||  // 拉丁十字架
                   value == 0x2721 ||  // 六芒星
                   value == 0x2728 ||  // 闪光
                   value == 0x2733 ||  // 八角星
                   value == 0x2734 ||  // 八角星
                   value == 0x2744 ||  // 雪花
                   value == 0x2747 ||  // 闪光
                   value == 0x274C ||  // 叉号
                   value == 0x274E ||  // 叉号方框
                   value == 0x2753 ||  // 黑色问号
                   value == 0x2754 ||  // 白色问号
                   value == 0x2755 ||  // 白色感叹号
                   value == 0x2757 ||  // 重感叹号
                   value == 0x2763 ||  // 心形感叹号
                   value == 0x2764 ||  // 红心
                   value == 0x2795 ||  // 加号
                   value == 0x2796 ||  // 减号
                   value == 0x2797 ||  // 除号
                   value == 0x27A1 ||  // 黑色右箭头
                   value == 0x27B0 ||  // 弯曲箭头
                   value == 0x27BF ||  // 双弯曲箭头
                   value == 0x2934 ||  // 箭头向右弯曲向上
                   value == 0x2935 ||  // 箭头向右弯曲向下
                   value == 0x2B05 ||  // 左箭头
                   value == 0x2B06 ||  // 上箭头
                   value == 0x2B07 ||  // 下箭头
                   value == 0x2B1B ||  // 大黑方块
                   value == 0x2B1C ||  // 大白方块
                   value == 0x2B50 ||  // 中等星
                   value == 0x2B55 ||  // 重圆圈
                   (value >= 0xFE00 && value <= 0xFE0F);      // 变体选择符
        }

        /// <summary>
        /// 获取Grapheme Cluster的详细信息
        /// </summary>
        public static GraphemeInfo GetGraphemeInfo(string grapheme)
        {
            var info = new GraphemeInfo
            {
                Text = grapheme,
                CharLength = grapheme.Length,
                RuneCount = 0
            };

            foreach (var _ in grapheme.EnumerateRunes())
            {
                info.RuneCount++;
            }

            info.IsEmoji = IsEmojiGrapheme(grapheme);
            
            // 检测emoji类型
            if (info.IsEmoji)
            {
                if (grapheme.IndexOf('\u200D') >= 0)
                {
                    info.EmojiType = EmojiType.ZwjSequence;
                }
                else if (grapheme.IndexOf('\uFE0F') >= 0 || grapheme.IndexOf('\uFE0E') >= 0)
                {
                    info.EmojiType = EmojiType.WithVariationSelector;
                }
                else if (grapheme.Length > 2)
                {
                    info.EmojiType = EmojiType.Complex;
                }
                else
                {
                    info.EmojiType = EmojiType.Simple;
                }
            }

            return info;
        }
    }

    /// <summary>
    /// Grapheme Cluster信息
    /// </summary>
    public class GraphemeInfo
    {
        public string Text { get; set; } = string.Empty;
        public int CharLength { get; set; }
        public int RuneCount { get; set; }
        public bool IsEmoji { get; set; }
        public EmojiType EmojiType { get; set; }
    }

    /// <summary>
    /// Emoji类型
    /// </summary>
    public enum EmojiType
    {
        None,
        Simple,              // 简单emoji (单个Rune)
        WithVariationSelector, // 带变体选择符
        ZwjSequence,         // ZWJ序列
        Complex              // 其他复杂emoji
    }
}
