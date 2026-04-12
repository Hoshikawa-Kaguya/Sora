# Sora

一个基于 C#/.NET 10 的多协议异步机器人开发框架

[![NuGet](https://img.shields.io/nuget/v/HoshikawaKaguya.Sora?style=flat-square)](https://www.nuget.org/packages/HoshikawaKaguya.Sora/)
[![License](https://img.shields.io/badge/license-Apache--2.0-blueviolet?style=flat-square)](https://github.com/Hoshikawa-Kaguya/Sora/blob/master/LICENSE)

## 特性

- 多协议支持：[Milky](https://milky.ntqqrev.org/)（主要）/ OneBot v11（兼容）
- 模块化架构，按需引用
- 属性指令路由（`[CommandGroup]` + `[Command]`）
- 完整的事件分发与消息等待机制
- 简单直接的 API 设计

## 安装

```shell
dotnet add package HoshikawaKaguya.Sora
dotnet add package HoshikawaKaguya.Sora.Adapter.Milky    # Milky 协议
dotnet add package HoshikawaKaguya.Sora.Adapter.OneBot11 # 或 OneBot v11 协议
```

## 快速开始

```csharp
using Sora;
using Sora.Adapter.Milky;

SoraService service = SoraServiceFactory.Instance.CreateMilkyService(
    new MilkyConfig
        {
            Host        = "localhost",
            Port        = 3010,
            AccessToken = "your-token"
        });

service.Events.OnMessageReceived += async e =>
{
    if (e.Message.Body.GetText() == "ping")
        await e.Api.SendGroupMessageAsync(e.Message.GroupId, new MessageBody("pong"));
};

await service.StartAsync();
await Task.Delay(-1);
```

## 项目模块

| 包                                         | 说明                   |
|-------------------------------------------|----------------------|
| `HoshikawaKaguya.Sora`                    | 框架应用层                |
| `HoshikawaKaguya.Sora.Entities`           | 共享实体 — 事件、消息段、API 接口 |
| `HoshikawaKaguya.Sora.Core`               | 核心工具库                |
| `HoshikawaKaguya.Sora.Command`            | 属性指令路由               |
| `HoshikawaKaguya.Sora.Adapter.Milky`      | Milky 协议适配器          |
| `HoshikawaKaguya.Sora.Adapter.OneBot11`   | OneBot v11 协议适配器     |

## 文档与链接

- [GitHub 仓库](https://github.com/Hoshikawa-Kaguya/Sora)
- [开源协议 (Apache-2.0)](https://github.com/Hoshikawa-Kaguya/Sora/blob/master/LICENSE)
