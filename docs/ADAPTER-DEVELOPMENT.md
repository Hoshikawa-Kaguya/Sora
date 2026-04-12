# Adapter 开发指南

本文档面向希望为 Sora 框架开发第三方协议适配器的开发者。

> `__Name__`（双下划线包围）为占位符，请替换为实际名称。

## 概述

Sora 通过 `IBotAdapter` 接口抽象协议实现，任何协议（QQ、Telegram、Discord 等）都可以通过实现该接口接入框架。适配器项目需要引用 `Sora` 应用层项目，并通过扩展方法提供创建入口。

## Sora 项目结构

```
Sora.Core              ID 包装类型（UserId/GroupId/MessageId）、枚举、结果类型
  ↑
Sora.Entities          Segment、事件、Info 模型、IBotApi、IBotAdapter、EventDispatcher、MessageWaiter
  ↑
Sora.Command           [Command]/[CommandGroup] 特性、匹配器（Full/Regex/Keyword）、CommandManager
  ↑
Sora (facade)          SoraServiceFactory、SoraService — 组装入口，不引用任何适配器
  ↑
Sora.Adapter.*         协议适配器，引用 Sora 应用层项目
```

依赖方向自上而下。适配器位于最上层，引用 `Sora` 应用层项目即可获得全部框架能力。

各项目的关键职责：

| 项目 | 职责 |
|------|------|
| `Sora.Core` | 值类型（`UserId`、`GroupId`、`MessageId`）、枚举（`MatchType`、`SegmentType`、`MessageSourceType` 等）、`ApiResult` 结果类型 |
| `Sora.Entities` | 消息段（`Segment`、`MessageBody`）、事件（`BotEvent`、`MessageReceivedEvent` 等）、数据模型（`UserInfo`、`GroupInfo` 等）、核心接口（`IBotApi`、`IBotAdapter`、`IBotService`）、`EventDispatcher`、`MessageWaiter` |
| `Sora.Command` | `[Command]`/`[CommandGroup]` 特性扫描、`ICommandMatcher` 匹配策略、`CommandManager` 命令路由 |
| `Sora` | `SoraServiceFactory` 工厂、`SoraService` 事件管线（Waiter → Dispatcher → Commands） |
| `Sora.Adapter.*` | 协议实现：网络连接、事件/消息转换、`IBotApi` 实现、扩展接口（`IMilkyExtApi` 等） |

### 事件管线

```
协议网络层 → Converter → BotEvent → SoraService
  → MessageWaiter.TryMatch（连续对话，最高优先级）
  → EventDispatcher（按类型分发：OnMessageReceived、OnMemberJoined 等）
  → CommandManager.HandleMessageEventAsync（命令匹配，如启用）
```

适配器只需将协议数据转换为 `BotEvent` 并通过 `IAdapterEventSource.OnEvent` 触发，后续管线由 `SoraService` 自动处理。

## 适配器项目结构

```
Sora.Adapter.__YourProtocol__/
├── __YourProtocol__Adapter.cs           # IBotAdapter 实现
├── __YourProtocol__BotApi.cs            # IBotApi 实现
├── __YourProtocol__Config.cs            # 配置类（实现 IBotServiceConfig）
├── __YourProtocol__ServiceExtensions.cs # SoraServiceFactory 扩展方法
├── Converter/
│   ├── EventConverter.cs                # 协议事件 → BotEvent 转换
│   └── MessageConverter.cs              # 协议消息 → Segment 转换
├── Net/
│   └── ...                              # 网络层实现（WebSocket/HTTP 等）
└── Models/
    └── ...                              # 协议数据模型
```

适配器程序集请遵循 `Sora.Adapter.__ProtocolName__` 的命名规范。

## 注册 InternalsVisibleTo

适配器需要访问 `Sora.Entities` 中的 `internal` 成员（如 `BotConnection.SelfId` setter、`MessageBody.AddIncoming()` 等）。由于 C# 的 `InternalsVisibleTo` 不支持通配符，第三方适配器需要通过 **Pull Request** 将自己的程序集名称注册到框架中。

1. Fork 本仓库，在 `src/Sora.Entities/Sora.Entities.csproj` 中找到 `<!-- Adapters -->` 区域，添加程序集名称：

```xml
<!-- Adapters -->
<ItemGroup>
    <InternalsVisibleTo Include="Sora.Adapter.OneBot11"/>
    <InternalsVisibleTo Include="Sora.Adapter.Milky"/>
    <InternalsVisibleTo Include="Sora.Adapter.__YourProtocol__"/>
</ItemGroup>
```

2. 提交 PR，标题格式：`feat: register adapter Sora.Adapter.__YourProtocol__`，并在描述中附上适配器仓库链接。

如果目标平台需要框架中尚未定义的事件类型、Segment 类型、Info 模型等，可以在同一个 PR 中一并提交。

## 适配器可用的 Internal API

以下 `internal` 成员通过 `InternalsVisibleTo` 授权访问：

### BotConnection

| 成员 | 用途 |
|------|------|
| `State { internal set; }` | 更新连接状态（Idle → Connecting → Connected → Disconnected） |

> **注意**：`SelfId` 位于 `IBotAdapter` 接口上（由适配器自行维护），不在 `BotConnection` 上。

### MessageBody

| 成员 | 用途 |
|------|------|
| `static FromIncoming(IEnumerable<Segment>)` | 从接收到的协议数据构建消息体，跳过方向验证 |

> `FromIncoming()` 跳过了 `SegmentDirection` 验证，允许包含 `Incoming` 方向的 segment（如 `FileSegment`、`MarketFaceSegment`）。bot 开发者使用的 `Add()` 方法会拒绝 incoming-only segment。

### EventDispatcher

| 成员 | 用途 |
|------|------|
| `DispatchAsync(BotEvent, CancellationToken)` | 由 SoraService 调用，适配器不直接使用 |

### Segment incoming-only 属性

资源类 Segment 的 incoming-only 属性使用 `internal init`，仅适配器可在消息转换时设置：

| Segment | `internal init` 属性 |
|---------|---------------------|
| `ImageSegment` | `ResourceId`, `Url`, `Width`, `Height` |
| `AudioSegment` | `ResourceId`, `Url`, `Duration` |
| `VideoSegment` | `ResourceId`, `Url`, `Duration`, `Width`, `Height` |
| `ForwardSegment` | `ForwardId`, `Preview` |
| `FileSegment` | `FileId`, `FileName`, `FileSize`, `FileHash` |

Bot 开发者可设置的 outgoing 属性（`FileUri`、`ThumbUri`、`SubType`、`Messages` 等）保持 `public init`。

## 实现 IBotAdapter + IAdapterEventSource

```csharp
public sealed class __YourProtocol__Adapter : IBotAdapter, IAdapterEventSource
{
    public string ProtocolName => "__YourProtocol__";
    public AdapterState State { get; private set; } = AdapterState.Stopped;
    public UserId SelfId { get; private set; }

    private event Func<BotEvent, ValueTask>? _onEvent;

    event Func<BotEvent, ValueTask> IAdapterEventSource.OnEvent
    {
        add => _onEvent += value;
        remove => _onEvent -= value;
    }

    private BotConnection? _connection;

    public async ValueTask StartAsync(CancellationToken ct = default)
    {
        State = AdapterState.Starting;

        __YourProtocol__BotApi botApi = new(...);
        _connection = new BotConnection
        {
            ConnectionId = Guid.NewGuid(),
            Api          = botApi,
            State        = ConnectionState.Connecting
        };

        // 启动网络连接...
        State = AdapterState.Running;
    }

    public async ValueTask StopAsync(CancellationToken ct = default)
    {
        State = AdapterState.Stopping;
        // 断开网络连接...
        SelfId = default;
        State = AdapterState.Stopped;
    }

    private void HandleProtocolEvent(__ProtocolData__ data)
    {
        BotEvent? soraEvent = EventConverter.ToSoraEvent(data, _connection);

        if (soraEvent is not null && _connection is not null)
        {
            SelfId = soraEvent.SelfId;               // 适配器维护 SelfId
            _connection.State = ConnectionState.Connected;
        }

        if (soraEvent is not null)
            _ = _onEvent?.Invoke(soraEvent);
    }

    public IBotApi? GetApi() => _connection?.Api;
    public IBotConnection? GetConnection() => _connection;
    public async ValueTask DisposeAsync() => await StopAsync();
}
```

## 消息转换

```csharp
internal static class MessageConverter
{
    public static MessageBody ToMessageBody(__ProtocolMessage__ msg) =>
        MessageBody.FromIncoming(
            msg.Segments.Select(ConvertIncoming).OfType<Segment>());  // internal: 跳过方向验证
}
```

## 提供扩展方法入口

```csharp
public static class __YourProtocol__ServiceExtensions
{
    public static SoraService Create__YourProtocol__Service(
        this SoraServiceFactory factory,
        __YourProtocol__Config config)
    {
        __YourProtocol__Adapter adapter = new(config);
        return SoraServiceFactory.CreateService(adapter, config);
    }
}
```

## IBotApi 实现注意事项

- 所有方法均接受 `CancellationToken` 参数，需正确传递
- `ApiStatusCode` 分为四个区域：协议端错误（负数，如 `-404`）、成功（`0`）、HTTP 传输错误（`1-999`）、框架内部错误（`≥10000`）
- 协议端返回的负数错误码由 `MapRetCode` 通过 `Enum.IsDefined` 映射为 `ApiStatusCode.Protocol*` 值
- HTTP 层级错误使用原始 HTTP 状态码，框架已定义 `Unauthorized(401)`、`NotFound(404)` 等
- 使用 `ValueTask` 作为返回类型
- 协议特有功能通过 `IAdapterExtension` 扩展接口暴露，bot 开发者通过 `api.GetExtension<T>()` 访问

---

## 相关文档

- [← 返回 README](../README.md)
- [日志配置](LOGGING.md) — 适配器中的日志使用
- [测试说明](TESTING.md) — 测试架构与适配器测试
