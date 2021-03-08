<h1 align="center">
	<br>
	<img width="200" src="Sora/icon.png" alt="LOGO">
	<br>
	Sora
	<h4 align="center">
        一个基于<a href="https://github.com/howmanybots/onebot">OneBot</a>协议的 <a href="https://dotnet.microsoft.com/download/dotnet/5.0">C#/.Net 5</a> 异步机器人开发框架
	</h4>
	<h4 align="center">
	<a href="https://www.nuget.org/packages/Sora/">
		<img src="https://img.shields.io/nuget/v/Sora?style=for-the-badge&color=ff69b4">
	</a>
	<a href="https://github.com/howmanybots/onebot">
		<img src="https://img.shields.io/badge/OneBot-v11-black?style=for-the-badge">
	</a>
	<a href="https://opensource.org/licenses/AGPL-3.0">
		<img src="https://img.shields.io/github/license/Yukari316/Sora?style=for-the-badge&color=blueviolet">
	</a>
	<img src="https://img.shields.io/github/stars/Yukari316/Sora?style=for-the-badge">
	<img src="https://img.shields.io/github/workflow/status/Yukari316/Sora/.NET%20Core/master?style=for-the-badge">
	</h4>
</h1>

----

## 文档

查看框架的说明文档 [Docs](https://sora-docs.yukari.one/)

文档目前只有简单的向导和自动生成API文档

详细的介绍文档还在编写

如需要查看最新自动生成的文档请前往 [![Sora on fuget.org](https://www.fuget.org/packages/Sora/badge.svg)](https://www.fuget.org/packages/Sora)

## 开发注意事项

### **目前框架并没有发布LTS版本**

由于框架还在快速迭代中

框架可能因为各种调整而做出**毁灭性**调整

并且每次做出以下改动时会修改框架的子版本号，请在更新时注意：

 - 删除/移动/重命名API
 - 删除/移动/重命名命名空间

 

详细内容请关注文档的更新

> 本框架是通过和[Go-Cqhttp](https://github.com/Mrs4s/go-cqhttp)(版本:[0.9.40-fix3](https://github.com/Mrs4s/go-cqhttp/releases/tag/v0.9.40-fix3))通讯进行调试的，如使用其他平台可能会出现兼容性的问题

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

[Mrs4s](https://github.com/Mrs4s) | [wdvxdr1123](https://github.com/wdvxdr1123) | [Kengxxiao](https://github.com/Kengxxiao)

### 源代码参考

[Jie2GG](https://github.com/Jie2GG)/[Native.Framework](https://github.com/Jie2GG/Native.Framework)

### 使用到的开源库

[Fleck](https://github.com/statianzo/Fleck) | [Newtonsoft.Json](https://www.newtonsoft.com/json) | [System.Reactive](https://github.com/dotnet/reactive) | [YukariToolBox](https://github.com/DeepOceanSoft/YukariToolBox)

### 感谢 [JetBrains](https://www.jetbrains.com/?from=mirai) 为开源项目提供免费的全家桶授权

> 本项目使用了免费的[ReSharper](https://www.jetbrains.com/resharper/)插件

[<img src=".github/jetbrains-variant-3.png" width="200"/>](https://www.jetbrains.com/)