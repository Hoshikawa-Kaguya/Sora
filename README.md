<h1 align="center">
	<br>
	<img width="200" src="Sora/icon.png" alt="LOGO">
	<br>
	Sora
	<h4 align="center">
        一个基于<a href="https://github.com/howmanybots/onebot">OneBot</a>协议的 <a href="https://dotnet.microsoft.com/download/dotnet/6.0">C#/.Net 6</a> 异步机器人开发框架
	</h4>
	<h4 align="center">
	<a href="https://www.nuget.org/packages/Sora/">
		<img src="https://img.shields.io/nuget/vpre/Sora?style=flat-square&color=ff69b4" alt="nuget">
	</a>
	<a href="https://github.com/howmanybots/onebot">
		<img src="https://img.shields.io/badge/OneBot-v11-black?style=flat-square&logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAHAAAABwCAMAAADxPgR5AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAAxQTFRF////29vbr6+vAAAAk1hCcwAAAAR0Uk5T////AEAqqfQAAAKcSURBVHja7NrbctswDATQXfD//zlpO7FlmwAWIOnOtNaTM5JwDMa8E+PNFz7g3waJ24fviyDPgfhz8fHP39cBcBL9KoJbQUxjA2iYqHL3FAnvzhL4GtVNUcoSZe6eSHizBcK5LL7dBr2AUZlev1ARRHCljzRALIEog6H3U6bCIyqIZdAT0eBuJYaGiJaHSjmkYIZd+qSGWAQnIaz2OArVnX6vrItQvbhZJtVGB5qX9wKqCMkb9W7aexfCO/rwQRBzsDIsYx4AOz0nhAtWu7bqkEQBO0Pr+Ftjt5fFCUEbm0Sbgdu8WSgJ5NgH2iu46R/o1UcBXJsFusWF/QUaz3RwJMEgngfaGGdSxJkE/Yg4lOBryBiMwvAhZrVMUUvwqU7F05b5WLaUIN4M4hRocQQRnEedgsn7TZB3UCpRrIJwQfqvGwsg18EnI2uSVNC8t+0QmMXogvbPg/xk+Mnw/6kW/rraUlvqgmFreAA09xW5t0AFlHrQZ3CsgvZm0FbHNKyBmheBKIF2cCA8A600aHPmFtRB1XvMsJAiza7LpPog0UJwccKdzw8rdf8MyN2ePYF896LC5hTzdZqxb6VNXInaupARLDNBWgI8spq4T0Qb5H4vWfPmHo8OyB1ito+AysNNz0oglj1U955sjUN9d41LnrX2D/u7eRwxyOaOpfyevCWbTgDEoilsOnu7zsKhjRCsnD/QzhdkYLBLXjiK4f3UWmcx2M7PO21CKVTH84638NTplt6JIQH0ZwCNuiWAfvuLhdrcOYPVO9eW3A67l7hZtgaY9GZo9AFc6cryjoeFBIWeU+npnk/nLE0OxCHL1eQsc1IciehjpJv5mqCsjeopaH6r15/MrxNnVhu7tmcslay2gO2Z1QfcfX0JMACG41/u0RrI9QAAAABJRU5ErkJggg==" alt="onebot">
	</a>
	<a href="https://www.apache.org/licenses/LICENSE-2.0">
		<img src="https://img.shields.io/github/license/Yukari316/Sora?style=flat-square&color=blueviolet" alt="license">
	</a>
	<img src="https://img.shields.io/github/stars/Yukari316/Sora?style=flat-square" alt="stars">
	<img src="https://img.shields.io/github/workflow/status/Yukari316/Sora/.NET%20Core/master?style=flat-square" alt="workflow">
	<a href="https://github.com/Mrs4s/go-cqhttp">
		<img src="https://img.shields.io/badge/go--cqhttp-v1.0.0--beta7--fix2-blue?style=flat-square" alt="gocq-ver">
	</a>
	</h4>
</h1>

## 文档

> 对.Net5的支持在1.0.0-rc26之后的版本将会停止，1.0将会发布单独的.Net5版本
> 
> 之后的开发将会转为.Net6，由于.Net6为LTS的.Net版本，之后将会在.Net6的生命周期内使用.Net6进行开发

> 本框架只支持Array的上报格式

> 目前框架的协议版本是v11

查看框架的说明文档 [Docs](https://sora-docs.yukari.one/) [更新日志](https://sora-docs.yukari.one/updatelog/)

文档目前只有简单的向导和自动生成API文档

详细的介绍文档还在编写

如需要查看最新自动生成的文档请前往 [![Sora on fuget.org](https://www.fuget.org/packages/Sora/badge.svg)](https://www.fuget.org/packages/Sora)

<details>
  <summary>支持的连接方式</summary>

- 正向Websocket
- 反向Websocket


</details>

## 开发注意事项

### **目前框架并没有发布LTS版本**

目前会和go-cqhttp同步进行更新和调整

并在go-cqhttp正式更新LTS版本时更新第一个LTS版本

框架在LTS版本前可能会因为各种调整而做出**毁灭性**调整

详细内容请关注 [更新日志](https://sora-docs.yukari.one/updatelog/)

更新日志中会标注框架所对应的go-cqhttp版本号，并且框架对所对应的go-cqhttp均具有完整的API支持/扩展

<details>
<summary>开源协议</summary>

本项目使用了`Apache-2.0`开源协议

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

> 行号也可以是`ALL`一代表这整个方法都需要检查

```csharp
[NeedReview("L12-L123")]
internal async ValueTask METHOD_NAME()
```

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

## 关于本框架

Sora这个名字来源于日语中"空"的罗马音

当初只是想到了空灵这个词就想到了这么个字 ~~假装自己会起名~~

这个项目同时也是我学习C#这个语言的过程中的产物，所以里面可能会部分拉高血压的代码 ~~屎山~~

如果有什么建议的话，可以在[Discussions](https://github.com/Yukari316/Sora/discussions)里提出哦

## 鸣谢

感谢以下大佬对本框架开发的帮助

[Mrs4s](https://github.com/Mrs4s) | [wdvxdr1123](https://github.com/wdvxdr1123)  在使用[go-cqhttp](https://github.com/Mrs4s/go-cqhttp)调试时提供了帮助

[Kengxxiao](https://github.com/Kengxxiao) | [ExerciseBook](https://github.com/ExerciseBook)  对框架做出了改进

### 使用到的开源库

[Fleck](https://github.com/statianzo/Fleck) | 反向WS服务器

[Websocket.Client](https://github.com/Marfusios/websocket-client) | 正向WS客户端

[Newtonsoft.Json](https://www.newtonsoft.com/json) | Json序列化/反序列化

[System.Reactive](https://github.com/dotnet/reactive) | 响应式异步API支持

[YukariToolBox](https://github.com/DeepOceanSoft/YukariToolBox) | Log，异步扩展工具箱

### 感谢 [JetBrains](https://www.jetbrains.com/?from=Sora) 为开源项目提供免费的全家桶授权

> 本项目使用了免费的[ReSharper](https://www.jetbrains.com/resharper/)插件/[Rider](https://www.jetbrains.com/rider/?from=Sora)开发环境

[<img src=".github/jetbrains-variant-4.svg" width="200"/>](https://www.jetbrains.com/?from=Sora) [<img src=".github/icon-resharper.svg" width="100"/>](https://www.jetbrains.com/ReSharper/?from=Sora)[<img src=".github/icon-rider.svg" width="100"/>](https://www.jetbrains.com/rider/?from=Sora)
