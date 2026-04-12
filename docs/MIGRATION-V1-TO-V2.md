# 从 Sora 1.x 迁移到 2.0

> 本文档面向 Sora 1.x 用户，帮助你将现有机器人项目迁移到全新重构的 2.0 版本。

## 概述

Sora 2.0 是一次完全的从零重构，**不是**对 1.x 的增量升级。核心理念从「单协议轻量框架」转变为「多协议模块化框架」：

| 维度 | 1.x | 2.0 |
|------|-----|-----|
| .NET 版本 | .NET 6 | .NET 10 |
| 协议支持 | OneBot v11 only | **Milky**（主要）+ OneBot v11（兼容）|
| 项目结构 | 单一 NuGet 包 | 6 个模块化项目 |
| CQ 码 | ✅ 支持 | ❌ **已移除** |
| 消息段 | `SoraSegment` 类 | 多态 `Segment` record 继承体系 |
| 事件 | 分离的 `*EventArgs` | 统一的 `BotEvent` 继承体系 |
| API | `SoraApi` 具体类 | `IBotApi` 接口 |
| ID 类型 | `long` | 强类型 `UserId`、`GroupId`、`MessageId` |
| 返回值 | `ApiStatus` | `ApiResult<T>` 泛型结果 |
| 日志 | YukariToolBox | Serilog |

> **重要提示**：由于变更范围过大，建议新建项目后逐步迁移业务逻辑，而非尝试原地升级。

---

## 1. 环境准备

### 1.1 升级 .NET SDK

2.0 要求 **.NET 10 SDK**（使用 C# 预览版语言特性）。

```shell
# 验证 SDK 版本
dotnet --version
# 输出应为 10.0.x
```

### 1.2 更新 NuGet 包

1.x 只有一个 `Sora` 包。2.0 拆分为多个包，按需安装：

```xml
<!-- 1.x：一个包搞定 -->
<PackageReference Include="Sora" Version="1.x.x" />
```

```xml
<!-- 2.0：框架 + 协议适配器 -->
<PackageReference Include="HoshikawaKaguya.Sora" Version="2.x.x" />
<PackageReference Include="HoshikawaKaguya.Sora.Adapter.Milky" Version="2.x.x" />    <!-- Milky 协议（推荐）-->
<PackageReference Include="HoshikawaKaguya.Sora.Adapter.OneBot11" Version="2.x.x" /> <!-- 或 OneBot v11 -->
```

### 1.3 更新目标框架

```xml
<!-- 1.x -->
<TargetFramework>net6.0</TargetFramework>

<!-- 2.0 -->
<TargetFramework>net10.0</TargetFramework>
<LangVersion>preview</LangVersion>
```

---

## 2. 项目结构变化

1.x 所有功能都在一个 `Sora` 项目中。2.0 按职责拆分为 6 个项目：

> NuGet 包名使用 `HoshikawaKaguya.` 前缀（如 `HoshikawaKaguya.Sora.Core`），但项目目录和 C# 命名空间保持 `Sora.*` 格式。

```
Sora.Core              ← 值类型（UserId/GroupId/MessageId）、枚举、结果类型
  ↑
Sora.Entities          ← Segment、事件、Info 模型、IBotApi、EventDispatcher
  ↑
Sora.Command           ← [Command]/[CommandGroup] 属性路由
  ↑
Sora (facade)          ← SoraServiceFactory、SoraService — 组装入口
  ↑
Sora.Adapter.*         ← 协议适配器（Milky / OneBot v11）
```

作为 bot 开发者，你只需引用 `HoshikawaKaguya.Sora` + 对应的 `HoshikawaKaguya.Sora.Adapter.*` 包即可，其余依赖会自动传递。

---

## 3. 服务创建

### 1.x 写法

```csharp
using Sora;
using Sora.Net.Config;

// 通过配置类型区分正向/反向 WS
ISoraService service = SoraServiceFactory.CreateService(new ClientConfig
{
    Host = "127.0.0.1",
    Port = 6700
});

await service.StartService();
```

### 2.0 写法

```csharp
using Sora;
using Sora.Adapter.Milky;      // 或 Sora.Adapter.OneBot11

// 通过协议专用扩展方法创建
SoraService service = SoraServiceFactory.Instance.CreateMilkyService(
    new MilkyConfig
    {
        Host        = "127.0.0.1",
        Port        = 3010,
        AccessToken = "your-token"
    });

// 或 OneBot v11:
// SoraService service = SoraServiceFactory.Instance.CreateOneBot11Service(
//     new OneBot11Config
//     {
//         Host = "127.0.0.1",
//         Port = 6700,
//         Mode = ConnectionMode.ForwardWebSocket
//     });

await service.StartAsync();
```

**主要变化：**

| 变化点 | 1.x | 2.0 |
|--------|-----|-----|
| 工厂方法 | `SoraServiceFactory.CreateService(config)` | `SoraServiceFactory.Instance.CreateMilkyService(config)` |
| 返回类型 | `ISoraService` | `SoraService`（具体类型） |
| 启动方法 | `service.StartService()` | `service.StartAsync()` |
| 配置类 | `ClientConfig` / `ServerConfig` | `MilkyConfig` / `OneBot11Config` |

---

## 4. 事件系统

这是迁移中最大的变化之一。1.x 按消息来源分为不同的事件类型，2.0 统一为单一事件。

### 4.1 消息事件

**1.x：分离的群聊/私聊事件**

```csharp
// 1.x
service.Event.OnGroupMessage += async (_, e) =>
{
    // e: GroupMessageEventArgs
    await e.Reply("收到群消息");
};

service.Event.OnPrivateMessage += async (_, e) =>
{
    // e: PrivateMessageEventArgs
    await e.Reply("收到私聊消息");
};
```

**2.0：统一的消息事件**

```csharp
// 2.0
service.Events.OnMessageReceived += async e =>
{
    // e: MessageReceivedEvent
    // 通过 SourceType 区分来源
    if (e.Message.SourceType == MessageSourceType.Group)
        await e.Api.SendGroupMessageAsync(e.Message.GroupId, new MessageBody("收到群消息"));
    else
        await e.Api.SendFriendMessageAsync(e.Message.SenderId, new MessageBody("收到私聊消息"));
};
```

### 4.2 事件订阅方式变化

```csharp
// 1.x：(sender, eventArgs) 委托签名
service.Event.OnGroupMessage += async (_, e) => { ... };

// 2.0：单参数委托签名
service.Events.OnMessageReceived += async e => { ... };
```

### 4.3 事件名称映射

| 1.x 事件 | 2.0 事件 | 说明 |
|----------|---------|------|
| `OnGroupMessage` | `OnMessageReceived` | 统一，检查 `e.Message.SourceType == Group` |
| `OnPrivateMessage` | `OnMessageReceived` | 统一，检查 `e.Message.SourceType == Friend` |
| `OnSelfGroupMessage` | `OnMessageReceived` | 检查 `e.Message.SenderId == e.SelfId` |
| `OnSelfPrivateMessage` | `OnMessageReceived` | 同上 |
| `OnClientConnect` | `OnConnected` | — |
| — | `OnDisconnected` | 2.0 新增 |
| `OnFriendRequest` | `OnFriendRequest` | 类型改为 `FriendRequestEvent` |
| `OnGroupRequest` (加群请求) | `OnGroupJoinRequest` | 类型改为 `GroupJoinRequestEvent` |
| `OnGroupMemberChange` | `OnMemberJoined` / `OnMemberLeft` | 拆分为加入和离开 |
| `OnGroupAdminChange` | `OnGroupAdminChanged` | — |
| `OnGroupMuteEvent` | `OnGroupMute` | — |
| `OnGroupRecall` | `OnMessageDeleted` | — |
| `OnFriendRecall` | `OnMessageDeleted` | 统一，检查 `SourceType` |
| `OnGroupPoke` | `OnNudge` | — |
| `OnFileUpload` | `OnFileUpload` | — |
| `OnEssenceChange` | `OnGroupEssenceChanged` | — |
| — | `OnGroupReaction` | 2.0 新增 |
| — | `OnGroupNameChanged` | 2.0 新增 |
| — | `OnGroupInvitation` | 2.0 新增 |
| — | `OnPeerPinChanged` | 2.0 新增（Milky 协议）|

### 4.4 事件基类变化

```csharp
// 1.x
public class BaseSoraEventArgs : EventArgs
{
    public long LoginUid;       // Bot 自身 QQ 号
    public SoraApi SoraApi;     // API 实例
}

// 2.0
public abstract record BotEvent
{
    public required IBotApi Api { get; init; }   // API 实例
    public UserId SelfId { get; init; }          // Bot 自身 ID
    public Guid ConnectionId { get; init; }      // 连接 ID
    public DateTime Time { get; init; }          // 事件时间
    public bool IsContinueEventChain { get; set; } = true;  // 控制事件链
}
```

### 4.5 事件属性访问

```csharp
// 1.x：直接获取消息体
MessageBody body = e.Message.MessageBody;
long senderId = e.SenderInfo.UserId;
long groupId = e.SourceGroup.Id;

// 2.0：通过 MessageContext 统一访问
MessageBody body = e.Message.Body;
UserId senderId = e.Message.SenderId;
GroupId groupId = e.Message.GroupId;
string text = e.Message.Body.GetText();  // 提取纯文本
```

---

## 5. 消息段（Segment）

### 5.1 CQ 码已移除

2.0 **完全移除了 CQ 码支持**。所有消息都必须通过类型安全的 Segment 构建。

```csharp
// 1.x：CQ 码字符串
MessageBody body = "[CQ:at,qq=123456] 你好！[CQ:image,file=abc.jpg]";

// 2.0：不再支持！必须使用类型化 Segment
```

### 5.2 Segment 类型变化

1.x 使用 `SoraSegment` 类 + 静态工厂方法。2.0 使用多态 record 继承体系。

**1.x：**

```csharp
// 1.x：静态工厂方法
SoraSegment textSeg = SoraSegment.Text("你好");
SoraSegment atSeg = SoraSegment.At(123456);
SoraSegment imgSeg = SoraSegment.Image("file:///path/to/image.jpg");
SoraSegment faceSeg = SoraSegment.Face(178);

MessageBody body = new()
{
    SoraSegment.Text("Hello "),
    SoraSegment.At(123456)
};
```

**2.0：**

```csharp
// 2.0：类型安全的 record 实例
TextSegment textSeg = new() { Text = "你好" };
MentionSegment atSeg = new() { Target = new UserId(123456) };
ImageSegment imgSeg = new() { FileUri = "file:///path/to/image.jpg" };
FaceSegment faceSeg = new() { FaceId = "178" };

// 构建消息体
MessageBody body = new([
    new TextSegment { Text = "Hello " },
    new MentionSegment { Target = new UserId(123456) }
]);

// 或使用流式 API
MessageBody body2 = new MessageBody()
    .AddText("Hello ")
    .AddMention(new UserId(123456));

// 或使用运算符
MessageBody body3 = new TextSegment { Text = "Hello " }
    + new MentionSegment { Target = new UserId(123456) };

// 纯文本消息最简写法
MessageBody textOnly = new("Hello World");
```

### 5.3 Segment 类型名称映射

| 1.x `SoraSegment.*` 方法 | 2.0 Segment 类型 | 说明 |
|--------------------------|------------------|------|
| `SoraSegment.Text(text)` | `TextSegment` | `{ Text = "..." }` |
| `SoraSegment.At(qq)` | `MentionSegment` | `{ Target = new UserId(qq) }` |
| `SoraSegment.AtAll()` | `MentionAllSegment` | `new()` |
| `SoraSegment.Image(file)` | `ImageSegment` | `{ FileUri = "..." }` |
| `SoraSegment.Record(file)` | `AudioSegment` | `{ FileUri = "..." }`，注意改名 |
| `SoraSegment.Video(file)` | `VideoSegment` | `{ FileUri = "..." }` |
| `SoraSegment.Face(id)` | `FaceSegment` | `{ FaceId = "..." }` |
| `SoraSegment.Reply(msgId)` | `ReplySegment` | `{ TargetId = new MessageId(msgId) }` |
| `SoraSegment.Json(data)` | `LightAppSegment` | `{ JsonPayload = "...", AppName = "..." }` |
| `SoraSegment.Xml(data)` | `XmlSegment` | 仅接收方向（incoming-only）|
| `SoraSegment.Poke(...)` | — | 移至 API：`SendGroupNudgeAsync()` |
| `SoraSegment.Dice()` | — | OB11 专属，不在框架层 |
| `SoraSegment.Rps()` | — | OB11 专属，不在框架层 |
| `SoraSegment.Music(...)` | — | 暂不支持 |

### 5.4 消息体辅助方法

```csharp
// 2.0 新增的便捷方法
string text = body.GetText();                       // 提取所有文本
ImageSegment? img = body.GetFirst<ImageSegment>();   // 获取第一个图片
IEnumerable<MentionSegment> ats = body.GetAll<MentionSegment>(); // 获取所有@

// 发送验证
bool valid = body.IsValidForSending();
IReadOnlyList<string> errors = body.Validate();
```

### 5.5 Segment 方向概念（2.0 新增）

> 基于Milky协议设计

2.0 引入了 `SegmentDirection` 概念，区分消息段的使用方向：

- **Both** — 可收可发（`TextSegment`、`ImageSegment`、`MentionSegment` 等）
- **Incoming** — 仅接收（`FileSegment`、`MarketFaceSegment`、`XmlSegment`）
- **Outgoing** — 仅发送

incoming-only 的 Segment 通过 `MessageBody.Add()` 添加会被自动转换为outgoing segment。

---

## 6. API 调用

### 6.1 API 访问方式

```csharp
// 1.x：通过 EventArgs 上的 SoraApi 或 service.GetApi(connId)
SoraApi api = e.SoraApi;
// 或
SoraApi api = service.GetApi(connectionId);

// 2.0：通过事件上的 Api 属性
IBotApi api = e.Api;
```

### 6.2 返回值变化

1.x 的 API 方法返回元组或直接值。2.0 统一使用 `ApiResult<T>` 泛型结果：

```csharp
// 1.x
(ApiStatus status, int messageId) = await api.SendGroupMsg(groupId, body);
if (status.RetCode == ApiStatusType.Ok)
    Console.WriteLine($"发送成功，消息ID: {messageId}");

// 2.0
SendMessageResult result = await e.Api.SendGroupMessageAsync(groupId, body);
if (result.IsSuccess)
    Console.WriteLine($"发送成功，消息ID: {result.MessageId}");
else
    Console.WriteLine($"发送失败: {result.Message}");
```

```csharp
// 1.x
(ApiStatus status, List<GroupInfo> groups) = await api.GetGroupList();

// 2.0 — Data 在失败时为 null，需通过模式匹配安全访问
ApiResult<IReadOnlyList<GroupInfo>> result = await e.Api.GetGroupListAsync();
if (result is { IsSuccess: true, Data: { } groups })
{
    foreach (GroupInfo group in groups)
        Console.WriteLine(group.GroupName);
}
```

### 6.3 API 方法名变化

2.0 的 API 方法统一采用 `XxxAsync` 命名，使用强类型参数：

| 1.x 方法 | 2.0 方法 |
|----------|---------|
| `SendGroupMsg(long, MessageBody)` | `SendGroupMessageAsync(GroupId, MessageBody)` |
| `SendPrivateMsg(long, MessageBody)` | `SendFriendMessageAsync(UserId, MessageBody)` |
| `RecallMsg(int)` | `RecallGroupMessageAsync(GroupId, MessageId)` / `RecallPrivateMessageAsync(UserId, MessageId)` |
| `GetGroupInfo(long)` | `GetGroupInfoAsync(GroupId, bool noCache)` |
| `GetGroupList()` | `GetGroupListAsync(bool noCache)` |
| `GetGroupMemberInfo(long, long)` | `GetGroupMemberInfoAsync(GroupId, UserId, bool noCache)` |
| `GetGroupMemberList(long)` | `GetGroupMemberListAsync(GroupId, bool noCache)` |
| `GetUserInfo(long)` | `GetUserInfoAsync(UserId, bool noCache)` |
| `GetFriendList()` | `GetFriendListAsync(bool noCache)` |
| `GetLoginInfo()` | `GetSelfInfoAsync()` |
| `SetGroupBan(long, long, int)` | `MuteGroupMemberAsync(GroupId, UserId, int durationSeconds)` |
| `SetGroupWholeBan(long, bool)` | `MuteGroupAllAsync(GroupId, bool enable)` |
| `SetGroupKick(long, long, bool)` | `KickGroupMemberAsync(GroupId, UserId, bool rejectFuture)` |
| `SetGroupAdmin(long, long, bool)` | `SetGroupAdminAsync(GroupId, UserId, bool enable)` |
| `SetGroupCard(long, long, string)` | `SetGroupMemberCardAsync(GroupId, UserId, string card)` |
| `SetGroupName(long, string)` | `SetGroupNameAsync(GroupId, string name)` |
| `SetGroupLeave(long)` | `LeaveGroupAsync(GroupId)` |
| `SetFriendAddRequest(string, bool, string)` | `HandleFriendRequestAsync(UserId, bool isFiltered, bool approve, string remark)` |
| `SetGroupAddRequest(string, ...)` | `HandleGroupRequestAsync(GroupId, long notificationSeq, ...)` |

### 6.4 强类型 ID

2.0 引入了包装类型替代裸 `long`，防止混淆不同类型的 ID：

```csharp
// 1.x
long userId = 123456;
long groupId = 654321;
await api.SendGroupMsg(groupId, body);   // 容易混淆参数顺序

// 2.0
UserId userId = new(123456);
GroupId groupId = new(654321);
await api.SendGroupMessageAsync(groupId, body);  // 类型安全，编译器检查

// 从事件中获取
UserId sender = e.Message.SenderId;
GroupId group = e.Message.GroupId;
```

### 6.5 协议扩展 API（2.0 新增）

对于协议专有功能，2.0 通过扩展接口暴露：

```csharp
// 获取 Milky 专有 API
IMilkyExtApi? milkyApi = e.Api.GetExtension<IMilkyExtApi>();
if (milkyApi is not null)
{
    // 调用 Milky 特有功能
}
```

---

## 7. 指令系统

### 7.1 属性变化

**1.x：**

```csharp
using Sora.Attributes.Command;
using Sora.Enumeration;

[CommandSeries(SeriesName = "test")]
public static class Commands
{
    [SoraCommand(
        CommandExpressions = new[] { "hello" },
        Description = "打招呼",
        SourceType = MessageSourceMatchFlag.All,
        MatchType = MatchType.Full,
        Priority = 0)]
    public static async ValueTask Hello(BaseMessageEventArgs e)
    {
        e.IsContinueEventChain = false;
        await e.Reply("你好！");
    }
}
```

**2.0：**

```csharp
// using 不再需要手动引入 — GlobalUsings 覆盖

[CommandGroup(Name = "test", Prefix = "/")]
public static class Commands
{
    [Command(
        Expressions = ["hello"],
        Description = "打招呼",
        MatchType = MatchType.Full,
        Priority = 0,
        BlockAfterMatch = true,             // 替代 IsContinueEventChain = false
        ReentryMessage = "指令执行中，请稍候")]  // 可选：重入时回复用户
    public static async ValueTask Hello(MessageReceivedEvent e)
    {
        MessageBody reply = new("你好！");
        if (e.Message.SourceType == MessageSourceType.Group)
            await e.Api.SendGroupMessageAsync(e.Message.GroupId, reply);
        else
            await e.Api.SendFriendMessageAsync(e.Message.SenderId, reply);
    }
}
```

### 7.2 属性名称映射

| 1.x | 2.0 | 说明 |
|-----|-----|------|
| `[CommandSeries]` | `[CommandGroup]` | — |
| `SeriesName` | `Name` | — |
| `GroupPrefix` | `Prefix` | — |
| `[SoraCommand]` | `[Command]` | — |
| `CommandExpressions` | `Expressions` | 类型从 `string[]` 改为集合表达式 `["..."]` |
| `SourceType` | `SourceType` | 类型从 `MessageSourceMatchFlag` 改为 `MessageSourceType?`（null = 全部） |
| `PermissionLevel` | `PermissionLevel` | 类型从 `MemberRoleType` 改为 `MemberRole` |
| `SuperUserCommand` | — | 已移除，可在指令处理中自行实现 |
| `SourceGroups` / `SourceUsers` / `SourceLogins` | — | 已移除，可在指令处理中自行过滤 |
| `e.IsContinueEventChain = false` | `BlockAfterMatch = true`（属性级别）| 也可运行时设置 `e.IsContinueEventChain = false` |
| — | `PreventReentry = true`（默认值）| 同一用户重复触发未完成的指令时跳过执行，支持连续对话场景 |
| — | `ReentryMessage = "..."`（可选纯文本）| 设置后，被阻止时自动回复该文本给用户 |
| `BaseMessageEventArgs` | `MessageReceivedEvent` | 指令方法参数类型 |

### 7.3 指令注册

```csharp
// 1.x：自动扫描（通过 EventAdapter 内部注册）
// 无需手动调用

// 2.0：显式扫描程序集
service.Commands.ScanAssembly(typeof(Program).Assembly);
```

### 7.4 动态指令

```csharp
// 1.x
service.Event.CommandManager.RegisterDynamicCommand(
    new[] { "测试" },
    async e =>
    {
        e.IsContinueEventChain = false;
        await e.Reply("动态指令");
    },
    MessageSourceMatchFlag.Group);

// 2.0：通过 CommandManager 注册
service.Commands.RegisterDynamicCommand(
    async e =>
    {
        MessageBody reply = new("动态指令");
        await e.Api.SendGroupMessageAsync(e.Message.GroupId, reply);
    },
    ["测试"],
    matchType: MatchType.Full,
    sourceType: MessageSourceType.Group,
    preventReentry: true,           // 默认 true，防止同一用户重复触发
    reentryMessage: "执行中，请稍候"); // 可选：被阻止时回复用户
```

### 7.5 回复消息

1.x 的 `e.Reply()` 扩展方法在 2.0 中被移除。2.0 需要通过 `IBotApi` 显式发送：

```csharp
// 1.x
await e.Reply("你好");
await e.Reply(SoraSegment.Image("xxx"));

// 2.0
if (e.Message.SourceType == MessageSourceType.Group)
    await e.Api.SendGroupMessageAsync(e.Message.GroupId, new MessageBody("你好"));
else
    await e.Api.SendFriendMessageAsync(e.Message.SenderId, new MessageBody("你好"));
```

---

## 8. 消息等待（连续对话）

1.x 和 2.0 都支持消息等待，但 API 略有不同：

```csharp
// 1.x（不同版本实现不同，此处为参考）
// 通常通过 WaitForNextMessage 扩展方法

// 2.0
using Sora.Entities.MessageWaiting;

// 等待同一用户的下一条消息
MessageReceivedEvent? next = await e.WaitForNextMessageAsync(
    TimeSpan.FromSeconds(30));

if (next is not null)
{
    string text = next.Message.Body.GetText();
    await next.Api.SendGroupMessageAsync(next.Message.GroupId, new MessageBody($"你说了: {text}"));
}
else
{
    // 超时，未收到消息
}
```

---

## 9. 日志系统

```csharp
// 1.x：YukariToolBox
using YukariToolBox.LightLog;

Log.LogConfiguration.EnableConsoleOutput().SetLogLevel(LogLevel.Debug);
Log.Info("MyBot", "启动成功");

// 2.0：Serilog（通过 Microsoft.Extensions.Logging 抽象）
using Microsoft.Extensions.Logging;

ILogger logger = SoraLogger.CreateLogger("MyBot");
logger.LogInformation("启动成功");

// 自定义日志工厂（可选）
// 在创建 SoraService 之前设置
ILoggerFactory customFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

详见 [日志配置](LOGGING.md)。

---

## 10. 连接管理

```csharp
// 1.x：ConnectionManager
service.ConnManager.OnOpenConnectionAsync += (connId, e) => { ... };
service.ConnManager.OnCloseConnectionAsync += (connId, e) => { ... };

// 2.0：通过事件
service.Events.OnConnected += async e =>
{
    Console.WriteLine($"已连接: {e.ConnectionId}");
};

service.Events.OnDisconnected += async e =>
{
    Console.WriteLine($"已断开: {e.Reason}");
};

// 获取适配器信息
string protocol = service.Adapter.ProtocolName;  // "Milky" 或 "OneBot11"
```

---

## 11. 命名空间速查表

| 1.x 命名空间 | 2.0 命名空间 |
|-------------|-------------|
| `Sora` | `Sora` |
| `Sora.Net.Config` | `Sora.Adapter.Milky` / `Sora.Adapter.OneBot11` |
| `Sora.Interfaces.ISoraService` | `Sora.SoraService` |
| `Sora.Entities.Base.SoraApi` | `Sora.Entities.Interfaces.IBotApi` |
| `Sora.Entities.Segment.SoraSegment` | `Sora.Entities.Segments.*` |
| `Sora.Entities.MessageBody` | `Sora.Entities.Message.MessageBody` |
| `Sora.EventArgs.SoraEvent.*` | `Sora.Entities.Events.*` |
| `Sora.Enumeration.SegmentType` | `Sora.Core.Enums.SegmentType` |
| `Sora.Enumeration.MatchType` | `Sora.Core.Enums.MatchType` |
| `Sora.Enumeration.EventParamsType.MemberRoleType` | `Sora.Core.Enums.MemberRole` |
| `Sora.Entities.Info.*` | `Sora.Entities.Info.*` |
| `Sora.Attributes.Command.CommandSeries` | `Sora.Command.Attributes.CommandGroupAttribute` |
| `Sora.Attributes.Command.SoraCommand` | `Sora.Command.Attributes.CommandAttribute` |
| `YukariToolBox.LightLog.Log` | `Sora.SoraLogger` / `Microsoft.Extensions.Logging.ILogger` |

---

## 12. 完整迁移示例

下面对比一个最小 bot 的 1.x 和 2.0 实现：

### 1.x

```csharp
using Sora;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using Sora.Interfaces;
using Sora.Net.Config;
using YukariToolBox.LightLog;

Log.LogConfiguration.EnableConsoleOutput().SetLogLevel(LogLevel.Debug);

ISoraService service = SoraServiceFactory.CreateService(new ServerConfig
{
    Port = 8199
});

service.Event.OnGroupMessage += async (_, e) =>
{
    if (e.Message.MessageBody.GetText() == "ping")
    {
        await e.Reply("pong");
    }
};

await service.StartService();
await Task.Delay(-1);
```

### 2.0

```csharp
using Sora;
using Sora.Adapter.Milky;

SoraService service = SoraServiceFactory.Instance.CreateMilkyService(
    new MilkyConfig
    {
        Host        = "127.0.0.1",
        Port        = 3010,
        AccessToken = "your-token"
    });

service.Events.OnMessageReceived += async e =>
{
    if (e.Message.Body.GetText() == "ping")
    {
        if (e.Message.SourceType == MessageSourceType.Group)
            await e.Api.SendGroupMessageAsync(e.Message.GroupId, new MessageBody("pong"));
        else
            await e.Api.SendFriendMessageAsync(e.Message.SenderId, new MessageBody("pong"));
    }
};

await service.StartAsync();
await Task.Delay(-1);
```

---

## 13. 迁移检查清单

- [ ] 更新 `.csproj` 目标框架为 `net10.0`，添加 `<LangVersion>preview</LangVersion>`
- [ ] 安装 NuGet 包：`HoshikawaKaguya.Sora` + `HoshikawaKaguya.Sora.Adapter.Milky`（或 `HoshikawaKaguya.Sora.Adapter.OneBot11`）
- [ ] 移除旧 `Sora` 1.x 包和 `YukariToolBox` 依赖
- [ ] 更新服务创建代码：使用 `SoraServiceFactory.Instance.CreateXxxService()`
- [ ] 将 `OnGroupMessage` + `OnPrivateMessage` 合并为 `OnMessageReceived`
- [ ] 替换所有 `SoraSegment.*` 为对应的 Segment record 类型
- [ ] 移除所有 CQ 码字符串，改用 `MessageBody` 构建
- [ ] 替换 `e.Reply(...)` 为 `e.Api.SendGroupMessageAsync(...)` / `SendFriendMessageAsync(...)`
- [ ] 将 `long` 类型的 ID 替换为 `UserId`、`GroupId`、`MessageId`
- [ ] 更新 API 调用：使用新方法名 + 处理 `ApiResult<T>` 返回值
- [ ] 迁移指令：`[CommandSeries]` → `[CommandGroup]`，`[SoraCommand]` → `[Command]`
- [ ] 添加 `service.Commands.ScanAssembly(typeof(Program).Assembly)` 注册指令
- [ ] 更新日志代码：`YukariToolBox.LightLog` → `SoraLogger` / `ILogger`
- [ ] 更新所有 `using` 命名空间
- [ ] 编译并修复所有编译错误
- [ ] 测试所有功能

---

## 相关文档

- [← 返回 README](../README.md)
- [日志配置](LOGGING.md) — 日志系统详细配置
- [Adapter 开发指南](ADAPTER-DEVELOPMENT.md) — 第三方协议适配器开发
- [测试说明](TESTING.md) — 测试架构与运行方式
