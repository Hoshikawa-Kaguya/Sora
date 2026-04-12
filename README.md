<h1 align="center">
    <br>
    <img width="200" src="docs/icon.png" alt="LOGO">
    <br>
    Sora
    <h4 align="center">
        一个基于 <a href="https://dotnet.microsoft.com/download/dotnet/10.0">C#/.NET 10</a> 的多协议异步机器人开发框架 | 
        <a href="">
            框架文档（还没写）
        </a>
    </h4>
    <h4 align="center">
        <a href="https://www.nuget.org/packages/Sora/">
            <img src="https://img.shields.io/nuget/v/Sora?style=flat-square" alt="nuget">
        </a>
        <a href=""><!-- TODO: License 链接 -->
            <img src="https://img.shields.io/badge/license-Apache--2.0-blueviolet?style=flat-square" alt="license">
        </a>
        <img src="https://img.shields.io/github/stars/Yukari316/Sora?style=flat-square" alt="stars"><!-- TODO: GitHub stars badge -->
        <img src="https://img.shields.io/github/actions/workflow/status/Hoshikawa-Kaguya/Sora/ci.yml?branch=master&&style=flat-square" alt="workflow"><!-- TODO: CI workflow badge -->
    </h4>
</h1>

## 关于本框架

~~孩子们我睡醒了~~

> 新版本文档还没写，目前的文档大部分都是AI帮我写的，将就看看吧，真的懒得写文档了

> 新版框架不再会有CQ码支持

Sora 是一个以**轻量**和**易用**为核心目标的多协议异步机器人开发框架

与旧版 Sora 不同，新版从零重构，原生支持多种协议适配：

- [**Milky**](https://milky.ntqqrev.org/)（主要协议）
- **OneBot v11**（兼容协议）— 正向/反向 WebSocket

框架采用模块化架构，提供属性指令路由、事件分发、消息等待等功能，同时保持简单直接的 API 设计

## 项目结构

| 模块 | 说明 |
|------|------|
| `Sora` | 框架门面层 — SoraService、SoraServiceFactory、日志初始化 |
| `Sora.Entities` | 共享实体 — 事件、消息段、信息类型、API 接口 |
| `Sora.Core` | 核心工具 — 枚举、扩展方法、通用基础设施 |
| `Sora.Command` | 属性指令路由 — `[CommandGroup]` + `[Command]` 声明式指令 |
| `Sora.Adapter.Milky` | Milky 协议适配器 |
| `Sora.Adapter.OneBot11` | OneBot v11 协议适配器 |

## Protocol Adapter

> Milky 和 OneBot v11 均通过了较为较为完整的E2E测试
>
> 部分破坏性API/Event由于风控未作测试

### Milky

基于 [Milky](https://milky.ntqqrev.org/) HTTP API 的适配器，推荐优先使用。

[Milky](https://milky.ntqqrev.org/)协议100%支持，并且这个框架目前是基于[Milky](https://milky.ntqqrev.org/)在调试和开发

| 特性 | 说明 |
|------|------|
| 事件传输 | SSE / WebSocket / WebHook 三选一（默认 WebSocket） |
| API 调用 | HTTP REST |
| TLS | 支持（含客户端证书） |
| 自动重连 | 支持（SSE / WebSocket） |

```csharp
SoraService service = SoraServiceFactory.Instance.CreateMilkyService(
    new MilkyConfig { Host = "127.0.0.1", Port = 3000, AccessToken = "your-token" });
```

### OneBot v11

> 由于OneBot v11常年无人维护且各家协议端实现都不一样，使用OneBot v11可能会遇到很多不兼容或者意想不到的情况
>
> 这个adapter目前只对LLBot做了测试，不再推荐使用OneBot v11协议

基于 [OneBot v11](https://11.onebot.dev/) 的适配器，支持正向/反向 WebSocket。**只支持Array格式上报**

Note: 由于OneBot v11目前各协议端实现比较自由，所以有部分扩展API/Event并不支持（覆盖率在85%左右）

| 特性 | 说明 |
|------|------|
| 连接模式 | 正向 WS（Sora → OB11 Server）/ 反向 WS（OB11 Server → Sora） |
| TLS | 支持 |
| 心跳检测 | 支持 |
| 自动重连 | 支持（正向 WS） |

```csharp
SoraService service = SoraServiceFactory.Instance.CreateOneBot11Service(
    new OneBot11Config { Host = "127.0.0.1", Port = 6700, AccessToken = "your-token" });
```

> 需要开发自定义适配器？请参阅 [Adapter 开发指南](docs/ADAPTER-DEVELOPMENT.md)

## 快速开始

### 安装

<!-- TODO: 发布到 NuGet 后补充安装命令 -->
```shell
dotnet add package Sora
dotnet add package Sora.Adapter.Milky    # Milky 协议
dotnet add package Sora.Adapter.OneBot11 # 或 OneBot v11 协议
```

### 最小示例

```csharp
using Sora;
using Sora.Adapter.Milky;

// 创建 Milky 协议服务
SoraService service = SoraServiceFactory.Instance.CreateMilkyService(
    new MilkyConfig
        {
            Host        = "localhost",
            Port        = 3010,
            AccessToken = "your-token"
        });

// 注册消息事件
service.Events.OnMessageReceived += async e =>
{
    string text = e.Message.Body.GetText();
    if (text == "ping")
    {
        await e.Api.SendGroupMessageAsync(
            e.Message.GroupId,
            new MessageBody("pong"));
    }
};

// 启动服务
await service.StartAsync();
await Task.Delay(-1);
```

### 属性指令

```csharp
[CommandGroup(Name = "example", Prefix = "/")]
public static class MyCommands
{
    [Command(Expressions = ["hello"], MatchType = MatchType.Full, Description = "Say hello")]
    public static async ValueTask Hello(MessageReceivedEvent e)
    {
        MessageBody reply = new("Hello! 你好！");
        if (e.Message.SourceType == MessageSourceType.Group)
            await e.Api.SendGroupMessageAsync(e.Message.GroupId, reply);
        else
            await e.Api.SendFriendMessageAsync(e.Message.SenderId, reply);
    }
}

// 注册指令
service.Commands.ScanAssembly(typeof(Program).Assembly);
```

## 可以简单参考的文档

> 这些文档均由opus 4.6生成和~~少量人工修改~~，可能存在不准确的问题
>
> ~~懒得自己写文档了，好费劲~~

| 文档 | 说明 |
|------|------|
| [测试说明](docs/TESTING.md) | 双机器人测试架构、环境变量、Run-Tests.ps1 用法 |
| [本地测试配置](docs/TESTING-LOCAL.md) | 本地环境快速配置参考 |
| [日志配置](docs/LOGGING.md) | Serilog 默认日志、自定义 LoggerFactory、日志级别 |
| [Adapter 开发指南](docs/ADAPTER-DEVELOPMENT.md) | 第三方协议适配器开发详细指南 |
| [功能测试目录](docs/FUNCTIONAL-TEST-CATALOG.md) | 所有 E2E 测试的完整清单 |
| [已移除测试](docs/REMOVED_TESTS.md) | 无法自动化的测试及移除原因 |
| [从 1.x 迁移到 2.0](docs/MIGRATION-V1-TO-V2.md) | 1.x → 2.0 完整迁移指南 |

## 开发注意事项

<details>
<summary>开源协议</summary>

本项目使用了 `Apache-2.0` 开源协议

这意味着在引用/修改本类库时需要遵守相关的协议规定

</details>

### 构建要求

- .NET 10 SDK
- C# 预览版语言特性（LangVersion = preview）

```shell
dotnet build Sora.slnx --configuration Release
```

### 测试

> 我vibe了90%的测试工程，但都经过了实际的验证测试，但可能还有我没发现的问题
>
> ~~好懒不想写这么多测试，好像1.x版本就压根没写~~

项目包含单元测试和功能测试（E2E），使用 xUnit v3 框架：

```shell
# 运行单元测试（无需外部依赖）
dotnet test Sora.slnx --filter "Category=Unit" --no-build

# 运行所有测试（需要配置测试环境变量）
pwsh tests/scripts/Run-Tests.ps1 -Category All
```

功能测试采用双机器人完成E2E验证，需要配置 `SORA_TEST_*` 环境变量。部分破坏性API/Event并未测试（容易触发风控）

详见 [测试说明](docs/TESTING.md)。

## 关于 ISSUE

ISSUE 目前只接受 bug 的提交和新功能的建议

如果有使用问题或者不确定的问题请使用[Discussions](https://github.com/Yukari316/Sora/discussions)

> 请注意，开发者并没有**义务**回复您的问题。您应该具备基本的提问技巧。
>
> 如果不知道该怎么样提问，那么请在提问前阅读 [提问的智慧](https://github.com/ryanhanwu/How-To-Ask-Questions-The-Smart-Way/blob/master/README-zh_CN.md)

以下 ISSUE 会被直接关闭

- 提交 BUG 时没有使用 Template
- 提交当前版本下已经被修复的 BUG
- 询问问题

## 关于命名

Sora 这个名字来源于日语中"空"的罗马音

一拍脑袋想的.jpg

## 鸣谢

### 使用到的开源库

[Newtonsoft.Json](https://www.newtonsoft.com/json) | Json 序列化/反序列化

[Mapster](https://github.com/MapsterMapper/Mapster) | 对象映射

[Serilog](https://github.com/serilog/serilog) | 结构化日志

> 以下的依赖只被Onebot 11 adapter所使用

[Fleck](https://github.com/statianzo/Fleck) | 反向 WS 服务器

[Websocket.Client](https://github.com/Marfusios/websocket-client) | 正向 WS 客户端

[System.Reactive](https://github.com/dotnet/reactive) | 响应式异步 API 支持

### 感谢 [JetBrains](https://www.jetbrains.com/?from=Sora) 为开源项目提供免费的全家桶授权

> 本项目使用了 [Rider](https://www.jetbrains.com/rider/?from=Sora) 开发环境

<a href="https://www.jetbrains.com/?from=Sora">
    <img src=".github/jetbrains-variant-4.svg" width="170" alt="jetbrains">
</a>
<a href="https://www.jetbrains.com/rider/?from=Sora">
    <img src=".github/icon-rider.svg" alt="jetbrains">
</a>