jieba.NETAOT（AOTba）是[jieba中文分词](https://github.com/fxsjy/jieba)的.NET版本（C#实现），支持AOT编译。

当前版本为1.1.10，基于jieba 0.42，提供与jieba**基本一致**的功能与接口，但不支持其最新的paddle模式（如须使用paddle模式，请见https://github.com/sdcb/PaddleSharp/blob/master/docs%2Fpaddlenlp-lac.md ）。关于jieba的实现思路，可以看看[这篇wiki](https://github.com/anderscui/jieba.NET/wiki/%E7%90%86%E8%A7%A3%E7%BB%93%E5%B7%B4%E5%88%86%E8%AF%8D)里提到的资料。

此外，也提供了 `KeywordProcessor`，参考 [FlashText](https://github.com/vi3k6i5/flashtext) 实现。`KeywordProcessor` 可以更灵活地从文本中提取**词典中的关键词**，比如忽略大小写、含空格的词等。

如果您在开发中遇到与分词有关的需求或困难，请提交一个Issue，I see u:)

## 特点

* 支持三种分词模式：
    - 精确模式，试图将句子最精确地切开，适合**文本分析**；
    - 全模式，把句子中所有的可以成词的词语都扫描出来, **速度非常快，但是不能解决歧义**。具体来说，分词过程不会借助于词频查找最大概率路径，亦不会使用HMM；
    - 搜索引擎模式，在精确模式的基础上，对长词再次切分，提高召回率，**适合用于搜索引擎分词**。
* 支持**繁体分词**
* 支持添加自定义词典和自定义词
* 支持lcut与lcutforsearch直接返回列表
* 支持日期/时间完整提取不被拆开（如下午3点半、晚上8点30分、2021-01-01 09:00:00）
* 支持比例提取（如提取“金龙鱼1:1:1调和油”的“1:1:1”）
* 支持提取域名（如https://gitee.com/JTsamsde/AOTba ）
* 支持完整提取带下划线/短线单词（如TF-IDF）
* 支持版本号提取（如v1.0.1、1.0.1、3.2-preview1、4.1.2-rc1、2.1-alpha1、6.3-beta2）
* 支持异步加载词典
* 支持TF-IDF、TextRank、KeywordProcessor算法关键词提取
* Counter词频统计支持统计emoji与过滤emoji两种模式，适合不同类型的词云图制作
* 支持日期、时间、链接、版本号等实体提取
* 支持含Emoji句子断句（带未收录emoji识别能力）
* 支持带变体选择符、ZWJ、肤色选择符、区域选择符的复杂emoji断句（甚至支持到Unicode 16的emoji）
* 支持开启或关闭实体保护，以便OpenCC.NET调用
* 支持带空格的词（比如Kimi K2.5）
* 全面支持GB18030-2022级别3及一号修改单要求（基本区到扩展I区汉字、〇及康熙部首处理能力）
* 可AOT编译，纯CPU可流畅运行
* 内置正则超时熔断保护，可防ReDos攻击
* 1.0.9及以前为 MIT 授权协议，1.0.10及以上为 Apache 2.0 与 MIT 双授权协议，可以商用闭源发布
* 100%向下兼容jieba.NET的语法，仅需换nuget包即可完成迁移
* 支持Windows 7 SP1及以上操作系统（若要完整显示GB18030-2022所有文本，须Windows 11 24H2及以上）

## 算法

* 基于前缀词典实现高效的词图扫描，生成句子中汉字所有可能成词情况所构成的有向无环图 (DAG)
* 采用了动态规划查找最大概率路径, 找出基于词频的最大切分组合
* 对于未登录词，采用了基于汉字成词能力的HMM模型，使用了Viterbi算法
* 日期时间等实体识别是基于正则的，同时具有过滤算法，避免提取非时间词典词（如百年孤独）
* 通过GB18030_2022.cs实现GB18030-2022汉字合规
* 通过RuneHelper.cs和GraphemeClusterHelper.cs实现emoji识别

## 安装和配置

若是源码修改，安装配置前需确保Visual Studio版本在2026及以上

若安装nuget包，需确保Visual Studio版本在2019及以上

当前版本支持net10.0、net48、netstandard2.0和netstandard2.1（兼容.NET 6+），可以手动引用项目，也可以通过NuGet添加引用：

```shell
PM> Install-Package AOTba
```

若使用的软件引用了本项目类库，且运行在NT5.1内核的操作系统，须参考[B站文章的安装方法](https://www.bilibili.com/opus/1044900873850847240)安装.NET Framework 4.8。

安装之后，在packages\jieba.NET目录下可以看到Resources目录，这里面是jieba.NET运行所需的词典及其它数据文件，最简单的配置方法是将整个Resources目录拷贝到程序集所在目录，这样jieba.NET会使用内置的默认配置值。如果希望将这些文件放在其它位置，则要在app.config或web.config中添加如下的配置项：

```xml
<appSettings>
    <add key="JiebaConfigFileDir" value="C:\jiebanet\config" />
</appSettings>
```

需要注意的是，这个路径可以使用绝对路径或相对路径。**如果使用相对路径，那么jieba.NET会假设该路径是相对于当前应用程序域的BaseDirectory**。

配置示例：

* 采用绝对路径时，比如配置项为C:\jiebanet\config，那么主词典的路径会拼接为：C:\jiebanet\config\dict.txt。
* 采用相对路径时（或未添加任何配置项，那么将会使用默认的**相对路径：Resources**），比如配置项为..\config（可通过..来调整相对路径），若当前应用程序域的BaseDirectory是C:\myapp\bin\，那么主词典的路径会拼接为：C:\myapp\config\dict.txt。

### 使用代码配置词典路径

如果因为某些原因，不方便通过应用的 config 文件配置，可使用代码设置（在使用任何分词功能之前，建议使用绝对路径），如：

```c#
JiebaNet.Segmenter.ConfigManager.ConfigFileBaseDir = @"C:\jiebanet\config";
```

## 主要功能

### 1. 分词

* `JiebaSegmenter.Cut`方法接受三个输入参数，text为待分词的字符串；cutAll指定是否采用全模式；hmm指定使用是否使用hmm模型切分未登录词；返回类型为`IEnumerable<string>`
* `JiebaSegmenter.CutForSearch`方法接受两个输入参数，text为待分词的字符串；hmm指定使用是否使用hmm模型；返回类型为`IEnumerable<string>`
* `JiebaSegmenter.LCut`方法接受三个输入参数，text为待分词的字符串；cutAll指定是否采用全模式；hmm指定使用是否使用hmm模型切分未登录词；返回类型为`List<string>`
* `JiebaSegmenter.LCutForSearch`方法接受两个输入参数，text为待分词的字符串；hmm指定使用是否使用hmm模型；返回类型为`List<string>`

* 另外，jieba.NETAOT支持Tokenizer 自定义分词器（独立词典），如：
```c#
// 注意：关闭emoji处理的API已于1.0.10版本废除，改为自动判断emoji.txt是否存在
// 仅加载简体中文词库 + 支持表情包处理
var config = new JiebaConfig(JiebaMode.ZhHans);
var segmenter = new JiebaSegmenter(config);

// 不进行实体（日期、时间等）保护（适用于OpenCC.NET调用）
var config = new JiebaConfig(EntityProtect.Disabled);
var segmenter = new JiebaSegmenter(config);

//若 var segmenter = new JiebaSegmenter(); 则为全量加载

// Tokenizer 自定义分词器（独立词典）
var tokenizer = new Tokenizer(new JiebaConfig(JiebaMode.ZhHans));
var result = tokenizer.Lcut("我来到北京清华大学");

// jieba.dt 默认分词器
var dtResult = Jieba.Lcut("我来到北京清华大学");

// 异步加载
var asyncSegmenter = await JiebaSegmenter.CreateAsync();
```

代码示例

```c#
using JiebaNet.Segmenter;
var segmenter = new JiebaSegmenter();
var segments = segmenter.Cut("我来到北京清华大学", cutAll: true);
Console.WriteLine("【全模式】：{0}", string.Join("/ ", segments));

segments = segmenter.Cut("我来到北京清华大学");  // 默认为精确模式
Console.WriteLine("【精确模式】：{0}", string.Join("/ ", segments));

segments = segmenter.Cut("他来到了网易杭研大厦");  // 默认为精确模式，同时也使用HMM模型
Console.WriteLine("【新词识别】：{0}", string.Join("/ ", segments));

segments = segmenter.CutForSearch("小明硕士毕业于中国科学院计算所，后在日本京都大学深造"); // 搜索引擎模式
Console.WriteLine("【搜索引擎模式】：{0}", string.Join("/ ", segments));

segments = segmenter.Cut("结过婚的和尚未结过婚的");
Console.WriteLine("【歧义消除】：{0}", string.Join("/ ", segments));

// Lcut 方法直接返回 List<string>，无需 ToList() 转换
var words = segmenter.Lcut("我来到北京清华大学");
Console.WriteLine("【Lcut精确模式】：{0}", string.Join("/ ", words));

words = segmenter.Lcut("我来到北京清华大学", cutAll: true);
Console.WriteLine("【Lcut全模式】：{0}", string.Join("/ ", words));

// LcutForSearch 方法直接返回 List<string>
words = segmenter.LcutForSearch("小明硕士毕业于中国科学院计算所");
Console.WriteLine("【LcutForSearch】：{0}", string.Join("/ ", words));
```

输出

```
【全模式】：我/ 来到/ 北京/ 清华/ 清华大学/ 华大/ 大学
【精确模式】：我/ 来到/ 北京/ 清华大学
【新词识别】：他/ 来到/ 了/ 网易/ 杭研/ 大厦
【搜索引擎模式】：小明/ 硕士/ 毕业/ 于/ 中国/ 科学/ 学院/ 科学院/ 中国科学院/ 计算/ 计算所/ ，/ 后/ 在/ 日本/ 京都/ 大学/ 日本京都大学/ 深造
【歧义消除】：结过婚/ 的/ 和/ 尚未/ 结过婚/ 的
【Lcut精确模式】：我/ 来到/ 北京/ 清华大学
【Lcut全模式】：我/ 来到/ 北京/ 清华/ 清华大学/ 华大/ 大学
【LcutForSearch】：小明/ 硕士/ 毕业/ 于/ 中国/ 科学/ 学院/ 科学院/ 中国科学院/ 计算/ 计算所
```

AOT情形下含Emoji句子断句测试

```
=== AOTba AOT 兼容性测试 ===

[测试] 精确模式分词...
  结果: 我╱来到╱北京╱清华大学
  通过 ✓
[测试] 全模式分词...
  结果: 我╱来到╱北京╱清华╱清华大学╱华大╱大学
  通过 ✓
[测试] 搜索引擎模式分词...
  结果: 小明╱硕士╱毕业╱于╱中国╱科学╱学院╱科学院╱中国科学院╱计算╱计算所╱，╱后╱在╱日本╱京都╱大学╱日本京都大学╱深造
  通过 ✓
[测试] 词性标注...
  基础结果: 我/r╱爱/v╱北京/ns╱天安门/ns
  扩展区汉字+Emoji: 从/p╱𧒽岗/nz╱出发/v╱去/v╱吃/v╱𰻝𰻝面/nz╱，/x╱今天/t╱😀/x╱很/zg╱开心/v╱😊/x
  扩展区汉字词性nz: ✓
  Emoji词性x: ✓
  通过 ✓
[测试] TF-IDF关键词提取...
  结果: 欧亚╱增资╱置业╱4.3╱2.2
  基础测试通过 ✓
  [扩展区汉字+Emoji+ZWJ+变体选择符混合测试]
    输入: 从𧒽岗出发去吃𰻝𰻝面，经过石𬒔，今天😀很开心😊笑死了🤣，这是👨‍👩‍👧‍👦全家福和👨‍👨‍👧家庭，我爱❤️和▶︎视频，𰻝𰻝面是陕西特色面食
    结果: 𰻝𰻝面╱𧒽岗╱石𬒔╱😀╱😊╱🤣╱👨‍👩‍👧‍👦╱全家福╱👨‍👨‍👧╱❤️╱▶︎╱面食╱开心╱视频╱特色
    扩展区汉字: ✓
    基础Emoji: ✓
    ZWJ序列: ✓
    变体选择符: ✓
  通过 ✓
[测试] TextRank关键词提取...
  结果: 置业╱欧亚╱有限公司╱增资╱子公司
  基础测试通过 ✓
  [扩展区汉字+Emoji+ZWJ+变体选择符混合测试]
    输入: 从𧒽岗出发去吃𰻝𰻝面，经过石𬒔，今天😀很开心😊笑死了🤣，这是👨‍👩‍👧‍👦全家福和👨‍👨‍👧家庭，我爱❤️和▶︎视频，𰻝𰻝面是陕西特色面食
    结果: 𰻝𰻝面╱陕西╱家庭╱全家福╱面食╱特色╱𧒽岗╱出发╱视频╱石𬒔
    扩展区汉字: ✓
    基础Emoji: ✓（TextRank按词性过滤，emoji词性为x不在默认列表中）
    ZWJ序列: ✓（同上）
    变体选择符: ✓（同上）
  通过 ✓
[测试] 分词Tokenize...
  原文: 南京市长江大桥
  基础结果: 南京市[0,3], 长江大桥[3,7]
  原文: 𧒽岗𰻝𰻝面😀👨‍👩‍👧‍👦❤️▶︎开心
  扩展区汉字+Emoji默认模式:
    word 𧒽岗 start: 0 end: 2
    word 𰻝𰻝面 start: 2 end: 5
    word 😀 start: 5 end: 6
    word 👨‍👩‍👧‍👦 start: 6 end: 7
    word ❤️ start: 7 end: 8
    word ▶︎ start: 8 end: 9
    word 开心 start: 9 end: 11
  扩展区汉字位置: ✓
  基础Emoji位置: ✓
  ZWJ序列Emoji: ✓
  变体选择符Emoji: ✓
  扩展区汉字+Emoji搜索模式:
    word 𧒽岗 start: 0 end: 2
    word 𰻝𰻝面 start: 2 end: 5
    word 😀 start: 5 end: 6
    word 👨 start: 6 end: 6
    word 👩 start: 6 end: 6
    word 👧 start: 6 end: 6
    word 👦 start: 6 end: 7
    word 👨‍👩‍👧‍👦 start: 6 end: 7
    word ❤️ start: 7 end: 8
    word ▶︎ start: 8 end: 9
    word 开心 start: 9 end: 11
  通过 ✓
[测试] Emoji分词...
  输入: 今天天气真好😀明天去爬山🎉
  结果: 今天天气╱真╱好╱😀╱明天╱去╱爬山╱🎉
  通过 ✓
[测试] 复杂Emoji分词（ZWJ序列、变体选择符、肤色修饰）...
  ZWJ序列: 这是👨‍👨‍👧家庭 -> 这是╱👨‍👨‍👧╱家庭
  变体选择符: 今天看了▶︎视频 -> 今天╱看╱了╱▶︎╱视频
  肤色修饰: 他是👨🏻‍⚕️医生 -> 他╱是╱👨🏻‍⚕️╱医生
  国旗emoji: 我爱🇨🇳中国 -> 我╱爱╱🇨🇳╱中国
原始文本: 🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇨🇳🇨🇳🇨🇳🇨🇳🇨🇨🇨🇨🇨🇨🇨🇳🇨🇳🇨🇳🇨🇨🇳🇨🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇨🇳🇨🇳🇨🇳🇨🇨🇳🇨🇳🇨🇳🇨🇳🇨🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳 分词结果:🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇨🇨╱🇨🇨╱🇨🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇨🇳╱🇨🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳
  国旗消歧(18x🇳🇨+18x🇨🇳+6x🇨🇨): 长度=42, 期望=42
  🇳🇨: 18 (期望18), 🇨🇳: 18 (期望18), 🇨🇨: 6 (期望6)
  肤色emoji序列: 长度=6, 期望=6 -> 👋🏽╱👉🏿╱👉🏾╱👉🏽╱👉🏼╱👉🏻
  通过 ✓
[测试] 繁体中文分词...
  输入: 我來到北京清華大學
  结果: 我╱來到╱北京╱清華大學
  通过 ✓
[测试] Unicode 16.0 Emoji分词（指纹🫆）...
  输入: 这是我的🫆指纹
  结果: 这╱是╱我╱的╱🫆╱指纹
  通过 ✓
[测试] Counter词频统计...
  词频统计结果（前5）:
    的: 4
    ，: 3
    算法: 3
    计算: 3
    。: 3
  基础测试通过 ✓
  [Counter<string> 默认模式（过滤emoji）]
    输入: 从𧒽岗出发去吃𰻝𰻝面，经过石𬒔，今天😀很开心😊笑死了🤣，这是👨‍👩‍👧‍👦全家福和👨‍👨‍👧家庭，我爱❤️和▶︎视频，𰻝𰻝面是陕西特色面食
    词频结果:
      ，: 5
      𰻝𰻝面: 2
      和: 2
      从: 1
      𧒽岗: 1
      出发: 1
      去: 1
      吃: 1
      经过: 1
      石𬒔: 1
      今天: 1
      很: 1
      开心: 1
      笑: 1
      死: 1
      了: 1
      这是: 1
    扩展区汉字: ✓
    Emoji已过滤: ✓
  [Counter<string> CountEmoji模式（保留emoji）]
    词频结果:
      ，: 5
      𰻝𰻝面: 2
      和: 2
      从: 1
      𧒽岗: 1
      出发: 1
      去: 1
      吃: 1
      经过: 1
      石𬒔: 1
      今天: 1
      😀: 1
      很: 1
      开心: 1
      😊: 1
      笑: 1
      死: 1
    基础Emoji: ✓
    ZWJ序列: ✓
    变体选择符: ✓
  通过 ✓
[测试] KeywordProcessor关键词提取...
  输入: 你需要通过cet-4考试，学习c语言、.NET core、网络 编程、JavaScript，掌握字典 tree的用法
  提取结果: CET-4, C语言, .NET Core, 网络 编程, 字典 tree
  基础测试通过 ✓
  [扩展区汉字+Emoji+ZWJ+变体选择符混合测试]
    输入: 从𧒽岗出发去吃𰻝𰻝面，经过石𬒔，今天😀很开心😊笑死了🤣，这是👨‍👩‍👧‍👦全家福和👨‍👨‍👧家庭，我爱❤️和▶︎视频，𰻝𰻝面是陕西特色面食
    结果: 𧒽岗, 𰻝𰻝面, 石𬒔, 😀, 😊, 🤣, 👨‍👩‍👧‍👦, 👨‍👨‍👧, ❤️, ▶︎, 𰻝𰻝面
    扩展区汉字: ✓
    基础Emoji: ✓
    ZWJ序列: ✓
    变体选择符: ✓
  通过 ✓
[测试] 日期时间比值版本号分词...
  测试1: 今天4:50某某某领了一只记号笔
  结果: 今天4:50╱某某某╱领了╱一只╱记号笔
  测试2: 会议时间是2021-01-01 09:00:00
  结果: 会议╱时间╱是╱2021-01-01 09:00:00
  测试3: 2021年1月1日是元旦
  结果: 2021年1月1日╱是╱元旦
  测试4: 春节是中国的传统节日
  结果: 春节╱是╱中国╱的╱传统节日
  测试5: 明天下午3点开会
  结果: 明天下午3点╱开会
  测试6: 金龙鱼1:1:1调和油
  结果: 金龙鱼╱1:1:1╱调和油
  测试7: 比值是100:31
  结果: 比值╱是╱100:31
  测试8: 毫秒时间14:30:00.123
  结果: 毫秒╱时间╱14:30:00.123
  测试9: 黄金比例1:1.618
  结果: 黄金╱比例╱1:1.618
  测试10: 现在是北京时间八点整
  结果: 现在╱是╱北京时间╱八点整
  测试11: 会议在上午六点整开始
  结果: 会议╱在╱上午六点整╱开始
  测试12: 当前版本是v1.0.1
  结果: 当前╱版本╱是╱v1.0.1
  测试13: 软件版本1.0.1已发布
  结果: 软件版本1.0.1╱已╱发布
  测试14: 这是3.2-preview1版本
  结果: 这是╱3.2-preview1版本
  测试15: 发布候选版本4.1.2-rc1
  结果: 发布╱候选版本4.1.2-rc1
  测试16: 这是2.1-alpha1测试版
  结果: 这是╱2.1-alpha1测试版
  测试17: 当前是6.3-beta2版本
  结果: 当前╱是╱6.3-beta2版本
  测试18: 2026年1月13日19点03分14秒
  结果: 2026年1月13日19点03分14秒
  测试19: 二零二六年一月十三日十九点零三分十四秒
  结果: 二零二六年一月十三日十九点零三分十四秒
  测试20: 二零二六年一月十三日十九点二十分十四秒
  结果: 二零二六年一月十三日十九点二十分十四秒
  测试21: 十九点二十分十四秒
  结果: 十九点二十分十四秒
  测试22: 十九点二十分
  结果: 十九点二十分
  测试23: 十九点
  结果: 十九点
  测试24: 某人考试得了零分
  结果: 某人╱考试╱得╱了╱零分
  测试25: 三分天下
  结果: 三分╱天下
  测试26: 再等十九分二十秒，就要结束考试了
  结果: 再╱等╱十九分二十秒╱，╱就要╱结束╱考试╱了
  测试27: 再等19分20秒，就要结束考试了
  结果: 再╱等╱19分20秒╱，╱就要╱结束╱考试╱了
  测试28: 我是二零一零年出生的
  结果: 我╱是╱二零一零年╱出生╱的
  测试29: 我是二〇一〇年出生的
  结果: 我╱是╱二〇一〇年╱出生╱的
  测试30: 我是二零一零年五月出生的
  结果: 我╱是╱二零一零年五月╱出生╱的
  测试31: 我是二〇一〇年五月出生的
  结果: 我╱是╱二〇一〇年五月╱出生╱的
  测试32: 我是二零一零年五月一日出生的
  结果: 我╱是╱二零一零年五月一日╱出生╱的
  测试33: 我是二〇一〇年五月一日出生的
  结果: 我╱是╱二〇一〇年五月一日╱出生╱的
  通过 ✓
[测试] 日期时间词性标注...
  测试1: 今天4:50某某某领了一只记号笔
  结果: 今天4:50/t╱某某某/r╱领/v╱了/ul╱一只/m╱记号笔/n
  测试2: 比值是100:31
  结果: 比值/n╱是/v╱100:31/n
  测试3: 时间是14:30
  结果: 时间/n╱是/v╱14:30/t
  通过 ✓
[测试] lcut 直接返回 List<string>...
  结果: 我╱来到╱北京╱清华大学
  通过 ✓
[测试] lcut_for_search 直接返回 List<string>...
  结果: 小明╱硕士╱毕业╱于╱中国╱科学╱学院╱科学院╱中国科学院╱计算╱计算所
  通过 ✓
[测试] Tokenizer 自定义分词器...
  结果: 我╱来到╱北京╱清华大学
  通过 ✓
[测试] Jieba.Dt 默认分词器...
  结果: 我╱来到╱北京╱清华大学
  通过 ✓
[测试] Tokenizer 独立词典...
  tokenizer1: 小明╱最近╱在╱学习╱机器学习
  tokenizer2: 小明╱最近╱在╱学习╱机器╱学习
  通过 ✓
[测试] 连字符╱下划线连接单词分词...
  测试1: TF-IDF识别方法
  结果: TF-IDF╱识别方法
  测试2: word1_word2_word3
  结果: word1_word2_word3
  测试3: hello-world
  结果: hello-world
  测试4: test_case_example
  结果: test_case_example
  通过 ✓
[测试] 域名╱URL分词...
  测试1: https://gitee.com/JTsamsde/AOTba
  结果: https://gitee.com/JTsamsde/AOTba
  测试2: http://www.baidu.com/search?q=test
  结果: http://www.baidu.com/search?q=test
  测试3: gitee.com
  结果: gitee.com
  测试4: gitee.com/JTsamsde/AOTba
  结果: gitee.com/JTsamsde/AOTba
  测试5: 访问https://github.com查看代码
  结果: 访问╱https://github.com╱查看╱代码
  测试6: 访问gitee.com/JTsamsde/AOTba查看代码
  结果: 访问╱gitee.com/JTsamsde/AOTba╱查看╱代码
  测试7: www.baidu.com
  结果: www.baidu.com
  测试8: nuget.org
  结果: nuget.org
  通过 ✓
[测试] GB18030-2022扩展B-I区生僻字分词...
  测试1: 𩽾𩾌是深海中的一种鱼类
  结果: 𩽾𩾌╱是╱深海╱中╱的╱一种╱鱼类
  测试2: 南海有轨电车一号线，起点为𧒽岗，终点为林岳东
  结果: 南海有轨电车一号线╱，╱起点╱为╱𧒽岗╱，╱终点╱为╱林岳东
  测试3: 石𬒔是佛山市南海区桂城街道的一个地名
  结果: 石𬒔╱是╱佛山市╱南海区╱桂城街道╱的╱一个╱地名
  测试4: 我今天吃了𰻝𰻝面，很好吃
  结果: 我╱今天╱吃╱了╱𰻝𰻝面╱，╱很╱好吃
  测试5: 半径的日本新字体字形是半𮱻，繁体写作半徑
  结果: 半径╱的╱日本新字体╱字形╱是╱半𮱻╱，╱繁体╱写作╱半徑
  测试6: 从𧒽岗出发，经过石𬒔，最后去吃𰻝𰻝面和𩽾𩾌料理
  结果: 从╱𧒽岗╱出发╱，╱经过╱石𬒔╱，╱最后╱去╱吃╱𰻝𰻝面╱和╱𩽾𩾌╱料理
  测试7: 二〇一〇年
  结果: 二〇一〇年
  通过 ✓
[测试] EntityProtect.Disabled 禁用实体保护（OpenCC场景）...
  测试1: 2026年4月30日晚上9点开会
  结果: 2026╱年╱4╱月╱30╱日╱晚上╱9╱点╱开会
  测试2: 软件版本1.0.1已发布
  结果: 软件╱版本╱1.0╱.╱1╱已╱发布
  测试3: 访问https://github.com查看代码
  结果: 访问╱https╱:╱/╱/╱github╱.╱com╱查看╱代码
  测试4: 我来到北京清华大学
  结果: 我╱来到╱北京╱清华大学
  通过 ✓
[测试] Windows版本识别分词...
  测试1: 我使用的是Windows 10操作系统
  结果: 我╱使用╱的╱是╱Windows 10╱操作系统
  测试2: 我使用的是Windows7操作系统
  结果: 我╱使用╱的╱是╱Windows7╱操作系统
  测试3: 我使用的是Win 7操作系统
  结果: 我╱使用╱的╱是╱Win 7╱操作系统
  测试4: 我使用的是Win7操作系统
  结果: 我╱使用╱的╱是╱Win7╱操作系统
  测试5: 我使用的是Microsoft Windows 10操作系统
  结果: 我╱使用╱的╱是╱Microsoft Windows 10╱操作系统
  测试6: 我使用的是Microsoft(R) Windows(R) 11操作系统
  结果: 我╱使用╱的╱是╱Microsoft(R) Windows(R) 11╱操作系统
  测试7: 我使用的是Microsoft® Windows® 11操作系统
  结果: 我╱使用╱的╱是╱Microsoft® Windows® 11╱操作系统
  测试8: 服务器运行Windows Server 2022
  结果: 服务器╱运行╱Windows Server 2022
  测试9: 老电脑运行Windows XP系统
  结果: 老电脑╱运行╱Windows XP╱系统
  测试10: Windows Vista是Windows 7的前身
  结果: Windows Vista╱是╱Windows 7╱的╱前身
  测试11: 视窗95是早期的中文Windows版本
  结果: 视窗95╱是╱早期╱的╱中文╱Windows╱版本
  测试12: 视窗 10是目前最新的Windows版本
  结果: 视窗 10╱是╱目前╱最新╱的╱Windows╱版本
  测试13: Win32是Windows的32位API
  结果: Win32╱是╱Windows╱的╱32╱位╱API
  实体: 
  测试14: Win64是Windows的64位API
  结果: Win64╱是╱Windows╱的╱64╱位╱API
  实体: 
  测试15: 我使用的是windows 10操作系统
  结果: 我╱使用╱的╱是╱windows 10╱操作系统
  测试16: Windows 12预计明年发布
  结果: Windows 12╱预计╱明年╱发布
  测试17: Windows Server 2025是最新的服务器版本
  结果: Windows Server 2025╱是╱最新╱的╱服务器╱版本
  测试18: Windows 8.1是Windows 8的升级版
  结果: Windows 8.1╱是╱Windows 8╱的╱升级版
  测试19: Windows 2000是NT 5.0的商业名称
  结果: Windows 2000╱是╱NT╱ ╱5.0╱的╱商业╱名称
  测试20: Windows NT 4.0发布于1996年
  结果: Windows NT 4.0╱发布╱于╱1996年
  通过 ✓
[测试] 带空格词分词...
  测试1: Kimi K2.5是一个大语言模型
  结果: Kimi K2.5╱是╱一个╱大语言╱模型
  测试2: GPT 4o是OpenAI的多模态模型
  结果: GPT 4o╱是╱OpenAI╱的╱多模态╱模型
  测试3: Claude 3.5 Sonnet是Anthropic的模型
  结果: Claude 3.5 Sonnet╱是╱Anthropic╱的╱模型
  测试4: 对比Kimi K2.5和GPT 4o的性能
  结果: 对比╱Kimi K2.5╱和╱GPT 4o╱的╱性能
  通过 ✓
[测试] 词典词与时间实体冲突...
  测试1: 百年孤独
  结果: 百年孤独
  测试2: 千年老二
  结果: 千年老二
  测试3: 百年纪念
  结果: 百年纪念
  测试4: 百年诞辰
  结果: 百年诞辰
  测试5: 百年之后
  结果: 百年之后
  测试6: 百年孤独与2021年1月1日的故事
  结果: 百年孤独╱与╱2021年1月1日╱的╱故事
  批量验证34个词典词: 全部通过
  通过 ✓
[测试] 词典词与时间实体过滤性能...
  5条文本 × 1000次 = 5000次分词
  总耗时: 463.787ms
  平均每次: 92.757μs (0.093ms)
  性能达标 ✓（毫秒级以内）
  通过 ✓
[测试] AddWord 添加超长词典词（>30字符）...
  文本: 这是北京召开的中国共产党第十九届中央委员会第四次全体会议发布公报全文的职责范围
  分词: 这是╱北京召开的中国共产党第十九届中央委员会第四次全体会议发布公报全文╱的╱职责╱范围
  MaxScanLength 已扩展为 32（词长=32）
  通过 ✓
[测试] AddWord 添加包含时间实体的词典词（12点口令）...
  文本: 我的12点口令已更新，请查收
  分词: 我╱的╱12点口令╱已╱更新╱，╱请╱查收
  对照: 独立'12点'正常识别 ✓
  通过 ✓

=== 所有AOT测试通过！ ===
```

### 2. 添加自定义词典

#### 加载词典

* 开发者可以指定自定义的词典，以便包含jieba词库里没有的词。虽然jieba有新词识别能力，但是自行添加新词可以保证更高的正确率
* `JiebaSegmenter.LoadUserDict("user_dict_file_path")`
* 词典格式与主词典格式相同，即一行包含：词、词频（可省略）、词性（可省略），用空格隔开
* 仅不带空格的词可以省略词频与词性
* 词频省略时，分词器将使用自动计算出的词频保证该词被分出
* 注意：若词为带空格的词（比如Kimi K2.5），词频和词性不能省略。

如

```
创新办 3 i
云计算 5
凱特琳 nz
台中
机器学习 3
Kimi K2.5 3000 nz
```

#### 调整词典

* 使用`JiebaSegmenter.AddWord(word, freq=0, tag=null)`可添加一个新词，或调整已知词的词频；若`freq`不是正整数，则使用自动计算出的词频，计算出的词频可保证该词被分出来
* 使用`JiebaSegmenter.DeleteWord(word)`可移除一个词，使其不能被分出来

### 3. 关键词提取

#### 基于TF-IDF算法的关键词提取

* `JiebaNet.Analyser.TfidfExtractor.ExtractTags(string text, int count = 20, IEnumerable<string> allowPos = null)`可从指定文本中抽取出关键词。
* `JiebaNet.Analyser.TfidfExtractor.ExtractTagsWithWeight(string text, int count = 20, IEnumerable<string> allowPos = null)`可从指定文本中**抽取关键词的同时得到其权重**。
* 关键词抽取基于逆向文件频率（IDF），组件内置一个IDF语料库，可以配置为其它自定义的语料库。
* 关键词抽取会过滤停用词（Stop Words），组件内置一个停用词语料库，这个语料库合并了NLTK的英文停用词和哈工大的中文停用词。

#### 基于TextRank算法的关键词抽取

* `JiebaNet.Analyser.TextRankExtractor`与`TfidfExtractor`相同的接口。需要注意的是，`TextRankExtractor`默认情况下只提取名词和动词。
* 以固定窗口大小（默认为5，通过Span属性调整）和词之间的共现关系构建图

### 4. 词性标注

* `JiebaNet.Segmenter.PosSeg.PosSegmenter`类可以在分词的同时，为每个词添加词性标注。
* 词性标注采用和ictclas兼容的标记法，关于ictclas和jieba中使用的标记法列表，请参考：[词性标记](https://gist.github.com/luw2007/6016931)。
* 词性标注支持扩展区汉字（GB18030-2022）和Emoji，扩展区汉字标注为`nz`（其他专名），Emoji标注为`x`（非语素字）。

```c#
var posSeg = new PosSegmenter();
var s = "从𧒽岗出发去吃𰻝𰻝面，今天😀很开心😊";

var tokens = posSeg.Cut(s);
Console.WriteLine(string.Join(" ", tokens.Select(token => string.Format("{0}/{1}", token.Word, token.Flag))));
```

```
从/p 𧒽岗/nz 出发/v 去/v 吃/v 𰻝𰻝面/nz ，/x 今天/t 😀/x 很/zg 开心/v 😊/x
```

### 5. Tokenize：返回词语在原文的起止位置

起止位置基于字形簇（Grapheme Cluster）计算，而非char偏移，因此扩展区汉字和Emoji的位置与用户感知一致。

* 默认模式：保留ZWJ序列Emoji、变体选择符Emoji的整体性，不拆分

```c#
var segmenter = new JiebaSegmenter();
var s = "𧒽岗𰻝𰻝面😀👨‍👩‍👧‍👦❤️▶︎开心";
var tokens = segmenter.Tokenize(s);
foreach (var token in tokens)
{
    Console.WriteLine("word {0,-12} start: {1,-3} end: {2,-3}", token.Word, token.StartIndex, token.EndIndex);
}
```

```
word 𧒽岗          start: 0   end: 2
word 𰻝𰻝面        start: 2   end: 5
word 😀           start: 5   end: 6
word 👨‍👩‍👧‍👦          start: 6   end: 7
word ❤️           start: 7   end: 8
word ▶︎           start: 8   end: 9
word 开心           start: 9   end: 11
```

* 搜索模式：对长词提取子词以提高召回率，ZWJ序列Emoji会被拆出子Emoji（有意设计，与中文长词拆子词逻辑一致）

```c#
var segmenter = new JiebaSegmenter();
var s = "𧒽岗𰻝𰻝面😀👨‍👩‍👧‍👦❤️▶︎开心";
var tokens = segmenter.Tokenize(s, TokenizerMode.Search);
foreach (var token in tokens)
{
    Console.WriteLine("word {0,-12} start: {1,-3} end: {2,-3}", token.Word, token.StartIndex, token.EndIndex);
}
```

```
word 𧒽岗          start: 0   end: 2
word 𰻝𰻝面        start: 2   end: 5
word 😀           start: 5   end: 6
word 👨            start: 6   end: 6
word 👩            start: 6   end: 6
word 👧            start: 6   end: 6
word 👦            start: 6   end: 7
word 👨‍👩‍👧‍👦          start: 6   end: 7
word ❤️           start: 7   end: 8
word ▶︎           start: 8   end: 9
word 开心           start: 9   end: 11
```

### 6. 并行分词

使用如下方法：

* `JiebaSegmenter.CutInParallel()`、`JiebaSegmenter.CutForSearchInParallel()`
* `PosSegmenter.CutInParallel()`

### 7. 与Lucene.NET的集成

jiebaForLuceneNet项目提供了与Lucene.NET的简单集成，更多信息请看：[jiebaForLuceneNet](https://github.com/anderscui/jiebaForLuceneNet/wiki/%E4%B8%8ELucene.NET%E7%9A%84%E9%9B%86%E6%88%90)

### 8. 其它词典

jieba分词亦提供了其它的词典文件：

* 占用内存较小的词典文件 [https://raw.githubusercontent.com/anderscui/jieba.NET/master/ExtraDicts/dict.txt.small](https://raw.githubusercontent.com/anderscui/jieba.NET/master/ExtraDicts/dict.txt.small)
* 支持繁体分词更好的词典文件 [https://raw.githubusercontent.com/anderscui/jieba.NET/master/ExtraDicts/dict.txt.big](https://raw.githubusercontent.com/anderscui/jieba.NET/master/ExtraDicts/dict.txt.big)

### 9. 分词速度

* 全模式：2.5 MB/s
* 精确模式：1.1 MB/s
* 测试环境： Intel(R) Core(TM) i3-2120 CPU @ 3.30GHz；围城.txt（734KB）

### 10. 命令行分词

Segmenter.Cli项目build之后得到jiebanet.ext，它的选项和实例用法如下：

```shell
-f       --file          the file name, (必要的).
-d       --delimiter     the delimiter between tokens, default: / .
-a       --cut-all       use cut_all mode.
-n       --no-hmm        don't use HMM.
-p       --pos           enable POS tagging.
-v       --version       show version info.
-h       --help          show help details.

sample usages:
$ jiebanet -f input.txt > output.txt
$ jiebanet -d | -f input.txt > output.txt
$ jiebanet -p -f input.txt > output.txt
```

### 11. 词频统计

可以使用`Counter`类统计词频，其实现来自Python标准库的Counter类（具体接口和实现细节略有不同）。

`Counter<string>`支持两种emoji处理模式，适合不同类型的词云图制作：

- **默认模式**（`countEmoji: false`，默认值）：过滤emoji词频，仅统计文字词，适合制作纯文字词云图
- **Emoji提取模式**（`countEmoji: true`）：保留emoji词频，适合制作含emoji的词云图

Emoji过滤基于`GraphemeClusterHelper.IsEmojiGrapheme()`检测，可正确识别ZWJ序列、变体选择符、肤色修饰符等复杂emoji。

```c#
var seg = new JiebaSegmenter();
var s = "从𧒽岗出发去吃𰻝𰻝面，经过石𬒔，今天😀很开心😊笑死了🤣";

// 默认模式：过滤emoji，仅统计文字词
var freqs = new Counter<string>(seg.Cut(s));
foreach (var pair in freqs.MostCommon(10))
{
    Console.WriteLine($"{pair.Key}: {pair.Value}");
}

// Emoji提取模式：保留emoji词频
var emojiFreqs = new Counter<string>(seg.Cut(s), countEmoji: true);
foreach (var pair in emojiFreqs.MostCommon(17))
{
    Console.WriteLine($"{pair.Key}: {pair.Value}");
}
```

默认模式输出：

```bash
，: 2
从: 1
𧒽岗: 1
出发: 1
去: 1
吃: 1
𰻝𰻝面: 1
经过: 1
石𬒔: 1
今天: 1
```

Emoji提取模式输出：

```bash
，: 2
从: 1
𧒽岗: 1
出发: 1
去: 1
吃: 1
𰻝𰻝面: 1
经过: 1
石𬒔: 1
今天: 1
😀: 1
很: 1
开心: 1
😊: 1
笑: 1
死: 1
了: 1
```

`Counter`类可通过`Add`，`Subtract`和`Union`方法进行修改，最后以`MostCommon`方法获得频率最高的若干词。具体用法可见测试用例。

### 12. KeywordProcessor

可通过 `KeywordProcessor` 提取文本中的关键词，不过它的提取与 `KeywordExtractor`不同。`KeywordProcessor` 可理解为基于词典从文本中找出已知的词，仅仅如此。

jieba分词当前的实现里，不能处理忽略大小写、含空格的词之类的情况，而在**文本提取**应用中，这是很常见的场景。因此 `KeywordProcessor` 主要是作为提取之用，而非分词，尽管通过其中的方法，可以实现另一种基于字典的分词模式。

代码示例：

```c#
var kp = new KeywordProcessor();
kp.AddKeywords(new []{"𰻝𰻝面", "𧒽岗", "石𬒔", ".NET Core", "C语言", "字典 tree", "CET-4", "网络 编程"});

var keywords = kp.ExtractKeywords("你需要通过cet-4考试，去𧒽岗吃𰻝𰻝面，学习c语言、.NET core、网络 编程、JavaScript，掌握字典 tree的用法，经过石𬒔");

// keywords 值为：
// new List<string> { "CET-4", "𧒽岗", "𰻝𰻝面", "C语言", ".NET Core", "网络 编程", "字典 tree", "石𬒔"}

// 可以看到，结果中的词与开始添加的关键词相同，与输入句子中的词则不尽相同。如果需要返回句中找到的原词，可以使用 `raw` 参数。

var keywords = kp.ExtractKeywords("你需要通过cet-4考试，去𧒽岗吃𰻝𰻝面，学习c语言、.NET core、网络 编程、JavaScript，掌握字典 tree的用法，经过石𬒔", raw: true);

// keywords 值为：
// new List<string> { "cet-4", "𧒽岗", "𰻝𰻝面", "c语言", ".NET core", "网络 编程", "字典 tree", "石𬒔"}
```

### 13. 实体提取

可以提取日期、时间、域名、版本号等多种实体

使用ITimeRecognizer recognizer = new RegexTimeRecognizer();方法进行实体提取。

为了防止gitee镜像误判readme，此处不展示具体实体识别结果。具体代码可见TimeRecognizerDemo
