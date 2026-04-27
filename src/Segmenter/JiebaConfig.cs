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
    /// 实体保护模式
    /// 控制日期、时间、版本号、域名等实体是否在分词时作为整体保护
    /// </summary>
    public enum EntityProtect
    {
        /// <summary>
        /// 启用实体保护（默认）：日期、时间、版本号、域名等实体在精确模式与搜索引擎模式整体分出
        /// </summary>
        Enabled,

        /// <summary>
        /// 禁用实体保护：不保护日期、时间、版本号、域名等实体，默认拆分
        /// 适用于OpenCC繁简转换等场景，避免实体保护影响转换
        /// </summary>
        Disabled
    }

    /// <summary>
    /// jieba.NET分词器配置类
    /// 控制词典加载模式、实体保护等行为
    /// 支持自动检测词库缺失并降级处理
    /// Emoji处理改为自动检测emoji.txt是否存在：存在时进行emoji识别，不存在时不进行
    /// </summary>
    public class JiebaConfig
    {
        /// <summary>
        /// 分词模式（默认全量加载）
        /// </summary>
        public JiebaMode Mode { get; set; } = JiebaMode.All;

        /// <summary>
        /// 实体保护模式（默认启用）
        /// 启用时：日期、时间、版本号、域名等实体在精确模式与搜索引擎模式整体分出
        /// 禁用时：不保护实体，默认拆分，适用于OpenCC繁简转换等场景
        /// </summary>
        public EntityProtect EntityProtect { get; set; } = EntityProtect.Enabled;

        /// <summary>
        /// 是否启用自动降级（当词库缺失时自动切换模式）
        /// 默认为true
        /// </summary>
        public bool AutoFallback { get; set; } = true;

        /// <summary>
        /// 默认构造函数：全量加载+启用实体保护
        /// </summary>
        public JiebaConfig() { }

        /// <summary>
        /// 指定分词模式构造函数（默认启用实体保护）
        /// </summary>
        /// <param name="mode">分词模式</param>
        public JiebaConfig(JiebaMode mode)
        {
            Mode = mode;
            EntityProtect = EntityProtect.Enabled;
        }

        /// <summary>
        /// 指定实体保护模式构造函数
        /// </summary>
        /// <param name="entityProtect">实体保护模式</param>
        public JiebaConfig(EntityProtect entityProtect)
        {
            Mode = JiebaMode.All;
            EntityProtect = entityProtect;
        }

        /// <summary>
        /// 完整配置构造函数
        /// </summary>
        /// <param name="mode">分词模式</param>
        /// <param name="entityProtect">实体保护模式</param>
        public JiebaConfig(JiebaMode mode, EntityProtect entityProtect)
        {
            Mode = mode;
            EntityProtect = entityProtect;
        }

        /// <summary>
        /// 创建简体中文配置（启用实体保护）
        /// </summary>
        public static JiebaConfig CreateZhHans() => new(JiebaMode.ZhHans, EntityProtect.Enabled);

        /// <summary>
        /// 创建繁体中文配置（启用实体保护）
        /// </summary>
        public static JiebaConfig CreateZhHant() => new(JiebaMode.ZhHant, EntityProtect.Enabled);

        /// <summary>
        /// 创建全量加载配置（默认行为，兼容旧版本）
        /// </summary>
        public static JiebaConfig CreateAll() => new(JiebaMode.All, EntityProtect.Enabled);

        /// <summary>
        /// 根据词库文件存在情况执行自动降级逻辑
        /// 降级规则：
        /// - dict-hant丢失且当前是zh_hant → 自动进zh_hans模式
        /// - dict.txt丢失且当前是zh_hans → 自动进zh_hant模式
        /// - 都丢失 → 抛出异常
        /// Emoji处理已改为自动检测emoji.txt是否存在，无需手动降级
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

            var hasMainDict = File.Exists(mainDictPath);
            var hasHantDict = File.Exists(hantDictPath);

            var newMode = Mode;

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

            if (newMode != Mode)
            {
                return new JiebaConfig(newMode, EntityProtect) { AutoFallback = false };
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
        /// 自动检测emoji.txt是否存在：存在时加载，不存在时不加载
        /// </summary>
        internal bool ShouldLoadEmoji
        {
            get
            {
                var emojiDictPath = Path.Combine(ConfigManager.ConfigFileBaseDir, "emoji.txt");
                return File.Exists(emojiDictPath);
            }
        }

        /// <summary>
        /// 判断是否启用实体保护
        /// </summary>
        internal bool ShouldProtectEntities => EntityProtect == EntityProtect.Enabled;
    }
}
