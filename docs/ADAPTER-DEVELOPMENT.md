# Adapter 开发指南

本文档面向希望为 Sora 框架开发第三方协议适配器的开发者。

维护中的内置适配器为 Milky。`HoshikawaKaguya.Sora.Adapter.OneBot11` 已废弃并停止维护，不再安排功能开发、缺陷修复、协议对齐或测试维护；本文涉及 OB11 的内容仅记录现有实现。NuGet 包不设置 deprecated 标记。

> `__Name__`（双下划线包围）为占位符，请替换为实际名称。

## 概述

Sora 通过 `IBotAdapter` 接口抽象协议实现，任何协议（QQ、Telegram、Discord 等）都可以通过实现该接口接入框架。适配器项目需要引用 `HoshikawaKaguya.Sora` NuGet 包（框架应用层），并通过扩展方法提供创建入口。

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

> NuGet 包名使用 `HoshikawaKaguya.` 前缀（如 `HoshikawaKaguya.Sora.Core`），但项目目录和 C# 命名空间保持 `Sora.*` 格式。

依赖方向自上而下。适配器位于最上层，引用 `HoshikawaKaguya.Sora` NuGet 包即可获得全部框架能力。

各项目的关键职责：

| 项目 | NuGet 包 | 职责 |
|------|----------|------|
| `Sora.Core` | `HoshikawaKaguya.Sora.Core` | 值类型（`UserId`、`GroupId`、`MessageId`）、枚举（`MatchType`、`SegmentType`、`MessageSourceType` 等）、`ApiResult` 结果类型 |
| `Sora.Entities` | `HoshikawaKaguya.Sora.Entities` | 消息段（`Segment`、`MessageBody`）、事件（`BotEvent`、`MessageReceivedEvent` 等）、数据模型（`UserInfo`、`GroupInfo` 等）、核心接口（`IBotApi`、`IBotAdapter`、`IBotService`）、`EventDispatcher`、`MessageWaiter` |
| `Sora.Command` | `HoshikawaKaguya.Sora.Command` | 独立的命令扩展包：`[Command]`/`[CommandGroup]` 特性扫描、内置匹配策略、`CommandManager` 命令路由及命令过滤器 |
| `Sora` | `HoshikawaKaguya.Sora` | `SoraServiceFactory` 工厂、`SoraService` 事件管线（Waiter → Commands → Dispatcher） |
| `Sora.Adapter.*` | `HoshikawaKaguya.Sora.Adapter.*` | 协议实现：网络连接、事件/消息转换、`IBotApi` 实现、扩展接口（`IMilkyExtApi` 等） |

### 命令包边界

`Sora.Command` 自行维护命令扩展所需的实体，使用 `Sora.Command.InternalEntities` 命名空间，不将命令专用元数据或执行状态放入 `Sora.Entities`。该命名空间表示实体归属，不等同于 C# 的 `internal` 可见性；公共过滤器契约涉及的类型仍须可公开访问。

命令匹配方式由 `MatchType` 的 `Full`、`Regex`、`Keyword` 定义，与内置 `ICommandMatcher` 实现一一对应，由 `CommandManager` 内部固定字典保存。增加匹配方式需要框架同时修改枚举和对应实现，包使用者无法扩展该枚举，因此 matcher 注册不是对外扩展点。

### 事件管线

```
协议网络层 → Converter → BotEvent → SoraService
  → 触发者用户策略（BlockUsers 拦截；SuperUsers 标记）
  → MessageWaiter.TryMatch（连续对话，最高优先级 — 跳过所有过滤器）
  → PipelineContext 初始化
  → IEventPreFilter 阶段（scope-aware，可预处理或拦截事件）
  → 路由阶段（前置阶段未失败且事件链允许继续时）
      → CommandManager.HandleMessageEventAsync（命令匹配，如启用）
          → 匹配 → 权限/重入检查
          → CommandBeforeFilterAttribute 阶段（opt-in attribute，可阻止执行）
          → 执行命令处理器
          → CommandAfterFilterAttribute 阶段 → 重入占用作用域结束
      → EventDispatcher（事件链允许继续时按类型分发）
  → IEventPostFilter 阶段（scope-aware，正常完成或短路后进入）
```

适配器只需将协议数据转换为 `BotEvent` 并通过 `IAdapterEventSource.OnEvent` 触发，后续管线由 `SoraService` 自动处理。

管线只将携带当前操作实际接收的 Sora token、且该 token 已取消的 `OperationCanceledException` 原样向上传播，不记录为执行错误。其他取消异常按普通用户回调异常记录并隔离；若 Sora token 同时已取消，框架另行发出携带该 token 的取消。扩展自行创建 linked token source 时，应在自身边界将 Sora 取消转换为传入 token 的取消异常，框架不追踪 token 的关联关系。

普通用户回调错误在执行方法内部记录并隔离，正常完成、普通执行错误或短路后继续进入对应后置阶段。Sora 取消立即向外传播，不再执行后续 handler、after-filter 或 post-filter；执行链完整性不作为取消后的保证。编排层直接顺序调用各阶段，不捕获或暂存异常。重入标记通过包内部的值类型占用作用域及 `using` 保证释放，释放操作不执行后置回调。该契约不改变适配器调度；适配器仍负责观察回调完成或失败。

> **注意**：过滤器由框架用户配置。事件过滤器通过 `service.UseEventPreFilter()` / `service.UseEventPostFilter()` 显式注册（带 `EventTypes` / `SourceTypes` / `Predicate` 作用域属性）。命令过滤器以 attribute 形式贴在 `[Command]` 方法或 `[CommandGroup]` 类上，框架按 attribute 自动发现，无须注册。适配器开发者无需关心过滤器机制 — 它在 `SoraService` / `CommandManager` 内部透明运作。


### 用户策略与事件触发者

框架内部按具体事件类型读取已有字段来识别触发者；OB11 专有事件的判定由适配器负责。协议未提供触发者时，不从被操作对象或会话 ID 猜测。消息取 SenderId，撤回/禁言/群管理取 OperatorId，邀请取 InvitorId，主动申请取申请者；踢出取操作者，主动退群取离开者。成员加入取已知审批者、邀请者或自主加入者；置顶事件由当前账号触发。仅报告头衔/名片变更目标、连接状态或下载状态的事件不推断触发者。

服务在自动已读、waiter 和过滤器之前应用 BlockUsers；命中即停止整个事件。SuperUsers 设置 `IsSuperUser`，可供过滤器和 handler 读取。`[Command(SuperUserOnly = true)]` 和动态注册 `superUserOnly: true` 仅允许超级用户执行，同时继续检查 `PermissionLevel`。同时出现在两份名单的用户被拦截。

### 命令实例与连续对话

实例命令按声明类型使用 singleton：优先使用扫描前注册的实例，其次调用 public/non-public 无参构造函数；没有无参构造函数时，使用 `RuntimeHelpers.GetUninitializedObject` 创建实例。这是受支持的实例化方式，不执行构造函数和字段初始化器，字段为零值。需要初始化状态的命令应提供无参构造函数，或先调用 `RegisterCommandInstance<T>()`。

连续对话按连接、发送者、消息来源和群身份原子登记；非群消息忽略 GroupId，同源重复登记返回 null。timeout 按 Task.Delay 的范围在登记前校验，非法值抛 ArgumentOutOfRangeException。消息、超时、取消及断开只有一个完成方，后续完成不能覆盖已交付的结果。

### 连接生命周期

Milky WS/SSE 的 `StartAsync` 调度后台连接循环后返回，并不代表已经连通；通过 OnConnected 获取就绪通知。循环拥有 linked CTS 和网络资源，`StopAsync` 取消并等待释放。正的 ReconnectInterval 保留自动重连，零值只尝试首次连接；连接失败通过日志和断开事件报告。OB11 的失败/断开重试间隔使用 ReconnectInterval，心跳超时独立控制连接存活检测。

Milky HTTP API 的非零 retcode 保留原响应 Message 和 Data，统一失败状态不丢弃诊断信息。OB11 Debug 连接 URL 按设计包含配置的 query token，用于完整连接诊断。

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

适配器程序集请遵循 `Sora.Adapter.__ProtocolName__` 的命名规范（对应 NuGet 包 ID 为 `HoshikawaKaguya.Sora.Adapter.__ProtocolName__`）。

## 注册 InternalsVisibleTo

适配器需要访问 `Sora.Entities` 中的 `internal` 成员（如 `BotConnection.State` setter、`MessageBody.FromIncoming()` 等）。由于 C# 的 `InternalsVisibleTo` 不支持通配符，第三方适配器需要通过 **Pull Request** 将自己的程序集名称注册到框架中。

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
