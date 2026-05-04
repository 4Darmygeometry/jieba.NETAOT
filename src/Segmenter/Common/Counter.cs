﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JiebaNet.Segmenter.Common
{
    public interface ICounter<T>
    {
        int Count { get; }
        int Total { get; }
        int this[T key] { get; set; }
        IEnumerable<KeyValuePair<T, int>> Elements { get; }

        /// <summary>
        /// Lists the n most common elements from the most common to the least.
        /// </summary>
        /// <param name="n">Number of elements, list all elements if n is less than 0.</param>
        /// <returns></returns>
        IEnumerable<KeyValuePair<T, int>> MostCommon(int n = -1);

        /// <summary>
        /// Subtracts items from a counter.
        /// </summary>
        /// <param name="items"></param>
        void Subtract(IEnumerable<T> items);

        /// <summary>
        /// Subtracts counts from another counter.
        /// </summary>
        /// <param name="other"></param>
        void Subtract(ICounter<T> other);

        /// <summary>
        /// Adds items to a counter.
        /// </summary>
        /// <param name="items"></param>
        void Add(IEnumerable<T> items);

        /// <summary>
        /// Adds another counter.
        /// </summary>
        /// <param name="other"></param>
        void Add(ICounter<T> other);

        /// <summary>
        /// Union is the maximum of value in either of the input <see cref="ICounter{T}"/>.
        /// </summary>
        /// <param name="other">The other counter.</param>
        ICounter<T> Union(ICounter<T> other);

        void Remove(T key);
        void Clear();
        bool Contains(T key);
    }

    /// <summary>
    /// 通用计数器，统计集合中各元素的出现次数
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <remarks>
    /// 对于词级统计，使用 Counter&lt;string&gt; 配合分词器即可正确处理所有文本：
    /// - 默认模式：new Counter&lt;string&gt;(seg.Cut(s))，过滤emoji词频（countEmoji默认为false）
    /// - Emoji提取模式：new Counter&lt;string&gt;(seg.Cut(s), countEmoji: true)，保留emoji词频
    /// </remarks>
    public class Counter<T>: ICounter<T>
    {
        private Dictionary<T, int> data = new Dictionary<T, int>();

        /// <summary>
        /// 是否统计emoji词频
        /// 默认为false，过滤掉emoji词（使用GraphemeClusterHelper检测）
        /// 设为true时保留emoji词频
        /// 仅对Counter&lt;string&gt;有效，其他类型无影响
        /// </summary>
        private readonly bool _countEmoji;

        public Counter() {}

        /// <summary>
        /// 构造函数，默认过滤emoji词频（countEmoji为false）
        /// </summary>
        /// <param name="items">待统计的元素集合</param>
        /// <param name="countEmoji">是否统计emoji词频，默认false（过滤emoji）</param>
        public Counter(IEnumerable<T> items, bool countEmoji = false)
        {
            _countEmoji = countEmoji;
            CountItems(items);
        }

        public int Count => data.Count;
        public int Total => data.Values.Sum();
        public IEnumerable<KeyValuePair<T, int>> Elements => data;

        public int this[T key]
        {
            get => data.ContainsKey(key) ? data[key] : 0;
            set => data[key] = value;
        }

        public IEnumerable<KeyValuePair<T, int>> MostCommon(int n = -1)
        {
            var pairs = data.Where(pair => pair.Value > 0).OrderByDescending(pair => pair.Value);
            return n < 0 ? pairs : pairs.Take(n);
        }

        public void Subtract(IEnumerable<T> items)
        {
            SubtractItems(items);
        }

        public void Subtract(ICounter<T> other)
        {
            SubtractPairs(other.Elements);
        }

        public void Add(IEnumerable<T> items)
        {
            CountItems(items);
        }

        public void Add(ICounter<T> other)
        {
            CountPairs(other.Elements);
        }

        public ICounter<T> Union(ICounter<T> other)
        {
            var result = new Counter<T>();
            foreach (var pair in data)
            {
                var count = pair.Value;
                var otherCount = other[pair.Key];
                var newCount = count < otherCount ? otherCount : count;
                result[pair.Key] = newCount;
            }

            foreach (var pair in other.Elements)
            {
                if (!Contains(pair.Key))
                {
                    result[pair.Key] = pair.Value;
                }
            }
            return result;
        }

        public void Remove(T key)
        {
            if (data.ContainsKey(key))
            {
                data.Remove(key);
            }
        }

        public void Clear()
        {
            data.Clear();
        }

        public bool Contains(T key)
        {
            return data.ContainsKey(key);
        }

        #region Private Methods

        private void CountItems(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                // Counter<string>且_countEmoji为false时，过滤emoji词
                if (!_countEmoji && item is string s && GraphemeClusterHelper.IsEmojiGrapheme(s))
                {
                    continue;
                }

                data[item] = data.GetDefault(item, 0) + 1;
            }
        }

        private void CountPairs(IEnumerable<KeyValuePair<T, int>> pairs)
        {
            foreach (var pair in pairs)
            {
                this[pair.Key] += pair.Value;
            }
        }

        private void SubtractItems(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                // Counter<string>且_countEmoji为false时，过滤emoji词
                if (!_countEmoji && item is string s && GraphemeClusterHelper.IsEmojiGrapheme(s))
                {
                    continue;
                }

                data[item] = data.GetDefault(item, 0) - 1;
            }
        }

        private void SubtractPairs(IEnumerable<KeyValuePair<T, int>> pairs)
        {
            foreach (var pair in pairs)
            {
                this[pair.Key] -= pair.Value;
            }
        }

        #endregion
    }
}
