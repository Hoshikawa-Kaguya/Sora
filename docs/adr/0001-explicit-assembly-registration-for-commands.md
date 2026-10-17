# 指令以程序集注册句柄登记，归属必须显式声明

Sora.Command 需要支持外部插件 DLL 按 Assembly / Type 启停、按 Assembly 优先级分组执行以及注销，这要求每条命令都有唯一、可靠的归属。原有入口（`ScanAssembly`、`ScanType`、`RegisterDynamicCommand` 等）只能从方法或委托代码所在的 DLL 推断归属：宿主包装的 lambda、共享库工厂生成的委托、`Expression.Compile` 生成的委托都会被判错或无法判断。因此我们以破坏性变更删除这些入口：所有注册必须先通过 `CommandManager.RegisterAssembly` 取得 `AssemblyRegistration` 句柄；扫描命令归属扫描入口 Type，动态命令必须显式传入 `ownerType`，且它必须是该 Assembly 自己定义的类型（引用相等）。

## Considered Options

- **保留旧入口，按 `handler.Method.DeclaringType` 推断归属并允许可选 owner 覆盖**：被拒绝。推断在宿主包装、共享库工厂场景下静默判错，且一处疏忽就会让插件命令脱离启停与注销管理。
- **允许 owner 为本 Assembly 引用的父 Assembly 中的类型**：被拒绝。需要按程序集名称匹配，违背“不同 AssemblyLoadContext 中的同名程序集分别管理”的身份规则；同一个父类型会同时出现在多个注册中，造成按 Type 启停与实例查询的歧义；还需维护 Sora 与运行时程序集的排除名单。严格相等天然排除了这些类型。
- **由句柄本身实现插件可见的注册接口**：被拒绝。插件可经类型转换取得启停、调优先级与注销能力；改为通过 `AssemblyRegistration.Registrar` 暴露由内部类实现的 `ICommandRegistrar`。这只是 API 约定，不是安全边界。

## Consequences

- 所有使用者必须迁移注册代码（见 `docs/MIGRATION-V2-TO-V3.md`）；多个 Assembly 即使优先级都为 0，也从全局按命令优先级混排变为按 Assembly 分组执行。
- 归属只表示管理范围，不表示依赖：注销父 Assembly 不影响子插件中继承自它的命令，插件卸载顺序（子 → 父）与 AssemblyLoadContext 回收由宿主负责。
- 冻结的 OB11 示例与测试仍使用旧入口，不随之修改，它们不参与构建。
