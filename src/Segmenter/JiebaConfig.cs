using System;
using System.IO;

namespace JiebaNet.Segmenter
{
    /// <summary>
    /// 分词模式：简体中文或繁体中文
    /// </summary>
    public enum JiebaMode
    {
        /// <summary>
        /// 简体中文模式（加载dict.txt）
        /// </summary>
        ZhHans,

        /// <summary>
        /// 繁体中文模式（加载dict-hant.txt）
        /// </summary>
        ZhHant,

        /// <summary>
        /// 全量模式（加载所有词库，默认行为）
        /// </summary>
        All
    }

    /// <summary>
    /// 表情包处理模式
    /// </summary>
    public enum EmojiMode
    {
        /// <summary>
        /// 启用表情包处理（默认）
        /// </summary>
        Enabled,

        /// <summary>
        /// 禁用表情包处理（--no-emoji模式）
        /// </summary>
        Disabled
    }

    /// <summary>
    /// jieba.NET分词器配置类
    /// 控制词典加载模式、表情包处理等行为
    /// 支持自动检测词库缺失并降级处理
    /// </summary>
    public class JiebaConfig
    {
        /// <summary>
        /// 分词模式（默认全量加载）
        /// </summary>
        public JiebaMode Mode { get; set; } = JiebaMode.All;

        /// <summary>
        /// 表情包处理模式（默认启用）
        /// </summary>
        public EmojiMode Emoji { get; set; } = EmojiMode.Enabled;

        /// <summary>
        /// 是否启用自动降级（当词库缺失时自动切换模式）
        /// 默认为true
        /// </summary>
        public bool AutoFallback { get; set; } = true;

        /// <summary>
        /// 默认构造函数：全量加载+启用表情包
        /// </summary>
        public JiebaConfig() { }

        /// <summary>
        /// 指定分词模式构造函数（默认启用表情包）
        /// </summary>
        /// <param name="mode">分词模式</param>
        public JiebaConfig(JiebaMode mode)
        {
            Mode = mode;
            Emoji = EmojiMode.Enabled;
        }

        /// <summary>
        /// 完整配置构造函数
        /// </summary>
        /// <param name="mode">分词模式</param>
        /// <param name="emoji">表情包处理模式</param>
        public JiebaConfig(JiebaMode mode, EmojiMode emoji)
        {
            Mode = mode;
            Emoji = emoji;
        }

        /// <summary>
        /// 创建简体中文+表情包配置
        /// </summary>
        public static JiebaConfig CreateZhHans() => new(JiebaMode.ZhHans, EmojiMode.Enabled);

        /// <summary>
        /// 创建繁体中文+表情包配置
        /// </summary>
        public static JiebaConfig CreateZhHant() => new(JiebaMode.ZhHant, EmojiMode.Enabled);

        /// <summary>
        /// 创建简体中文+无表情包配置（--no-emoji模式）
        /// </summary>
        public static JiebaConfig CreateZhHansNoEmoji() => new(JiebaMode.ZhHans, EmojiMode.Disabled);

        /// <summary>
        /// 创建繁体中文+无表情包配置（--no-emoji模式）
        /// </summary>
        public static JiebaConfig CreateZhHantNoEmoji() => new(JiebaMode.ZhHant, EmojiMode.Disabled);

        /// <summary>
        /// 创建全量加载配置（默认行为，兼容旧版本）
        /// </summary>
        public static JiebaConfig CreateAll() => new(JiebaMode.All, EmojiMode.Enabled);

        /// <summary>
        /// 根据词库文件存在情况执行自动降级逻辑
        /// 降级规则：
        /// - emoji.txt丢失 → 自动变--no-emoji模式
        /// - dict-hant丢失且当前是zh_hant → 自动进zh_hans模式
        /// - dict.txt丢失且当前是zh_hans → 自动进zh_hant模式
        /// - 都丢失 → 抛出异常
        /// </summary>
        /// <param name="configFileBaseDir">词库基础目录</param>
        /// <returns>经过降级处理后的配置</returns>
        /// <exception cref="InvalidOperationException">当核心词库全部丢失时抛出</exception>
        internal JiebaConfig ApplyAutoFallback(string configFileBaseDir)
        {
            if (!AutoFallback)
                return this;

            var mainDictPath = Path.Combine(configFileBaseDir, "dict.txt");
            var hantDictPath = Path.Combine(configFileBaseDir, "dict-hant.txt");
            var emojiDictPath = Path.Combine(configFileBaseDir, "emoji.txt");

            var hasMainDict = File.Exists(mainDictPath);
            var hasHantDict = File.Exists(hantDictPath);
            var hasEmojiDict = File.Exists(emojiDictPath);

            var newMode = Mode;
            var newEmoji = Emoji;

            if (!hasEmojiDict && newEmoji == EmojiMode.Enabled)
            {
                newEmoji = EmojiMode.Disabled;
            }

            if (!hasMainDict && !hasHantDict)
            {
                throw new InvalidOperationException(
                    $"核心词库缺失：dict.txt与dict-hant.txt均不存在于目录 {configFileBaseDir}。" +
                    "请确保至少提供一个中文词库文件。");
            }

            if (newMode == JiebaMode.ZhHans && !hasMainDict)
            {
                if (!hasHantDict)
                {
                    throw new InvalidOperationException(
                        $"简体词库缺失：dict.txt不存在于目录 {configFileBaseDir}，且无繁体词库可降级。");
                }
                newMode = JiebaMode.ZhHant;
            }
            else if (newMode == JiebaMode.ZhHant && !hasHantDict)
            {
                if (!hasMainDict)
                {
                    throw new InvalidOperationException(
                        $"繁体词库缺失：dict-hant.txt不存在于目录 {configFileBaseDir}，且无简体词库可降级。");
                }
                newMode = JiebaMode.ZhHans;
            }

            if (newMode != Mode || newEmoji != Emoji)
            {
                return new JiebaConfig(newMode, newEmoji) { AutoFallback = false };
            }

            return this;
        }

        /// <summary>
        /// 判断是否需要加载简体中文词库
        /// </summary>
        internal bool ShouldLoadZhHans => Mode == JiebaMode.All || Mode == JiebaMode.ZhHans;

        /// <summary>
        /// 判断是否需要加载繁体中文词库
        /// </summary>
        internal bool ShouldLoadZhHant => Mode == JiebaMode.All || Mode == JiebaMode.ZhHant;

        /// <summary>
        /// 判断是否需要加载emoji词库
        /// </summary>
        internal bool ShouldLoadEmoji => Emoji == EmojiMode.Enabled;
    }
}
