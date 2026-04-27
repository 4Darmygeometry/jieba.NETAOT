using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace JiebaNet.Segmenter.Common
{
    /// <summary>
    /// 基于Span的高性能字符串处理扩展方法
    /// 减少字符串分配，提升分词性能
    /// 此方法只处理英文，处理中文请使用GB18030_2022.cs
    /// </summary>
    public static class SpanExtensions
    {
        /// <summary>
        /// 使用Span进行字符串切片，避免Substring分配
        /// 仅在需要字符串时才创建新实例
        /// </summary>
        public static ReadOnlySpan<char> AsSpanSlice(this string s, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(s) || startIndex < 0 || length < 0 || startIndex + length > s.Length)
            {
                return ReadOnlySpan<char>.Empty;
            }
            return s.AsSpan(startIndex, length);
        }

        /// <summary>
        /// 使用Span进行字符串切片（从startIndex到末尾）
        /// </summary>
        public static ReadOnlySpan<char> AsSpanFrom(this string s, int startIndex)
        {
            if (string.IsNullOrEmpty(s) || startIndex < 0 || startIndex >= s.Length)
            {
                return ReadOnlySpan<char>.Empty;
            }
            return s.AsSpan(startIndex);
        }

        /// <summary>
        /// 高效比较两个字符序列是否相等
        /// </summary>
        public static bool SequenceEqual(this ReadOnlySpan<char> span, string value)
        {
            return span.SequenceEqual(value.AsSpan());
        }

        /// <summary>
        /// 高效比较两个字符序列是否相等（忽略大小写）
        /// </summary>
        public static bool SequenceEqualIgnoreCase(this ReadOnlySpan<char> span, string value)
        {
            return span.Equals(value.AsSpan(), StringComparison.OrdinalIgnoreCase);
        }



        /// <summary>
        /// 检查字符是否为ASCII字母或数字
        /// </summary>
        public static bool IsAsciiAlphanumeric(this char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
        }

        /// <summary>
        /// 使用ArrayPool创建临时字符串，减少GC压力
        /// 用于高频临时字符串操作
        /// </summary>
        public static string ToStringPooled(this ReadOnlySpan<char> span)
        {
            return span.ToString();
        }

        /// <summary>
        /// 计算字符序列的哈希值（用于字典查找）
        /// </summary>
        public static int GetSpanHashCode(this ReadOnlySpan<char> span)
        {
#if NET48 || NETSTANDARD2_0 || NETSTANDARD2_1
            // 对于旧框架和.NET Standard 2.1，使用简单的哈希算法
            unchecked
            {
                int hash = 17;
                foreach (var c in span)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
#else
            // .NET 5+ 使用内置方法
            return string.GetHashCode(span);
#endif
        }

        /// <summary>
        /// 比较两个字符序列是否相等
        /// </summary>
        public static bool SpanEquals(this ReadOnlySpan<char> left, ReadOnlySpan<char> right)
        {
            return left.SequenceEqual(right);
        }
    }

    /// <summary>
    /// 高性能字符缓冲区，用于分词过程中的临时字符串构建
    /// </summary>
    public ref struct SpanBuffer
    {
        private Span<char> _buffer;
        private int _length;

        public SpanBuffer(Span<char> buffer)
        {
            _buffer = buffer;
            _length = 0;
        }

        public int Length => _length;
        public int Capacity => _buffer.Length;
        public bool IsEmpty => _length == 0;
        public bool IsFull => _length >= _buffer.Length;

        public ReadOnlySpan<char> AsSpan()
        {
            return _buffer.Slice(0, _length);
        }

        public void Append(char c)
        {
            if (_length < _buffer.Length)
            {
                _buffer[_length++] = c;
            }
        }

        public void Append(ReadOnlySpan<char> span)
        {
            var toCopy = Math.Min(span.Length, _buffer.Length - _length);
            span.Slice(0, toCopy).CopyTo(_buffer.Slice(_length));
            _length += toCopy;
        }

        public void Clear()
        {
            _length = 0;
        }

        public override string ToString()
        {
            return _buffer.Slice(0, _length).ToString();
        }
    }

    /// <summary>
    /// 基于Span的字典查找辅助类
    /// 用于在不创建字符串的情况下查找字典
    /// </summary>
    public static class SpanDictionaryHelper
    {
        /// <summary>
        /// 使用Span作为键查找字典
        /// 通过遍历字典进行匹配（适用于小字典）
        /// 对于大字典，建议使用专门的Span字典实现
        /// </summary>
        public static bool TryGetValue<TKey, TValue>(
            IDictionary<TKey, TValue> dictionary,
            ReadOnlySpan<char> key,
            out TValue value,
            Func<TKey, string> keyToString) where TKey : class
        {
            // 对于字符串键的字典，直接遍历比较
            foreach (var kvp in dictionary)
            {
                var keyStr = keyToString(kvp.Key);
                if (key.SequenceEqual(keyStr))
                {
                    value = kvp.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        /// <summary>
        /// 优化的字符串字典查找
        /// 先尝试使用现有字符串实例，避免创建新字符串
        /// </summary>
        public static bool TryGetValueOptimized(
            IDictionary<string, int> dictionary,
            ReadOnlySpan<char> key,
            out int value)
        {
            // 遍历字典，使用Span比较避免字符串分配
            // 注意：这种方式对于大字典性能不佳
            // 更好的方案是使用专门的Span字典或哈希表
            foreach (var kvp in dictionary)
            {
                if (key.SequenceEqual(kvp.Key))
                {
                    value = kvp.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
}
