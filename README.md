<h1 align="center">
    <br>
    <img width="200" src="Sora/icon.png" alt="LOGO">
    <br>
    Sora
    <h4 align="center">
        一个基于<a href="https://github.com/howmanybots/onebot">OneBot</a>协议的 <a href="https://dotnet.microsoft.com/download/dotnet/6.0">C#/.Net 6</a> 异步机器人开发框架 | 
        <a href="https://sora-docs.yukari.one/">
            框架文档
        </a>
    </h4>
    <h4 align="center">
        <a href="https://www.nuget.org/packages/Sora/">
            <img src="https://img.shields.io/nuget/v/Sora?style=flat-square" alt="nuget">
        </a>
        <a href="https://onebot.dev/">
            <img src="https://img.shields.io/badge/OneBot-v11-black?style=flat-square&logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAHAAAABwCAMAAADxPgR5AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAAxQTFRF////29vbr6+vAAAAk1hCcwAAAAR0Uk5T////AEAqqfQAAAKcSURBVHja7NrbctswDATQXfD//zlpO7FlmwAWIOnOtNaTM5JwDMa8E+PNFz7g3waJ24fviyDPgfhz8fHP39cBcBL9KoJbQUxjA2iYqHL3FAnvzhL4GtVNUcoSZe6eSHizBcK5LL7dBr2AUZlev1ARRHCljzRALIEog6H3U6bCIyqIZdAT0eBuJYaGiJaHSjmkYIZd+qSGWAQnIaz2OArVnX6vrItQvbhZJtVGB5qX9wKqCMkb9W7aexfCO/rwQRBzsDIsYx4AOz0nhAtWu7bqkEQBO0Pr+Ftjt5fFCUEbm0Sbgdu8WSgJ5NgH2iu46R/o1UcBXJsFusWF/QUaz3RwJMEgngfaGGdSxJkE/Yg4lOBryBiMwvAhZrVMUUvwqU7F05b5WLaUIN4M4hRocQQRnEedgsn7TZB3UCpRrIJwQfqvGwsg18EnI2uSVNC8t+0QmMXogvbPg/xk+Mnw/6kW/rraUlvqgmFreAA09xW5t0AFlHrQZ3CsgvZm0FbHNKyBmheBKIF2cCA8A600aHPmFtRB1XvMsJAiza7LpPog0UJwccKdzw8rdf8MyN2ePYF896LC5hTzdZqxb6VNXInaupARLDNBWgI8spq4T0Qb5H4vWfPmHo8OyB1ito+AysNNz0oglj1U955sjUN9d41LnrX2D/u7eRwxyOaOpfyevCWbTgDEoilsOnu7zsKhjRCsnD/QzhdkYLBLXjiK4f3UWmcx2M7PO21CKVTH84638NTplt6JIQH0ZwCNuiWAfvuLhdrcOYPVO9eW3A67l7hZtgaY9GZo9AFc6cryjoeFBIWeU+npnk/nLE0OxCHL1eQsc1IciehjpJv5mqCsjeopaH6r15/MrxNnVhu7tmcslay2gO2Z1QfcfX0JMACG41/u0RrI9QAAAABJRU5ErkJggg==" alt="onebot">
        </a>
        <a href="https://www.apache.org/licenses/LICENSE-2.0">
            <img src="https://img.shields.io/github/license/Yukari316/Sora?style=flat-square&color=blueviolet" alt="license">
        </a>
        <img src="https://img.shields.io/github/stars/Yukari316/Sora?style=flat-square" alt="stars">
        <img src="https://img.shields.io/github/actions/workflow/status/Hoshikawa-Kaguya/Sora/nuget.yml?branch=master&&style=flat-square" alt="workflow">
        <a href="https://github.com/Mrs4s/go-cqhttp">
            <img src="https://img.shields.io/badge/go--cqhttp-v1.1.0-blue?style=flat-square" alt="gocq-ver">
        </a>
    </h4>
</h1>

## 重要信息

[go-cqhttp](https://github.com/Mrs4s/go-cqhttp) 因为qq官方的一系列协议升级，可能后续会停止开发

详细的原因：[go-cqhttp#2471](https://github.com/Mrs4s/go-cqhttp/issues/2471)

目前的情况是，`gocq`能用就不要做任何迁移，如果一定要迁移，哈哈，我也不知道迁移到哪，替代的已经无了

## 文档

**=====本框架只支持Array的上报格式!=====**

本页面不会对框架的特性做介绍，如果需要详细了解框架的功能**一定**要看**文档**！

->[Docs](https://sora-docs.yukari.one/)<-

->[更新日志](https://sora-docs.yukari.one/updatelog/)<- `更新日志中会标注框架所对应的go-cqhttp版本号`

文档目前只有简单的向导和自动生成API文档

详细的介绍文档还在编写

如需要查看最新自动生成的文档请前往 [![Sora on fuget.org](https://www.fuget.org/packages/Sora/badge.svg)](https://www.fuget.org/packages/Sora)

<details>
  <summary>支持的连接方式</summary>

- 正向Websocket
- 反向Websocket

</details>

## 关于本框架

> 本框架从开始到今后都只会支持onebot协议，非onebot的平台并不会考虑进行支持

这是一个以轻量为主的 [onebot](https://onebot.dev/) 机器人开发框架，主要的支持方向为 [go-cqhttp](https://github.com/Mrs4s/go-cqhttp)

这个框架将会一直以简单易用为主，也会向着更加便捷的方向进行开发

所以不会有什么特别复杂的功能

同时也不会将框架拆分为多个不同功能的包 ~~毕竟本来就没有什么功能~~

如果希望拥有  `指令路由`  `多IM平台支持`  等等功能，推荐使用 [OneBot-Framework](https://github.com/ParaParty/OneBot-Framework)

这个项目同时也是我学习C#这个语言的过程中的产物，所以里面可能会部分拉高血压的代码 ~~屎山~~

如果有什么建议的话，可以在[Discussions](https://github.com/Yukari316/Sora/discussions)里提出哦

## 开发注意事项

<details>
<summary>开源协议</summary>

本项目使用了 `Apache-2.0`开源协议

这意味着在引用/修改本类库时需要遵守相关的协议规定

</details>

<details>
<summary>代码复查</summary>

### 我复查了某一段代码

如果有代码复查，请在函数上面贴上Reviewed以代表是谁以及什么时候进行了代码复查

例如：

```csharp
[Reviewed("XiaoHe321", "2021-03-11 00:45")]
internal async ValueTask METHOD_NAME()
```

若修改了这段代码，请将Reviewed注解及时删除，以方便代码复查人员知道，你改了这段代码，方便进行复查。

对于自己代码的复查，请不要贴上Reviewed。

### 我修改了一段代码需要复查

如果有代码需要复查，请在函数上面贴上NeedReview以代表这段代码需要复查

例如：

> 行号也可以是 `ALL`一代表这整个方法都需要检查

```csharp
[NeedReview("L12-L123")]
internal async ValueTask METHOD_NAME()
```

</details>

<details>
<summary>QQ频道</summary>

本框架的正式版本和正常发布版本目前将不会支持Guild相关的API

具有Guild API的测试版本已经在 `extra/guild`分支中编写，如果需要这部分的API请自行clone引用或打包

**警告：请勿将Guild分支测试版本在生产环境中使用，其中很多API都是实验性或不稳定的！**

`extra/guild` 分支不会和主分支同步，这是一个完全实验性质的分支

把这两套IM的协议做在一个框架里会导致API非常混乱（我都不知道QQ项目组的人怎么想的）

并且鉴于频道也没什么人用，而且通话质量依旧是中东战场音质，本框架将会在V12协议适配频道协议后再考虑适配问题

</details>

## 关于ISSUE

ISSUE 目前只接受bug的提交和新功能的建议

如果有使用问题或者不确定的问题请使用[Discussions](https://github.com/Yukari316/Sora/discussions)

> 请注意, 开发者并没有**义务**回复您的问题. 您应该具备基本的提问技巧。
>
> 如果不知道该怎么样提问，那么请在提问前阅读 [提问的智慧](https://github.com/ryanhanwu/How-To-Ask-Questions-The-Smart-Way/blob/master/README-zh_CN.md)

以下ISSUE会被直接关闭

- 提交BUG时没有使用Template
- 提交当前版本下已经被修复的BUG
- 询问问题（为什么不用用[Discussions](https://github.com/Yukari316/Sora/discussions)呢）

## 关于命名

Sora这个名字来源于日语中"空"的罗马音

一拍脑袋想的.jpg

## 鸣谢

感谢以下大佬对本框架开发的帮助

[Mrs4s](https://github.com/Mrs4s) | [wdvxdr1123](https://github.com/wdvxdr1123)  在使用[go-cqhttp](https://github.com/Mrs4s/go-cqhttp)调试时提供了帮助

[Kengxxiao](https://github.com/Kengxxiao) | [ExerciseBook](https://github.com/ExerciseBook)  对框架做出了改进

### 使用到的开源库

[Fleck](https://github.com/statianzo/Fleck) | 反向WS服务器

[Websocket.Client](https://github.com/Marfusios/websocket-client) | 正向WS客户端

[Newtonsoft.Json](https://www.newtonsoft.com/json) | Json序列化/反序列化

[protobuf-net](https://github.com/protobuf-net/protobuf-net) | ProtoBuf序列化/反序列化

[System.Reactive](https://github.com/dotnet/reactive) | 响应式异步API支持

[YukariToolBox](https://github.com/DeepOceanSoft/YukariToolBox) | Log，异步扩展工具箱

### 感谢 [JetBrains](https://www.jetbrains.com/?from=Sora) 为开源项目提供免费的全家桶授权

> 本项目使用了免费的[ReSharper](https://www.jetbrains.com/resharper/)插件/[Rider](https://www.jetbrains.com/rider/?from=Sora)开发环境

<a href="https://www.jetbrains.com/?from=Sora">
    <img src=".github/jetbrains-variant-4.svg" alt="jetbrains">
</a>
<a href="https://www.jetbrains.com/ReSharper/?from=Sora">
    <img src=".github/icon-resharper.svg" alt="jetbrains">
</a>
<a href="https://www.jetbrains.com/rider/?from=Sora">
    <img src=".github/icon-rider.svg" alt="jetbrains">
</a>
