# 迁移到 Sora 3.0

> 本文分两部分：[V2 → V3](#v2--v3) 面向 2.x 用户；[V1 → V3 直接迁移](#v1--v3-直接迁移) 面向仍在使用 1.x、希望一步升级到 3.0 的用户。指令模块的完整说明见 [指令模块文档](COMMANDS.md)。

## V2 → V3

3.0 对 `HoshikawaKaguya.Sora.Command` 的注册 API 做了**破坏性变更**：为支持外部插件按程序集 / Type 启停、按程序集优先级分组执行以及注销，所有指令都必须通过程序集注册句柄登记，并显式声明归属。其他模块（事件、消息段、API、日志、Milky 适配器）没有因此改变。

旧 API 只能从方法或委托代码所在的 DLL 推断归属，宿主包装的 lambda、共享库工厂生成的委托都会被归错，或无法判断归属，导致插件指令脱离启停与注销管理；因此 3.0 改为显式注册，不保留推断式入口。

### API 对照

| 2.x | 3.0 |
|-----|-----|
| `service.Commands.ScanAssembly(assembly)` | `service.Commands.RegisterAssembly(assembly).ScanCommandGroups()` |
| `service.Commands.ScanAssemblies(a, b)` | 对每个程序集分别 `RegisterAssembly(...).ScanCommandGroups()` |
| `service.Commands.ScanType(type)` | `service.Commands.RegisterAssembly(type.Assembly).ScanType(type)` |
| `service.Commands.RegisterCommandInstance(instance)` | `registration.RegisterCommandInstance(instance)`（扫描前调用） |
| `service.Commands.RegisterDynamicCommand(handler, expressions, ...)` | `registration.Registrar.RegisterDynamicCommand(ownerType, handler, expressions, ...)` |
| `service.Commands.UnregisterDynamicCommand(id)` | `registration.Registrar.UnregisterDynamicCommand(id)` |
| `service.Commands.GetCommandInstance<T>()` | 不变 |
| — | `registration.SetEnabled` / `SetPriority` / `SetTypeEnabled` / `UnregisterAsync` |
| — | `service.Commands.TryGetAssembly` / `GetAssemblyStates` |

旧入口已删除，没有过渡期的兼容重载。

### 迁移示例

```csharp
// 2.x
service.Commands.RegisterCommandInstance(new MyCommands(config));
service.Commands.ScanAssembly(typeof(Program).Assembly);
service.Commands.RegisterDynamicCommand(async e => await ReplyAsync(e, "pong"), ["ping"]);

// 3.0
AssemblyRegistration commands = service.Commands.RegisterAssembly(typeof(Program).Assembly);
commands.RegisterCommandInstance(new MyCommands(config));
commands.ScanCommandGroups();
commands.Registrar.RegisterDynamicCommand(typeof(MyCommands), async e => await ReplyAsync(e, "pong"), ["ping"]);
```

`RegisterAssembly` 对已注册的程序集返回同一句柄，可以多次调用来取得它；也可以用 `TryGetAssembly` 查找。

### 动态指令的 `ownerType`

- 必填，位于第一个参数，其余参数与 2.x 完全相同。
- 必须是该程序集**自己定义的类型**。Sora、运行时或其他程序集的类型（包括被继承的基类）抛 `ArgumentException`。
- 它决定指令随哪个 Type 启停；不需要带 `[CommandGroup]`，也不需要被扫描过。只有一个程序集的普通机器人可以统一使用某个指令类或 `typeof(Program)`。

### 行为变化

| 行为 | 2.x | 3.0 |
|------|-----|-----|
| 执行顺序 | 全部指令按 `Priority` 全局排序 | 先按程序集优先级（默认 0，相同时按注册先后）分组，组内再按 `Priority` 排序。**多程序集即使优先级都为 0 也按组执行**，不再穿插 |
| 命令优先级相同 | 未定义的先后 | 按注册先后，扫描与动态指令混排 |
| 重复扫描同一 Type | 重复注册，指令会执行多次 | 跳过并记录 Warning |
| 扫描中途失败 | 已扫描的部分保留 | 本次调用不发布任何指令 |
| 扫描抽象类 / 开放泛型 `[CommandGroup]` | 尝试实例化并失败 | `ScanCommandGroups` 跳过；直接 `ScanType` 抛 `ArgumentException` |
| 跨程序集扫描 / 预注册实例 | 不检查 | 类型必须属于该程序集，否则抛 `ArgumentException` |
| 注销 | 只能按 ID 注销动态指令 | 另可 `UnregisterAsync` 整个程序集，并等待在途调用结束 |

只有单个程序集、且不需要启停与注销的项目，迁移后的执行顺序与 2.x 一致（同优先级的先后此前未定义）。

### 仍使用 OB11 的项目

`HoshikawaKaguya.Sora.Adapter.OneBot11` 的支持范围截至 Sora 2.2，其源码、测试和示例不随 3.0 更新，仍使用旧注册 API。使用 OB11 的项目应固定在 Sora 2.2，或迁移到 Milky。

### 检查清单

- [ ] 用 `RegisterAssembly(...)` 取得句柄，替换所有 `ScanAssembly` / `ScanAssemblies` / `ScanType`
- [ ] 把 `RegisterCommandInstance` 移到句柄上，并保证在扫描之前调用
- [ ] 为每个 `RegisterDynamicCommand` 补上本程序集的 `ownerType`，通过 `Registrar` 调用
- [ ] 多程序集项目：确认新的分组执行顺序，必要时设置程序集优先级
- [ ] 删除依赖“重复扫描会重复注册”的代码
- [ ] 编译并运行测试

---

## V1 → V3 直接迁移

除指令系统外，1.x 到 3.0 的迁移与 1.x 到 2.0 完全相同：请按 [从 1.x 迁移到 2.0](MIGRATION-V1-TO-V2.md) 处理环境、项目结构、服务创建、事件、消息段、API、消息等待、日志与连接管理，**跳过其中 §7.3 指令注册与 §7.4 动态指令**，改用下面的 3.0 写法。§7.1 / §7.2 的特性改名（`[CommandSeries]` → `[CommandGroup]`、`[SoraCommand]` → `[Command]` 等）与 §7.4.1 过滤器说明同样适用于 3.0。

### 指令注册

```csharp
// 1.x：自动扫描，无需手动调用

// 3.0：注册程序集后显式扫描
AssemblyRegistration commands = service.Commands.RegisterAssembly(typeof(Program).Assembly);
commands.ScanCommandGroups();
```

### 动态指令

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

// 3.0
commands.Registrar.RegisterDynamicCommand(
    typeof(Program),                  // 归属类型：必须定义在本程序集中
    async e =>
    {
        MessageBody reply = new("动态指令");
        await e.Api.SendGroupMessageAsync(e.Message.GroupId, reply);
    },
    ["测试"],
    matchType: MatchType.Full,
    sourceType: MessageSourceType.Group,
    preventReentry: true,             // 默认 true，防止同一用户重复触发
    reentryMessage: "执行中，请稍候");  // 可选：被阻止时回复用户
```

`BlockAfterMatch` 默认为 `true`，替代 1.x 中手动设置的 `e.IsContinueEventChain = false`。

### 检查清单补充

在 [1.x → 2.0 检查清单](MIGRATION-V1-TO-V2.md#13-迁移检查清单) 的基础上：

- [ ] 用 `service.Commands.RegisterAssembly(typeof(Program).Assembly).ScanCommandGroups()` 代替清单中的 `ScanAssembly`
- [ ] 动态指令通过 `registration.Registrar.RegisterDynamicCommand(ownerType, ...)` 注册

## 相关文档

- [← 返回 README](../README.md)
- [指令模块文档](COMMANDS.md)
- [从 1.x 迁移到 2.0](MIGRATION-V1-TO-V2.md)
