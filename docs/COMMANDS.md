# 指令模块（Sora.Command）

> 适用于 Sora 3.0 起的 `HoshikawaKaguya.Sora.Command`。从 2.x 升级请先阅读 [V2 → V3 迁移指南](MIGRATION-V2-TO-V3.md)。

`Sora.Command` 是独立的命令扩展包：它扫描 `[CommandGroup]` / `[Command]` 特性，支持运行时注册的动态指令，并按程序集管理指令的启停、执行顺序与注销。每个 `SoraService` 通过 `service.Commands` 暴露一个 `CommandManager`。

## 1. 概念与术语

| 术语 | 含义 |
|------|------|
| 程序集注册（`AssemblyRegistration`） | 一个 Assembly 在 manager 中从注册到注销的一次生命周期。该 Assembly 的所有指令、Type 状态与优先级都只能经由它登记和管理。注销后再次注册得到新的句柄 |
| 归属 Type | 一条指令由哪个 Type 管理 Type 级启停。扫描指令归属扫描时传入的 Type；动态指令归属注册时显式传入的 `ownerType` |
| 程序集优先级 | 决定不同 Assembly 之间的先后，数值大者先执行；相同时按注册先后 |
| 命令优先级 | `[Command(Priority = ...)]` 或动态注册的 `priority`，只决定同一 Assembly 内的先后；扫描指令与动态指令混合排序，相同时按注册先后。没有 Type 优先级 |
| 局部启用状态 | 单独为某个 Assembly 或某个 Type 设定的开关 |
| 有效启用 | 一条指令实际参与匹配的条件：所属 Assembly 与归属 Type 的局部启用状态同时为开 |
| 在途调用 | 一条已匹配的指令从 before-filter 开始到 after-filter 结束的整个过程，包括 handler 内等待后续消息的连续对话 |

归属只表示**管理范围**，不表示依赖关系：一条指令的方法体、过滤器或闭包仍可以引用其他程序集的代码。

## 2. 注册与扫描

所有注册都必须先取得程序集注册句柄：

```csharp
AssemblyRegistration commands = service.Commands.RegisterAssembly(typeof(Program).Assembly);
commands.ScanCommandGroups();
```

| API | 说明 |
|-----|------|
| `CommandManager.RegisterAssembly(assembly, priority = 0, enabled = true)` | 注册 Assembly 并返回句柄。`priority` / `enabled` 只在首次注册时生效；已注册时返回同一句柄、忽略参数并记录 Warning。注册本身不扫描 |
| `AssemblyRegistration.ScanCommandGroups()` | 扫描该 Assembly 中 exported、带 `[CommandGroup]` 的 class，返回新注册的指令数。跳过抽象的非静态类和开放泛型类型 |
| `AssemblyRegistration.ScanType(type)` | 扫描单个 Type；不要求 `[CommandGroup]`。`type` 必须定义在该 Assembly 中，否则抛 `ArgumentException`；抽象非静态类或开放泛型类型同样抛 `ArgumentException` |
| `AssemblyRegistration.RegisterCommandInstance<T>(instance)` | 为实例指令预注册单例，必须在扫描 `T` 之前调用，否则抛 `InvalidOperationException`；`T` 必须定义在该 Assembly 中 |
| `CommandManager.TryGetAssembly(assembly, out registration)` | 查找当前注册 |
| `CommandManager.GetCommandInstance<T>()` | 获取 `T` 的单例，未注册时返回 null |

扫描规则：

- 指令方法必须只有一个继承自 `BotEvent` 的参数，返回 `ValueTask` 或 `Task`；不符合的方法被忽略。
- 方法枚举包含继承的方法。继承的指令归属**被扫描的派生类型**及其 Assembly：例如 `PluginA.Commands : Shared.CommandsBase`，扫描 `PluginA.Commands` 得到的继承指令由插件 A 管理，与 `Shared.dll` 是否注册无关。
- 同一注册中重复扫描同一 Type 会跳过并记录 Warning，不会重复注册，也不会重置状态。
- 一次 `ScanCommandGroups` / `ScanType` 调用的结果整体发布：扫描过程中抛出异常（例如构造函数失败、依赖程序集缺失）时，本次调用不发布任何指令、不保留本批新建的实例，异常原样抛出。
- 扫描期间会持有 manager 的注册锁：同一 manager 的其他注册、启停、调优先级与状态查询会等待扫描结束，但消息处理不受影响。指令类构造函数应保持轻量，不要跨线程等待同一 manager 的操作。

### 指令实例

实例指令（非 static 的指令方法）按扫描的 Type 使用**单例**：该 Type 的所有实例指令共用同一个对象。扫描时按以下顺序取得实例：

1. 扫描前通过 `RegisterCommandInstance<T>()` 预注册的实例；
2. 调用该 Type 的无参构造函数（public 或 non-public 均可）；
3. 都没有时记录一条 Warning（`... has no parameterless constructor — using GetUninitializedObject ...`），使用 `RuntimeHelpers.GetUninitializedObject` 创建实例。

只含 static 指令的 Type 不会创建实例。

#### 未提供实例且缺少无参构造函数的后果

第 3 种方式是受支持的行为，不是错误，因此**扫描会成功，指令也会正常注册**，但得到的是一个“空壳”对象：

- 不执行任何实例构造函数，包括有参构造函数和基类构造函数；
- 不执行字段初始化器和自动属性初始化器，例如 `private readonly List<int> _items = new();`、`public int Limit { get; } = 10;` 都不会生效；
- 所有实例字段为零值或 `null`（`readonly` 字段同样如此）；静态字段和静态构造函数不受影响。

因此问题不会在启动时暴露，而是在指令被触发时出现：handler 访问这些字段时通常抛 `NullReferenceException`，或者静默地使用错误的默认值（如 `0`、`false`）。抛出的异常会按普通指令失败处理：记录 Error 日志，after-filter 收到该异常，其他指令照常执行。每次触发都会重复失败，直到修正注册方式。

无参构造函数存在但抛出异常时，异常以 `TargetInvocationException` 抛给扫描调用方，本次扫描不注册任何指令。

#### 设计建议

- **尽量不要让 Sora 创建实例。** 只要 Type 含有实例指令，就由宿主或插件自己构造实例，并在扫描前用 `RegisterCommandInstance` 预注册。依赖关系（配置、HttpClient、数据库连接等）通过构造函数显式传入，不要依赖框架的构造方式。

  ```csharp
  AssemblyRegistration registration = service.Commands.RegisterAssembly(pluginAssembly);
  registration.RegisterCommandInstance(new WeatherCommands(config, httpClient));
  registration.ScanCommandGroups();
  ```

- **以具体类型调用 `RegisterCommandInstance`。** 实例按泛型参数 `T`（即参数的静态类型）登记，不是按实例的运行时类型。用基类类型的变量传入时，实例会登记在基类下：基类在其他程序集时会抛 `ArgumentException`；基类在同一程序集时，扫描派生类型会找不到它，转而创建空壳实例。应写 `RegisterCommandInstance(new WeatherCommands(...))` 或 `RegisterCommandInstance<WeatherCommands>(instance)`。
- **必须在扫描该 Type 之前预注册**；扫描之后调用会抛 `InvalidOperationException`，实例也不能再替换。需要更换状态时，通过 `GetCommandInstance<T>()` 取得同一实例修改，或注销后重新注册程序集。
- **无状态的指令优先写成 static 方法或 static class**，这样根本不需要实例。
- **单例状态必须线程安全。** 不同消息可能并发执行同一 Type 的指令；共享状态使用 `Interlocked`、`ConcurrentDictionary` 或 `Lock`。单次调用内的临时数据放在局部变量里，不要写入字段。
- **构造函数保持轻量**：只保存依赖，不做网络请求或长时间初始化；扫描时持有注册锁，不要在构造函数中跨线程等待同一 manager 的操作。
- **实例的生命周期由宿主负责。** 停用再启用不会重建实例；`UnregisterAsync` 只释放 manager 的引用，不调用 `Dispose` / `DisposeAsync`。宿主应保留自己创建的实例，在 `UnregisterAsync` 完成后再释放资源。
- 启动日志中出现上述 `GetUninitializedObject` Warning 时，按配置错误处理：为该 Type 预注册实例，或在确认无状态时改为 static 指令。

## 3. 特性指令

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
```

`[CommandGroup]`：`Name` 用于组织，`Prefix` 会加在组内全部表达式之前（Regex 匹配时前缀被转义并插入到 `^` 之后）。

`[Command]` 属性：

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `Expressions` | `[]` | 匹配表达式 |
| `MatchType` | `Full` | `Full` 全文匹配、`Regex` 正则、`Keyword` 包含关键字。匹配方式与内置匹配器一一对应，不可扩展 |
| `SourceScope` | `Any` | 指令接受的消息来源范围（`MessageSourceScope`），可组合；`Any` 表示不限来源 |
| `PermissionLevel` | `Member` | 最低群角色 |
| `SuperUserOnly` | `false` | 额外要求触发者为 `SuperUsers` 中的用户；超级用户不会绕过群角色检查 |
| `Priority` | `0` | 命令优先级，只在所属 Assembly 内比较 |
| `BlockAfterMatch` | `true` | 匹配后阻断事件链 |
| `PreventReentry` | `true` | 同一用户在该指令执行完成前再次触发时跳过 |
| `ReentryMessage` | `""` | 重入被拒绝时回复的纯文本 |
| `Description` | `""` | 帮助文本 |

`MessageSourceScope` 是 `[Flags]` 枚举（`Friend`、`Group`、`Temp`，`Any` = 三者之和），只用于声明指令或事件过滤器接受哪些来源；接收到的消息本身的来源仍是 `MessageSourceType`，不会出现 `Any`。例如只在私聊（好友与临时会话）中响应：

```csharp
[Command(Expressions = ["whoami"], SourceScope = MessageSourceScope.Friend | MessageSourceScope.Temp)]
public static ValueTask WhoAmI(MessageReceivedEvent e)
{
    // 只会在好友消息或临时会话消息中触发，群消息不会匹配
    return ValueTask.CompletedTask;
}
```

范围中不含任何来源（例如 `0`）的指令在注册时记录 Warning，并且永远不会匹配。

配置 `BlockUsers` 后，框架在自动已读、连续对话和过滤器之前忽略该触发者的整个事件；同时出现在两份名单中的用户被拦截。

## 4. 动态指令与 Registrar

动态指令通过注册句柄的 `Registrar` 注册，第一个参数 `ownerType` 必填：

```csharp
Guid id = commands.Registrar.RegisterDynamicCommand(
    typeof(MyPluginCommands),
    async e => await ReplyAsync(e, "pong"),
    ["ping"],
    matchType: MatchType.Full,
    priority: 10,
    beforeFilters: [new CooldownAttribute(5)]);

commands.Registrar.UnregisterDynamicCommand(id);
```

- `ownerType` 必须是**该 Assembly 自己定义的类型**（按运行时对象比较）。Sora、运行时或其他 Assembly（包括插件所继承基类所在的 Assembly）的类型一律抛 `ArgumentException`。基类在 `Shared.dll` 时，用插件自己的派生类型作为 owner。
- 归属完全由“句柄 + `ownerType`”决定，不检查委托代码编译在哪个 DLL 中。因此宿主代码包装插件调用的 lambda 也能明确归属到插件。
- 动态指令与扫描指令遵循同一套启停、排序与注销规则；Assembly 或 `ownerType` 停用期间也可以注册和注销，注册的指令在两者都启用后开始匹配。
- `sourceScope` 与特性的 `SourceScope` 含义相同，默认 `MessageSourceScope.Any`。
- `UnregisterDynamicCommand` 只能注销本注册下的动态指令，找不到时返回 `false`。
- 直接贴在 lambda 上的过滤器特性会被自动发现，并与 `beforeFilters` / `afterFilters` 合并、按引用去重、按 `Order` 稳定排序。

### 框架式机器人

当主框架负责加载插件、插件只负责声明指令时，主框架保留 `AssemblyRegistration`（管理权），只把 `ICommandRegistrar`（注册权）交给插件：

```csharp
AssemblyRegistration registration = service.Commands.RegisterAssembly(pluginAssembly, config.Priority, enabled: false);
registration.RegisterCommandInstance(new PluginCommands(config));
registration.ScanCommandGroups();
plugin.Initialize(registration.Registrar);   // 主框架自定义的插件入口，Sora 不提供
registration.SetEnabled(true);
```

`Registrar` 由框架内部类实现，无法转换回 `AssemblyRegistration`。这是 API 约定而不是安全边界：同进程的插件仍可通过反射访问更多能力。

## 5. 执行顺序

指令按两级顺序执行：

1. 第一级：已启用的 Assembly 按**程序集优先级降序**排列，相同时按首次注册先后。
2. 第二级：每个 Assembly 内的有效启用指令按**命令优先级降序**排列，相同时按注册先后；扫描指令与动态指令混排，不按 Type 分组。

一个 Assembly 的指令全部执行完毕（包括 after-filter）后，才进入下一个 Assembly。

| Assembly | 程序集优先级 | 指令（命令优先级） |
|----------|-------------:|--------------------|
| A | 10 | A1 = 1，A2 = 100，动态 D1 = 50 |
| B | 5 | B1 = 1000 |

执行顺序为 `A2 → D1 → A1 → B1`：B1 的命令优先级再高也排在 A 之后。把 B 的优先级改为 20 后，下一条消息的顺序为 `B1 → A2 → D1 → A1`。即使所有程序集优先级都为 0，多个 Assembly 也按组执行，不会全局混排。

- `BlockAfterMatch = true` 或运行中设置 `e.IsContinueEventChain = false` 是**全局阻断**：当前 Assembly 剩余的指令和之后所有 Assembly 都不再执行，后续的普通事件分发也会停止。
- 重入被拒绝、before-filter 短路、handler 普通异常仍按该指令的 `BlockAfterMatch` 阻断。
- 执行顺序表只在指令表变化时重建（注册、注销、扫描、启停、调优先级），消息处理时只读取当前版本，不排序。每条消息只读取一次：启停与调优先级从**下一条消息**起生效，已开始处理的消息仍按旧顺序执行完。
- 顺序保证针对同一条消息；不同消息可以并发处理。

## 6. 过滤器、重入保护与连续对话

### 指令过滤器

继承 `CommandBeforeFilterAttribute` / `CommandAfterFilterAttribute`，贴在 `[Command]` 方法或 `[CommandGroup]` 类上即可启用（opt-in），无须注册：

```csharp
public sealed class CooldownAttribute : CommandBeforeFilterAttribute
{
    public override ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e, CommandFilterContext cmd, CancellationToken ct) =>
        ValueTask.FromResult(true); // 返回 false 短路，handler 不执行
}
```

- 执行阶段为 before-filter → handler → after-filter。after-filter 会收到是否被短路以及 handler 的普通异常。
- 过滤器按 `Order` 升序执行；`Order` 相同时类级过滤器先于方法级。类级过滤器实例由组内全部指令共享，有状态的过滤器必须线程安全。
- 过滤器在扫描或动态注册时固定，之后不再改变；停用再启用不会重建过滤器，其状态延续。
- 过滤器的普通异常会记录日志并按放行处理。只有 `CancellationToken` 等于本次传入 token 且该 token 已取消的 `OperationCanceledException` 被视为 Sora 取消，会立即中断后续执行。
- `CommandFilterContext.DeclaringType` 是方法声明类型；继承指令的声明类型可能与归属 Type 不同。

### 重入保护

`PreventReentry = true` 时，同一方法、连接、发送者、群和来源类型的调用未结束前，再次触发会被跳过，可选回复 `ReentryMessage`。保护范围覆盖 handler 等待后续消息的时间。

### 连续对话

在 handler 中等待同一来源的下一条消息：

```csharp
MessageReceivedEvent? next = await e.WaitForNextMessageAsync(TimeSpan.FromSeconds(30), ct);
MessageReceivedEvent? answer = await e.WaitForNextMessageAsync(["^yes$", "^no$"], MatchType.Regex, TimeSpan.FromSeconds(30));
MessageReceivedEvent? number = await e.WaitForNextMessageAsync(m => int.TryParse(m.Message.Body.GetText(), out _));
```

返回 null 表示超时或取消。被 waiter 消费的消息不经过事件过滤器和指令匹配。handler 等待期间仍属于在途调用。

## 7. 启停与查询

| API | 说明 |
|-----|------|
| `AssemblyRegistration.SetEnabled(bool)` | 设置 Assembly 局部启用状态 |
| `AssemblyRegistration.SetPriority(int)` | 设置程序集优先级 |
| `AssemblyRegistration.SetTypeEnabled(type, bool)` | 设置归属 Type 的局部启用状态；对外部或未知 Type 返回 `false` |
| `AssemblyRegistration.IsEnabled` / `Priority` / `IsRegistered` | 当前状态 |
| `CommandManager.GetAssemblyStates()` | 按执行顺序返回全部 Assembly（包括已停用的）的不可变快照，含各 Type 的局部 / 有效启用状态与指令数 |

- setter 在注册有效时返回 `true`（值未变化也返回 `true`，且不重建执行顺序）；注册已注销时返回 `false`。
- 停用只影响是否参与执行，不会删除注册、单例实例、过滤器状态或动态指令的 ID。
- 重新启用 Assembly 时保留各 Type 自己的局部状态：之前单独停用的 Type 仍保持停用。
- Assembly / Type 状态在首次有指令归属时创建，并保留到注销；动态指令的增删不会重置它们。

## 8. 注销与插件卸载

```csharp
await registration.UnregisterAsync(timeoutToken);
```

`UnregisterAsync` 的行为：

1. 在第一个 await 之前同步完成摘除：移除该注册的全部指令（扫描与动态）、Type 状态、扫描记录和单例实例引用。之后不会再开始新的调用；已在处理的消息会跳过该 Assembly 尚未执行的指令。
2. 然后等待在途调用结束，包括 handler 中进行中的连续对话。
3. `ct` 只取消等待，摘除不会回滚。handler 永不返回时可以用它设置超时。
4. 不会调用实例的 `Dispose` / `DisposeAsync`，实例的生命周期由宿主负责。
5. 重复调用会等待同一次排空。注销后的旧句柄：setter 返回 `false`，扫描、预注册实例和动态注册抛 `InvalidOperationException`。再次 `RegisterAssembly` 同一 Assembly 会得到新的句柄和全新状态。

注销完成只保证 manager 内不再有该 Assembly 的注册与在途调用。handler 返回后仍在运行的后台任务、自行注册的 waiter、事件订阅和定时器由宿主负责。另外，在注销之前就已开始处理的其他消息仍持有旧的执行顺序表，可能让 AssemblyLoadContext 稍晚才被回收。

### 卸载顺序

注销只按归属移除，不追踪依赖：注销父 Assembly（例如被继承的 `Shared.dll`）不会影响子插件中继承自它的指令，这些指令继续执行。卸载插件时需要按依赖从叶到根依次处理：

```text
1. await childRegistration.UnregisterAsync(ct)
2. await parentRegistration.UnregisterAsync(ct)
3. 释放宿主自己持有的 Assembly / Type / 实例 / 委托引用
4. loadContext.Unload()
5. 用 WeakReference(loadContext) 循环执行 GC 并设置超时，确认已回收
```

跳过注销直接卸载时的结果：

| 操作 | 结果 |
|------|------|
| 对 collectible ALC 调用 `Unload()`，但未注销 | manager 仍引用其中的 Type、方法、实例与委托，ALC 永远不会被回收，指令照常执行（内存泄漏） |
| 删除插件 DLL 文件 | 使用 `LoadFromAssemblyPath` 加载时 Windows 会锁定文件，删除失败；从流或字节加载时删除成功，但已加载的代码仍在内存中运行 |
| 删除插件依赖的、尚未加载的 DLL | 首次用到时抛 `FileNotFoundException`；在 handler 中发生时按普通指令失败记录日志并隔离 |
| 插件位于默认 ALC | 无法卸载，只能注销指令 |

## 9. 3.1 → 3.2 迁移

3.1 的 `[Command]` 使用可空的 `MessageSourceType?` 表示来源，而 C# 特性参数不支持可空类型，因此无法在特性上赋值。3.2 改为由指令与事件过滤器共用的来源范围 `MessageSourceScope`：

| 3.1 | 3.2 |
|-----|-----|
| `[Command(SourceType = ...)]`（无法编译） | `[Command(SourceScope = MessageSourceScope.Group)]`，可组合，如 `Friend \| Temp` |
| `RegisterDynamicCommand(..., sourceType: null)` | 省略参数，或 `sourceScope: MessageSourceScope.Any` |
| `RegisterDynamicCommand(..., sourceType: MessageSourceType.Group)` | `sourceScope: MessageSourceScope.Group` |
| `IEventPreFilter` / `IEventPostFilter` 的 `MessageSourceType[]? SourceTypes`（`null` = 全部） | `MessageSourceScope SourceScope`（默认 `Any`） |
| `PeerPinChangedEvent.MessageScene`（`string`） | `PeerPinChangedEvent.SourceType`（`MessageSourceType`） |

事件过滤器的来源判断同时调整：

- `SourceScope` 为 `Any` 时不按来源限制，所有事件都会进入过滤器（与 3.1 的 `null` 相同）。
- 其他范围只匹配来源在范围内的**带来源事件**（实现 `ISourcedEvent` 的事件：`MessageReceivedEvent`、`MessageDeletedEvent`、`NudgeEvent`、`FileUploadEvent`、`PeerPinChangedEvent`）。3.1 只判断 `MessageReceivedEvent`，其他事件一律跳过；3.2 中撤回、戳一戳、文件上传和置顶变化事件也会按各自的 `SourceType` 参与判断。
- 不带来源的事件（如入群、好友请求）在非 `Any` 范围下仍然跳过该过滤器。

## 相关文档

- [← 返回 README](../README.md)
- [V2 → V3 迁移指南](MIGRATION-V2-TO-V3.md)
- [Adapter 开发指南](ADAPTER-DEVELOPMENT.md) — 事件管线概览
