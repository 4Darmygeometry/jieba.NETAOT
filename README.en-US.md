jieba.NETAOT (AOTba) is the .NET version (C# implementation) of [jieba Chinese Segmentation](https://github.com/fxsjy/jieba), supporting AOT compilation.

The current version is 1.1.11, based on jieba 0.42. It provides functions and interfaces **basically consistent** with jieba, but it does not support the latest paddle mode (if you need paddle mode, please see https://github.com/sdcb/PaddleSharp/blob/master/docs%2Fpaddlenlp-lac.md). For the implementation logic of jieba, you can refer to the materials mentioned in [this wiki](https://github.com/anderscui/jieba.NET/wiki/%E7%90%86%E8%A7%A3%E7%BB%93%E5%B7%B4%E5%88%86%E8%AF%8D).

Additionally, it provides `KeywordProcessor`, based on the implementation of [FlashText](https://github.com/vi3k6i5/flashtext). `KeywordProcessor` can more flexibly extract **keywords from a dictionary** from text, such as ignoring case or handling words containing spaces.

If you encounter any requirements or difficulties related to word segmentation during development, please submit an Issue. I see u:)

## Features

* Supports three segmentation modes:
    - Accurate mode: attempts to cut sentences as precisely as possible, suitable for **text analysis**;
    - All mode: scans all possible words in the sentence. **Very fast, but cannot resolve ambiguity**. Specifically, the segmentation process does not rely on word frequency to find the maximum probability path, nor does it use HMM;
    - Search engine mode: based on accurate mode, it further splits long words to increase recall, **suitable for search engine segmentation**.
* Supports **Traditional Chinese segmentation**
* Supports adding custom dictionaries and custom words
* Supports `lcut` and `lcutforsearch` returning lists directly
* Supports full extraction of dates/times without splitting them (e.g., "3:30 PM", "8:30 PM", "2021-01-01 09:00:00")
* Supports ratio extraction (e.g., extracting "1:1:1" from "Jinlongyu 1:1:1 blended oil")
* Supports domain name extraction (e.g., https://gitee.com/JTsamsde/AOTba)
* Supports full extraction of words with underscores/hyphens (e.g., TF-IDF)
* Supports version number extraction (e.g., v1.0.1, 1.0.1, 3.2-preview1, 4.1.2-rc1, 2.1-alpha1, 6.3-beta2)
* Supports asynchronous dictionary loading
* Supports keyword extraction using TF-IDF, TextRank, and KeywordProcessor algorithms
* `Counter` word frequency statistics support two modes: counting emojis and filtering emojis, suitable for different types of word clouds
* Supports entity extraction for dates, times, links, version numbers, etc.
* Supports sentence segmentation containing Emojis (with the ability to recognize unrecorded emojis)
* Supports complex emoji segmentation with variation selectors, ZWJ, skin tone modifiers, and regional indicators (supporting emojis up to Unicode 16)
* Supports enabling or disabling entity protection for OpenCC.NET calls
* Supports words containing spaces (e.g., Kimi K2.5)
* Full support for GB18030-2022 Level 3 and Amendment No. 1 requirements (handling Basic zone to Extension I zone characters, 〇, and Kangxi radicals)
* AOT compilable, runs smoothly on pure CPU
* Built-in regex timeout circuit breaker protection to prevent ReDoS attacks
* Versions 1.0.9 and earlier are under the MIT license; version 1.0.10 and later are dual-licensed under Apache 2.0 and MIT, allowing commercial closed-source release
* 100% backward compatible with jieba.NET syntax; migration can be completed simply by changing the NuGet package
* Supports Windows 7 SP1 and above (Windows 11 24H2 or above is required to fully display all GB18030-2022 text)

## Algorithms

* Efficient word graph scanning based on prefix dictionaries, generating a Directed Acyclic Graph (DAG) consisting of all possible word formations of Chinese characters in a sentence.
* Employs dynamic programming to find the maximum probability path and determine the best split combination based on word frequency.
* For out-of-vocabulary (OOV) words, an HMM model based on character-forming capability is used with the Viterbi algorithm.
* Entity recognition for dates and times is based on regular expressions, combined with filtering algorithms to avoid extracting non-time dictionary words (e.g., "One Hundred Years of Solitude").
* Compliance with GB18030-2022 is implemented via `GB18030_2022.cs`.
* Emoji recognition is implemented via `RuneHelper.cs` and `GraphemeClusterHelper.cs`.

## Installation and Configuration

If modifying from source, ensure Visual Studio version is 2026 or higher before installation/configuration.

If installing the NuGet package, ensure Visual Studio version is 2019 or higher.

The current version supports net10.0, net48, netstandard2.0, and netstandard2.1 (compatible with .NET 6+). You can manually reference the project or add it via NuGet:

```shell
PM> Install-Package AOTba
```

If the software using this library runs on an OS with an NT5.1 kernel, please refer to the [Bilibili article installation method](https://www.bilibili.com/opus/1044900873850847240) to install .NET Framework 4.8.

After installation, you will find the `Resources` directory under `packages\jieba.NET`. This contains the dictionaries and other data files required for jieba.NET to run. The simplest configuration is to copy the entire `Resources` directory to the assembly directory; jieba.NET will then use the built-in default configuration. If you wish to place these files elsewhere, add the following configuration item to `app.config` or `web.config`:

```xml
<appSettings>
    <add key="JiebaConfigFileDir" value="C:\jiebanet\config" />
</appSettings>
```

Note that this path can be absolute or relative. **If a relative path is used, jieba.NET assumes it is relative to the BaseDirectory of the current application domain**.

Configuration examples:

* Using an absolute path: if the config is `C:\jiebanet\config`, the main dictionary path will be concatenated as: `C:\jiebanet\config\dict.txt`.
* Using a relative path (or if no config is added, the default **relative path is Resources**): if the config is `..\config` (relative paths can be adjusted using `..`) and the current application domain BaseDirectory is `C:\myapp\bin\`, the main dictionary path will be concatenated as: `C:\myapp\config\dict.txt`.

### Configuring Dictionary Path via Code

If it is inconvenient to configure via the application config file, you can set it via code (it is recommended to use absolute paths before using any segmentation features):

```c#
JiebaNet.Segmenter.ConfigManager.ConfigFileBaseDir = @"C:\jiebanet\config";
```

## Main Functions

### 1. Segmentation

* `JiebaSegmenter.Cut` method accepts three input parameters: `text` is the string to be segmented; `cutAll` specifies whether to use all mode; `hmm` specifies whether to use the HMM model for OOV words; returns `IEnumerable<string>`.
* `JiebaSegmenter.CutForSearch` method accepts two input parameters: `text` is the string to be segmented; `hmm` specifies whether to use the HMM model; returns `IEnumerable<string>`.
* `JiebaSegmenter.LCut` method accepts three input parameters: `text` is the string to be segmented; `cutAll` specifies whether to use all mode; `hmm` specifies whether to use the HMM model for OOV words; returns `List<string>`.
* `JiebaSegmenter.LCutForSearch` method accepts two input parameters: `text` is the string to be segmented; `hmm` specifies whether to use the HMM model; returns `List<string>`.

* Additionally, jieba.NETAOT supports custom Tokenizers (independent dictionaries):
```c#
// Note: API for disabling emoji processing was deprecated in version 1.0.10; 
// it now automatically determines if emoji.txt exists.
// Load simplified Chinese library only + support emoji processing
var config = new JiebaConfig(JiebaMode.ZhHans);
var segmenter = new JiebaSegmenter(config);

// Disable entity (date, time, etc.) protection (suitable for OpenCC.NET calls)
var config = new JiebaConfig(EntityProtect.Disabled);
var segmenter = new JiebaSegmenter(config);

// If var segmenter = new JiebaSegmenter();, it performs a full load.

// Tokenizer custom segmenter (independent dictionary)
var tokenizer = new Tokenizer(new JiebaConfig(JiebaMode.ZhHans));
var result = tokenizer.Lcut("我来到北京清华大学");

// jieba.dt default segmenter
var dtResult = Jieba.Lcut("我来到北京清华大学");

// Asynchronous loading
var asyncSegmenter = await JiebaSegmenter.CreateAsync();
```

Code Example:

```c#
using JiebaNet.Segmenter;
var segmenter = new JiebaSegmenter();
var segments = segmenter.Cut("我来到北京清华大学", cutAll: true);
Console.WriteLine("【All Mode】: {0}", string.Join("/ ", segments));

segments = segmenter.Cut("我来到北京清华大学");  // Default is Accurate mode
Console.WriteLine("【Accurate Mode】: {0}", string.Join("/ ", segments));

segments = segmenter.Cut("他来到了网易杭研大厦");  // Accurate mode + HMM model
Console.WriteLine("【New Word Recognition】: {0}", string.Join("/ ", segments));

segments = segmenter.CutForSearch("小明硕士毕业于中国科学院计算所，后在日本京都大学深造"); // Search engine mode
Console.WriteLine("【Search Engine Mode】: {0}", string.Join("/ ", segments));

segments = segmenter.Cut("结过婚的和尚未结过婚的");
Console.WriteLine("【Ambiguity Resolution】: {0}", string.Join("/ ", segments));

// Lcut method returns List<string> directly, no need for ToList() conversion
var words = segmenter.Lcut("我来到北京清华大学");
Console.WriteLine("【Lcut Accurate Mode】: {0}", string.Join("/ ", words));

words = segmenter.Lcut("我来到北京清华大学", cutAll: true);
Console.WriteLine("【Lcut All Mode】: {0}", string.Join("/ ", words));

// LcutForSearch method returns List<string> directly
words = segmenter.LcutForSearch("小明硕士毕业于中国科学院计算所");
Console.WriteLine("【LcutForSearch】: {0}", string.Join("/ ", words));
```

Output:

```
【All Mode】: 我/ 来到/ 北京/ 清华/ 清华大学/ 华大/ 大学
【Accurate Mode】: 我/ 来到/ 北京/ 清华大学
【New Word Recognition】: 他/ 来到/ 了/ 网易/ 杭研/ 大厦
【Search Engine Mode】: 小明/ 硕士/ 毕业/ 于/ 中国/ 科学/ 学院/ 科学院/ 中国科学院/ 计算/ 计算所/ ，/ 后/ 在/ 日本/ 京都/ 大学/ 日本京都大学/ 深造
【Ambiguity Resolution】: 结过婚/ 的/ 和/ 尚未/ 结过婚/ 的
【Lcut Accurate Mode】: 我/ 来到/ 北京/ 清华大学
【Lcut All Mode】: 我/ 来到/ 北京/ 清华/ 清华大学/ 华大/ 大学
【LcutForSearch】: 小明/ 硕士/ 毕业/ 于/ 中国/ 科学/ 学院/ 科学院/ 中国科学院/ 计算/ 计算所
```

Emoji Sentence Segmentation Test in AOT Scenario:

```
=== AOTba AOT Compatibility Test ===

[Test] Accurate mode segmentation...
  Result: 我╱来到╱北京╱清华大学
  Pass ✓
[Test] All mode segmentation...
  Result: 我╱来到╱北京╱清华╱清华大学╱华大╱大学
  Pass ✓
[Test] Search engine mode segmentation...
  Result: 小明╱硕士╱毕业╱于╱中国╱科学╱学院╱科学院╱中国科学院╱计算╱计算所╱，╱后╱在╱日本╱京都╱大学╱日本京都大学╱深造
  Pass ✓
[Test] POS Tagging...
  Basic Result: 我/r╱爱/v╱北京/ns╱天安门/ns
  Ext-Zone Char+Emoji: 从/p╱𧒽岗/nz╱出发/v╱去/v╱吃/v╱𰻝𰻝面/nz╱，/x╱今天/t╱😀/x╱很/zg╱开心/v╱😊/x
  Ext-Zone Char POS nz: ✓
  Emoji POS x: ✓
  Pass ✓
[Test] TF-IDF Keyword Extraction...
  Result: 欧亚╱增资╱置业╱4.3╱2.2
  Basic test pass ✓
  [Ext-Zone Char+Emoji+ZWJ+Variation Selector Mixed Test]
    Input: 从𧒽岗出发去吃𰻝𰻝面，经过石𬒔，今天😀很开心😊笑死了🤣，这是👨‍👩‍👧‍👦全家福和👨‍👨‍👧家庭，我爱❤️和▶︎视频，𰻝𰻝面是陕西特色面食
    Result: 𰻝𰻝面╱𧒽岗╱石𬒔╱😀╱😊╱🤣╱👨‍👩‍👧‍👦╱全家福╱👨‍👨‍👧╱❤️╱▶︎╱面食╱开心╱视频╱特色
    Ext-Zone Char: ✓
    Basic Emoji: ✓
    ZWJ Sequence: ✓
    Variation Selector: ✓
  Pass ✓
[Test] TextRank Keyword Extraction...
  Result: 置业╱欧亚╱有限公司╱增资╱子公司
  Basic test pass ✓
  [Ext-Zone Char+Emoji+ZWJ+Variation Selector Mixed Test]
    Input: 从𧒽岗出发去吃𰻝𰻝面，经过石𬒔，今天😀很开心😊笑死了🤣，这是👨‍👩‍👧‍👦全家福和👨‍👨‍👧家庭，我爱❤️和▶︎视频，𰻝𰻝面是陕西特色面食
    Result: 𰻝𰻝面╱陕西╱家庭╱全家福╱面食╱特色╱𧒽岗╱出发╱视频╱石𬒔
    Ext-Zone Char: ✓
    Basic Emoji: ✓（TextRank filters by POS, emoji POS x is not in default list）
    ZWJ Sequence: ✓（Same as above）
    Variation Selector: ✓（Same as above）
  Pass ✓
[Test] Tokenize...
  Original: 南京市长江大桥
  Basic Result: 南京市[0,3], 长江大桥[3,7]
  Original: 𧒽岗𰻝𰻝面😀👨‍👩‍👧‍👦❤️▶︎开心
  Ext-Zone Char+Emoji Default Mode:
    word 𧒽岗 start: 0 end: 2
    word 𰻝𰻝面 start: 2 end: 5
    word 😀 start: 5 end: 6
    word 👨‍👩‍👧‍👦 start: 6 end: 7
    word ❤️ start: 7 end: 8
    word ▶︎ start: 8 end: 9
    word 开心 start: 9 end: 11
  Ext-Zone Char position: ✓
  Basic Emoji position: ✓
  ZWJ Sequence Emoji: ✓
  Variation Selector Emoji: ✓
  Ext-Zone Char+Emoji Search Mode:
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
  Pass ✓
[Test] Emoji Segmentation...
  Input: 今天天气真好😀明天去爬山🎉
  Result: 今天天气╱真╱好╱😀╱明天╱去╱爬山╱🎉
  Pass ✓
[Test] Complex Emoji Segmentation (ZWJ sequence, Variation selector, Skin tone)...
  ZWJ Sequence: 这是👨‍👨‍👧家庭 -> 这是╱👨‍👨‍👧╱家庭
  Variation Selector: 今天看了▶︎视频 -> 今天╱看╱了╱▶︎╱视频
  Skin Tone: 他是👨🏻‍⚕️医生 -> 他╱是╱👨🏻‍⚕️╱医生
  Flag Emoji: 我爱🇨🇳中国 -> 我╱爱╱🇨🇳╱中国
Original text: 🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇨🇳🇨🇳🇨🇳🇨🇳🇨🇨🇨🇨🇨🇨🇨🇳🇨🇳🇨🇳🇨🇨🇳🇨🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇨🇳🇨🇳🇨🇳🇨🇨🇳🇨🇳🇨🇳🇨🇳🇨🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳🇨🇳 Segmentation result:🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇨🇨╱🇨🇨╱🇨🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇨🇳╱🇨🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇳🇨╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳╱🇨🇳
  Flag disambiguation (18x🇳🇨+18x🇨🇳+6x🇨🇨): length=42, expected=42
  🇳🇨: 18 (exp 18), 🇨🇳: 18 (exp 18), 🇨🇨: 6 (exp 6)
  Skin tone emoji sequence: length=6, expected=6 -> 👋🏽╱👉🏿╱👉🏾╱👉🏽╱👉🏼╱👉🏻
  Pass ✓
[Test] Traditional Chinese Segmentation...
  Input: 我來到北京清華大學
  Result: 我╱來到╱北京╱清華大學
  Pass ✓
[Test] Unicode 16.0 Emoji Segmentation (Fingerprint🫆)...
  Input: 这是我的🫆指纹
  Result: 这╱是╱我╱的╱🫆╱指纹
  Pass ✓
[Test] Counter Word Frequency...
  Frequency results (top 5):
    的: 4
    ，: 3
    算法: 3
    计算: 3
    。: 3
  Basic test pass ✓
  [Counter<string> Default Mode (Filter emoji)]
    Input: 从𧒽岗出发去吃𰻝𰻝面，经过石𬒔，今天😀很开心😊笑死了🤣，这是👨‍👩‍👧‍👦全家福和👨‍👨‍👧家庭，我爱❤️和▶︎视频，𰻝𰻝面是陕西特色面食
    Frequency results:
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
    Ext-Zone Char: ✓
    Emoji filtered: ✓
  [Counter<string> CountEmoji Mode (Keep emoji)]
    Frequency results:
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
    Basic Emoji: ✓
    ZWJ Sequence: ✓
    Variation Selector: ✓
  Pass ✓
[Test] KeywordProcessor Extraction...
  Input: 你需要通过cet-4考试，学习c语言、.NET core、网络 编程、JavaScript，掌握字典 tree的用法
  Result: CET-4, C语言, .NET Core, 网络 编程, 字典 tree
  Basic test pass ✓
  [Ext-Zone Char+Emoji+ZWJ+Variation Selector Mixed Test]
    Input: 从𧒽岗出发去吃𰻝𰻝面，经过石𬒔，今天😀很开心😊笑死了🤣，这是👨‍👩‍👧‍👦全家福和👨‍👨‍👧家庭，我爱❤️和▶︎视频，𰻝𰻝面是陕西特色面食
    Result: 𧒽岗, 𰻝𰻝面, 石𬒔, 😀, 😊, 🤣, 👨‍👩‍👧‍👦, 👨‍👨‍👧, ❤️, ▶︎, 𰻝𰻝面
    Ext-Zone Char: ✓
    Basic Emoji: ✓
    ZWJ Sequence: ✓
    Variation Selector: ✓
  Pass ✓
[Test] Date-Time-Ratio-Version segmentation...
  Test 1: 今天4:50某某某领了一只记号笔
  Result: 今天4:50╱某某某╱领了╱一只╱记号笔
  Test 2: 会议时间是2021-01-01 09:00:00
  Result: 会议╱时间╱是╱2021-01-01 09:00:00
  Test 3: 2021年1月1日是元旦
  Result: 2021年1月1日╱是╱元旦
  Test 4: 春节是中国的传统节日
  Result: 春节╱是╱中国╱的╱传统节日
  Test 5: 明天下午3点开会
  Result: 明天下午3点╱开会
  Test 6: 金龙鱼1:1:1调和油
  Result: 金龙鱼╱1:1:1╱调和油
  Test 7: 比值是100:31
  Result: 比值╱是╱100:31
  Test 8: 毫秒时间14:30:00.123
  Result: 毫秒╱时间╱14:30:00.123
  Test 9: 黄金比例1:1.618
  Result: 黄金╱比例╱1:1.618
  Test 10: 现在是北京时间八点整
  Result: 现在╱是╱北京时间╱八点整
  Test 11: 会议在上午六点整开始
  Result: 会议╱在╱上午六点整╱开始
  Test 12: 当前版本是v1.0.1
  Result: 当前╱版本╱是╱v1.0.1
  Test 13: 软件版本1.0.1已发布
  Result: 软件版本1.0.1╱已╱发布
  Test 14: 这是3.2-preview1版本
  Result: 这是╱3.2-preview1版本
  Test 15: 发布候选版本4.1.2-rc1
  Result: 发布╱候选版本4.1.2-rc1
  Test 16: 这是2.1-alpha1测试版
  Result: 这是╱2.1-alpha1测试版
  Test 17: 当前是6.3-beta2版本
  Result: 当前╱是╱6.3-beta2版本
  Test 18: 2026年1月13日19点03分14秒
  Result: 2026年1月13日19点03分14秒
  Test 19: 二零二六年一月十三日十九点零三分十四秒
  Result: 二零二六年一月十三日十九点零三分十四秒
  Test 20: 二零二六年一月十三日十九点二十分十四秒
  Result: 二零二六年一月十三日十九点二十分十四秒
  Test 21: 十九点二十分十四秒
  Result: 十九点二十分十四秒
  Test 22: 十九点二十分
  Result: 十九点二十分
  Test 23: 十九点
  Result: 十九点
  Test 24: 某人考试得了零分
  Result: 某人╱考试╱得╱了╱零分
  Test 25: 三分天下
  Result: 三分╱天下
  Test 26: 再等十九分二十秒，就要结束考试了
  Result: 再╱等╱十九分二十秒╱，╱就要╱结束╱考试╱了
  Test 27: 再等19分20秒，就要结束考试了
  Result: 再╱等╱19分20秒╱，╱就要╱结束╱考试╱了
  Test 28: 我是二零一零年出生的
  Result: 我╱是╱二零一零年╱出生╱的
  Test 29: 我是二〇一〇年出生的
  Result: 我╱是╱二〇一〇年╱出生╱的
  Test 30: 我是二零一零年五月出生的
  Result: 我╱是╱二零一零年五月╱出生╱的
  Test 31: 我是二〇一〇年五月出生的
  Result: 我╱是╱二〇一〇年五月╱出生╱的
  Test 32: 我是二零一零年五月一日出生的
  Result: 我╱是╱二零一零年五月一日╱出生╱的
  Test 33: 我是二〇一〇年五月一日出生的
  Result: 我╱是╱二〇一〇年五月一日╱出生╱的
  Pass ✓
[Test] Date-Time POS Tagging...
  Test 1: 今天4:50某某某领了一只记号笔
  Result: 今天4:50/t╱某某某/r╱领/v╱了/ul╱一只/m╱记号笔/n
  Test 2: 比值是100:31
  Result: 比值/n╱是/v╱100:31/n
  Test 3: 时间是14:30
  Result: 时间/n╱是/v╱14:30/t
  Pass ✓
[Test] lcut returns List<string> directly...
  Result: 我╱来到╱北京╱清华大学
  Pass ✓
[Test] lcut_for_search returns List<string> directly...
  Result: 小明╱硕士╱毕业╱于╱中国╱科学╱学院╱科学院╱中国科学院╱计算╱计算所
  Pass ✓
[Test] Tokenizer custom segmenter...
  Result: 我╱来到╱北京╱清华大学
  Pass ✓
[Test] Jieba.Dt default segmenter...
  Result: 我╱来到╱北京╱清华大学
  Pass ✓
[Test] Tokenizer independent dictionary...
  tokenizer1: 小明╱最近╱在╱学习╱机器学习
  tokenizer2: 小明╱最近╱在╱学习╱机器╱学习
  Pass ✓
[Test] Hyphen/Underscore connected word segmentation...
  Test 1: TF-IDF识别方法
  Result: TF-IDF╱识别方法
  Test 2: word1_word2_word3
  Result: word1_word2_word3
  Test 3: hello-world
  Result: hello-world
  Test 4: test_case_example
  Result: test_case_example
  Pass ✓
[Test] Domain/URL segmentation...
  Test 1: https://gitee.com/JTsamsde/AOTba
  Result: https://gitee.com/JTsamsde/AOTba
  Test 2: http://www.baidu.com/search?q=test
  Result: http://www.baidu.com/search?q=test
  Test 3: gitee.com
  Result: gitee.com
  Test 4: gitee.com/JTsamsde/AOTba
  Result: gitee.com/JTsamsde/AOTba
  Test 5: 访问https://github.com查看代码
  Result: 访问╱https://github.com╱查看╱代码
  Test 6: 访问gitee.com/JTsamsde/AOTba查看代码
  Result: 访问╱gitee.com/JTsamsde/AOTba╱查看╱代码
  Test 7: www.baidu.com
  Result: www.baidu.com
  Test 8: nuget.org
  Result: nuget.org
  Pass ✓
[Test] GB18030-2022 Ext B-I zone rare char segmentation...
  Test 1: 𩽾𩾌是深海中的一种鱼类
  Result: 𩽾𩾌╱是╱深海╱中╱的╱一种╱鱼类
  Test 2: 南海有轨电车一号线，起点为𧒽岗，终点为林岳东
  Result: 南海有轨电车一号线╱，╱起点╱为╱𧒽岗╱，╱终点╱为╱林岳东
  Test 3: 石𬒔是佛山市南海区桂城街道的一个地名
  Result: 石𬒔╱是╱佛山市╱南海区╱桂城街道╱的╱一个╱地名
  Test 4: 我今天吃了𰻝𰻝面，很好吃
  Result: 我╱今天╱吃╱了╱𰻝𰻝面╱，╱很╱好吃
  Test 5: 半径的日本新字体字形是半𮱻，繁体写作半徑
  Result: 半径╱的╱日本新字体╱字形╱是╱半𮱻╱，╱繁体╱写作╱半徑
  Test 6: 从𧒽岗出发，经过石𬒔，最后去吃𰻝𰻝面和𩽾𩾌料理
  Result: 从╱𧒽岗╱出发╱，╱经过╱石𬒔╱，╱最后╱去╱吃╱𰻝𰻝面╱和╱𩽾𩾌╱料理
  Test 7: 二〇一〇年
  Result: 二〇一〇年
  Pass ✓
[Test] EntityProtect.Disabled disable entity protection (OpenCC scenario)...
  Test 1: 2026年4月30日晚上9点开会
  Result: 2026╱年╱4╱月╱30╱日╱晚上╱9╱点╱开会
  Test 2: 软件版本1.0.1已发布
  Result: 软件╱版本╱1.0╱.╱1╱已╱发布
  Test 3: 访问https://github.com查看代码
  Result: 访问╱https╱:╱/╱/╱github╱.╱com╱查看╱代码
  Test 4: 我来到北京清华大学
  Result: 我╱来到╱北京╱清华大学
  Pass ✓
[Test] Windows version recognition segmentation...
  Test 1: 我使用的是Windows 10操作系统
  Result: 我╱使用╱的╱是╱Windows 10╱操作系统
  Test 2: 我使用的是Windows7操作系统
  Result: 我╱使用╱的╱是╱Windows7╱操作系统
  Test 3: 我使用的是Win 7操作系统
  Result: 我╱使用╱的╱是╱Win 7╱操作系统
  Test 4: 我使用的是Win7操作系统
  Result: 我╱使用╱的╱是╱Win7╱操作系统
  Test 5: 我使用的是Microsoft Windows 10操作系统
  Result: 我╱使用╱的╱是╱Microsoft Windows 10╱操作系统
  Test 6: 我使用的是Microsoft(R) Windows(R) 11操作系统
  Result: 我╱使用╱的╱是╱Microsoft(R) Windows(R) 11╱操作系统
  Test 7: 我使用的是Microsoft® Windows® 11操作系统
  Result: 我╱使用╱的╱是╱Microsoft® Windows® 11╱操作系统
  Test 8: 服务器运行Windows Server 2022
  Result: 服务器╱运行╱Windows Server 2022
  Test 9: 老电脑运行Windows XP系统
  Result: 老电脑╱运行╱Windows XP╱系统
  Test 10: Windows Vista是Windows 7的前身
  Result: Windows Vista╱是╱Windows 7╱的╱前身
  Test 11: 视窗95是早期的中文Windows版本
  Result: 视窗95╱是╱早期╱的╱中文╱Windows╱版本
  Test 12: 视窗 10是目前最新的Windows版本
  Result: 视窗 10╱是╱目前╱最新╱的╱Windows╱版本
  Test 13: Win32是Windows的32位API
  Result: Win32╱是╱Windows╱的╱32╱位╱API
  Entity: 
  Test 14: Win64是Windows的64位API
  Result: Win64╱是╱Windows╱的╱64╱位╱API
  Entity: 
  Test 15: 我使用的是windows 10操作系统
  Result: 我╱使用╱的╱是╱windows 10╱操作系统
  Test 16: Windows 12预计明年发布
  Result: Windows 12╱预计╱明年╱发布
  Test 17: Windows Server 2025是最新的服务器版本
  Result: Windows Server 2025╱是╱最新╱的╱服务器╱版本
  Test 18: Windows 8.1是Windows 8的升级版
  Result: Windows 8.1╱是╱Windows 8╱的╱升级版
  Test 19: Windows 2000是NT 5.0的商业名称
  Result: Windows 2000╱是╱NT╱ ╱5.0╱的╱商业╱名称
  Test 20: Windows NT 4.0发布于1996年
  Result: Windows NT 4.0╱发布╱于╱1996年
  Pass ✓
[Test] Word with space segmentation...
  Test 1: Kimi K2.5是一个大语言模型
  Result: Kimi K2.5╱是╱一个╱大语言╱模型
  Test 2: GPT 4o是OpenAI的多模态模型
  Result: GPT 4o╱是╱OpenAI╱的╱多模态╱模型
  Test 3: Claude 3.5 Sonnet是Anthropic的模型
  Result: Claude 3.5 Sonnet╱是╱Anthropic╱的╱模型
  Test 4: 对比Kimi K2.5和GPT 4o的性能
  Result: 对比╱Kimi K2.5╱和╱GPT 4o╱的╱性能
  Pass ✓
[Test] Dictionary word vs Time entity conflict...
  Test 1: 百年孤独
  Result: 百年孤独
  Test 2: 千年老二
  Result: 千年老二
  Test 3: 百年纪念
  Result: 百年纪念
  Test 4: 百年诞辰
  Result: 百年诞辰
  Test 5: 百年之后
  Result: 百年之后
  Test 6: 百年孤独与2021年1月1日的故事
  Result: 百年孤独╱与╱2021年1月1日╱的╱故事
  Batch verification of 34 dictionary words: All pass
  Pass ✓
[Test] Dictionary word vs Time entity filtering performance...
  5 texts × 1000 times = 5000 segmentations
  Total time: 463.787ms
  Average per time: 92.757μs (0.093ms)
  Performance standard met ✓ (within milliseconds)
  Pass ✓
[Test] AddWord adding ultra-long dictionary words (>30 chars)...
  Text: 这是北京召开的中国共产党第十九届中央委员会第四次全体会议发布公报全文的职责范围
  Segmentation: 这是╱北京召开的中国共产党第十九届中央委员会第四次全体会议发布公报全文╱的╱职责╱范围
  MaxScanLength extended to 32 (word length=32)
  Pass ✓
[Test] AddWord adding dictionary words containing time entities (12 o'clock password)...
  Text: 我的12点口令已更新，请查收
  Segmentation: 我╱的╱12点口令╱已╱更新╱，╱请╱查收
  Control: Independent '12点' correctly recognized ✓
  Pass ✓

=== All AOT tests passed! ===
```

### 2. Adding Custom Dictionaries

#### Loading Dictionaries

* Developers can specify a custom dictionary to include words not present in the jieba library. Although jieba has new word recognition capabilities, adding new words manually ensures higher accuracy.
* `JiebaSegmenter.LoadUserDict("user_dict_file_path")`
* The dictionary format is the same as the main dictionary: each line contains: word, word frequency (optional), part of speech (optional), separated by spaces.
* Frequency and POS can only be omitted for words without spaces.
* When frequency is omitted, the segmenter uses an automatically calculated frequency to ensure the word is segmented.
* Note: For words containing spaces (e.g., "Kimi K2.5"), frequency and POS cannot be omitted.

Example:

```
创新办 3 i
云计算 5
凱特琳 nz
台中
机器学习 3
Kimi K2.5 3000 nz
```

#### Adjusting Dictionaries

* Use `JiebaSegmenter.AddWord(word, freq=0, tag=null)` to add a new word or adjust the frequency of an existing word; if `freq` is not a positive integer, an automatically calculated frequency is used to ensure the word is segmented.
* Use `JiebaSegmenter.DeleteWord(word)` to remove a word so it can no longer be segmented.

### 3. Keyword Extraction

#### Keyword Extraction based on TF-IDF Algorithm

* `JiebaNet.Analyser.TfidfExtractor.ExtractTags(string text, int count = 20, IEnumerable<string> allowPos = null)` can extract keywords from a specified text.
* `JiebaNet.Analyser.TfidfExtractor.ExtractTagsWithWeight(string text, int count = 20, IEnumerable<string> allowPos = null)` extracts keywords while **simultaneously providing their weights**.
* Keyword extraction is based on Inverse Document Frequency (IDF). The component has a built-in IDF corpus, which can be configured to other custom corpora.
* Keyword extraction filters stop words. The component has a built-in stop word corpus that merges English stop words from NLTK and Chinese stop words from HIT (Harbin Institute of Technology).

#### Keyword Extraction based on TextRank Algorithm

* `JiebaNet.Analyser.TextRankExtractor` uses the same interface as `TfidfExtractor`. Note that `TextRankExtractor` only extracts nouns and verbs by default.
* It builds a graph based on the co-occurrence relationship between words using a fixed window size (default is 5, adjustable via the `Span` property).

### 4. POS Tagging

* The `JiebaNet.Segmenter.PosSeg.PosSegmenter` class can add part-of-speech (POS) tags to each word during segmentation.
* POS tagging uses a notation compatible with `ictclas`. For a list of notations used in `ictclas` and `jieba`, please refer to: [POS Tags](https://gist.github.com/luw2007/6016931).
* POS tagging supports extension zone characters (GB18030-2022) and Emojis. Extension zone characters are tagged as `nz` (other proper nouns), and Emojis are tagged as `x` (non-morpheme character).

```c#
var posSeg = new PosSegmenter();
var s = "从𧒽岗出发去吃𰻝𰻝面，今天😀很开心😊";

var tokens = posSeg.Cut(s);
Console.WriteLine(string.Join(" ", tokens.Select(token => string.Format("{0}/{1}", token.Word, token.Flag))));
```

```
从/p 𧒽岗/nz 出发/v 去/v 吃/v 𰻝𰻝面/nz ，/x 今天/t 😀/x 很/zg 开心/v 😊/x
```

### 5. Tokenize: Returning start and end positions of words in the original text

Positions are calculated based on Grapheme Clusters rather than `char` offsets, ensuring that positions for extension zone characters and Emojis match user perception.

* Default Mode: Maintains the integrity of ZWJ sequence Emojis and Variation Selector Emojis without splitting them.

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

* Search Mode: Extracts sub-words from long words to increase recall. ZWJ sequence Emojis will be split into sub-emojis (intentional design, consistent with the logic of splitting Chinese long words).

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

### 6. Parallel Segmentation

Use the following methods:

* `JiebaSegmenter.CutInParallel()`, `JiebaSegmenter.CutForSearchInParallel()`
* `PosSegmenter.CutInParallel()`

### 7. Integration with Lucene.NET

The `jiebaForLuceneNet` project provides simple integration with Lucene.NET. For more information, see: [jiebaForLuceneNet](https://github.com/anderscui/jiebaForLuceneNet/wiki/%E4%B8%8ELucene.NET%E7%9A%84%E9%9B%86%E6%88%90)

### 8. Other Dictionaries

jieba segmentation also provides other dictionary files:

* Smaller memory footprint dictionary: [https://raw.githubusercontent.com/anderscui/jieba.NET/master/ExtraDicts/dict.txt.small](https://raw.githubusercontent.com/anderscui/jieba.NET/master/ExtraDicts/dict.txt.small)
* Dictionary with better support for Traditional Chinese: [https://raw.githubusercontent.com/anderscui/jieba.NET/master/ExtraDicts/dict.txt.big](https://raw.githubusercontent.com/anderscui/jieba.NET/master/ExtraDicts/dict.txt.big)

### 9. Segmentation Speed

* All mode: 2.5 MB/s
* Accurate mode: 1.1 MB/s
* Test Environment: Intel(R) Core(TM) i3-2120 CPU @ 3.30GHz; 围城.txt (734KB)

### 10. Command Line Segmentation

Building the `Segmenter.Cli` project produces `jiebanet.ext`. Its options and usage examples are as follows:

```shell
-f       --file          the file name, (required).
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

### 11. Word Frequency Statistics

The `Counter` class can be used for word frequency statistics, implemented after the `Counter` class in the Python standard library (though interfaces and implementation details differ slightly).

`Counter<string>` supports two emoji processing modes, suitable for different types of word clouds:

- **Default Mode** (`countEmoji: false`, default): Filters emoji frequency and only counts text words; suitable for creating text-only word clouds.
- **Emoji Extraction Mode** (`countEmoji: true`): Retains emoji frequency; suitable for creating word clouds containing emojis.

Emoji filtering is based on `GraphemeClusterHelper.IsEmojiGrapheme()`, which correctly identifies complex emojis such as ZWJ sequences, variation selectors, and skin tone modifiers.

```c#
var seg = new JiebaSegmenter();
var s = "从𧒽岗出发去吃𰻝𰻝面，经过石𬒔，今天😀很开心😊笑死了🤣";

// Default mode: filter emojis, count text words only
var freqs = new Counter<string>(seg.Cut(s));
foreach (var pair in freqs.MostCommon(10))
{
    Console.WriteLine($"{pair.Key}: {pair.Value}");
}

// Emoji extraction mode: retain emoji frequency
var emojiFreqs = new Counter<string>(seg.Cut(s), countEmoji: true);
foreach (var pair in emojiFreqs.MostCommon(17))
{
    Console.WriteLine($"{pair.Key}: {pair.Value}");
}
```

Default mode output:

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

Emoji extraction mode output:

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

The `Counter` class can be modified using `Add`, `Subtract`, and `Union` methods, and the most frequent words can be obtained using the `MostCommon` method. Refer to test cases for detailed usage.

### 12. KeywordProcessor

`KeywordProcessor` can extract keywords from text, but its extraction differs from `KeywordExtractor`. `KeywordProcessor` can be understood as finding known words from a dictionary within the text—nothing more.

The current implementation of jieba segmentation cannot handle cases like ignoring case or words containing spaces, which are common in **text extraction** applications. Therefore, `KeywordProcessor` is primarily for extraction rather than segmentation, although another dictionary-based segmentation mode can be implemented through its methods.

Code Example:

```c#
var kp = new KeywordProcessor();
kp.AddKeywords(new []{"𰻝𰻝面", "𧒽岗", "石𬒔", ".NET Core", "C语言", "字典 tree", "CET-4", "网络 编程"});

var keywords = kp.ExtractKeywords("你需要通过cet-4考试，去𧒽岗吃𰻝𰻝面，学习c语言、.NET core、网络 编程、JavaScript，掌握字典 tree的用法，经过石𬒔");

// keywords value is:
// new List<string> { "CET-4", "𧒽岗", "𰻝𰻝面", "C语言", ".NET Core", "网络 编程", "字典 tree", "石𬒔"}

// As you can see, the words in the result are identical to the keywords added at the beginning, 
// not necessarily identical to the words in the input sentence. 
// If you need the original words found in the sentence, use the `raw` parameter.

var keywords = kp.ExtractKeywords("你需要通过cet-4考试，去𧒽岗吃𰻝𰻝面，学习c语言、.NET core、网络 编程、JavaScript，掌握字典 tree的用法，经过石𬒔", raw: true);

// keywords value is:
// new List<string> { "cet-4", "𧒽岗", "𰻝𰻝面", "c语言", ".NET core", "网络 编程", "字典 tree", "石𬒔"}
```

### 13. Entity Extraction

Dates, times, domain names, version numbers, and various other entities can be extracted.

Use the `ITimeRecognizer recognizer = new RegexTimeRecognizer();` method for entity extraction.

To prevent Gitee mirror misidentification of the README, specific entity recognition results are not shown here. Refer to `TimeRecognizerDemo` for specific code.

### Donation
[![pmBoq4e.md.jpg](https://s41.ax1x.com/2026/07/06/pmBoq4e.md.jpg)](https://imgchr.com/i/pmBoq4e)
[![pmBTSDP.md.png](https://s41.ax1x.com/2026/07/06/pmBTSDP.md.png)](https://imgchr.com/i/pmBTSDP)
