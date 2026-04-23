using System.Collections.Generic;

namespace JiebaNet.Segmenter
{
    /// <summary>
    /// 时间实体，表示识别出的日期时间片段
    /// </summary>
    public class TimeEntity
    {
        /// <summary>
        /// 识别出的时间文本
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// 在原文中的起始位置
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// 在原文中的结束位置
        /// </summary>
        public int End { get; }

        /// <summary>
        /// 时间类型（datetime, date, time, lunardate, festival等）
        /// </summary>
        public string Type { get; }

        public TimeEntity(string text, int start, int end, string type)
        {
            Text = text;
            Start = start;
            End = end;
            Type = type;
        }

        public override string ToString()
        {
            return $"[{Text}, {Start}-{End}, {Type}]";
        }
    }

    /// <summary>
    /// 时间识别器接口
    /// </summary>
    public interface ITimeRecognizer
    {
        /// <summary>
        /// 识别文本中的日期时间实体
        /// </summary>
        /// <param name="text">待识别文本</param>
        /// <returns>识别出的时间实体列表，按位置排序</returns>
        List<TimeEntity> Recognize(string text);
    }
}
