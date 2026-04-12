# 日志配置

Sora 使用 [Microsoft.Extensions.Logging](https://learn.microsoft.com/zh-cn/dotnet/core/extensions/logging) 抽象（`ILogger` / `ILoggerFactory`），默认后端为 [Serilog](https://serilog.net/)。

## 默认行为

如果未配置日志，框架会在首次创建 `SoraService` 时自动创建预配置的 **Serilog Console Logger**：

```
[14:23:05 INF] Sora.SoraService: SoraService abc123 starting (adapter: Milky)
[14:23:05 INF] Sora.Adapter.Milky.MilkyAdapter: Milky adapter starting (transport: WebSocket, host: 127.0.0.1:3000)
[14:23:05 INF] Sora.Adapter.Milky.MilkyAdapter: Milky adapter connected (transport: WebSocket)
[14:23:05 INF] Sora.SoraService: SoraService abc123 started
```

## 配置方式

### 通过 Config 设置 LogLevel

通过配置对象设置初始最低LogLevel：

```csharp
SoraService service = SoraServiceFactory.Instance.CreateMilkyService(new MilkyConfig
{
    Host = "127.0.0.1",
    Port = 3000,
    MinimumLogLevel = LogLevel.Debug,  // 默认: Information
});
```

> **注意：** `MinimumLogLevel` 仅影响框架默认的 Serilog 后端。如果提供了自定义 `LoggerFactory`，请通过自己的`LoggerFactory`配置LogLevel。

### 自定义 Logger Factory

> **重要：** 由于Sora目前使用的是静态（静态类）日志实现，Logger Factory 在首次创建 `SoraService` 后即被锁定。如果未提供自定义工厂，框架会创建默认的 Serilog 控制台日志器。后续服务创建会忽略 `LoggerFactory` 属性。

通过配置对象传入自定义的 `ILoggerFactory`。

```csharp
using Serilog;
using Serilog.Extensions.Logging;

// 使用自定义设置配置 Serilog
Serilog.Core.Logger serilogLogger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/bot.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

SoraService service = SoraServiceFactory.Instance.CreateMilkyService(new MilkyConfig
{
    Host = "127.0.0.1",
    Port = 3000,
    LoggerFactory = new SerilogLoggerFactory(serilogLogger, dispose: true),
});
```

### 禁用日志

```csharp
using Microsoft.Extensions.Logging.Abstractions;

SoraService service = SoraServiceFactory.Instance.CreateMilkyService(new MilkyConfig
{
    Host = "127.0.0.1",
    Port = 3000,
    LoggerFactory = NullLoggerFactory.Instance,
});
```

### 使用任意 MEL 兼容的提供程序

框架使用标准的 `ILogger` / `ILoggerFactory`，因此可以使用任意提供程序：

```csharp
using Microsoft.Extensions.Logging;

ILoggerFactory factory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();        // 内置控制台
    builder.AddEventLog();       // Windows 事件日志
    builder.SetMinimumLevel(LogLevel.Debug);
});

SoraService service = SoraServiceFactory.Instance.CreateMilkyService(new MilkyConfig
{
    Host = "127.0.0.1",
    Port = 3000,
    LoggerFactory = factory,
});
```

## 不同 LogLevel 之间的区分设计原则

| 级别 | 内容 | 示例 |
|------|------|------|
| **Trace** | 协议原始数据 | 原始 JSON 载荷、HTTP 请求/响应体 |
| **Debug** | 内部运行细节 | 事件分发、指令匹配、WS/SSE 连接详情、API 调用 |
| **Information** | 关键生命周期事件 | 服务启动/停止、适配器连接/断开、指令扫描结果 |
| **Warning** | 可恢复的问题 | 缺少无参构造函数回退、API 调用超时 |
| **Error** | 处理器/指令失败 | 事件处理器中的未处理异常、指令处理器异常、事件解析错误 |

## 架构

```
SoraLogger (静态, Sora.Entities)
    ├── ILoggerFactory (MEL 抽象)
    │       └── Serilog (默认后端, 位于 Sora 门面层)
    ├── InternalInitFactory()  — 首次创建服务时由框架调用
    └── IsSealed               — 首次创建服务后锁定（后续配置被忽略）

各组件通过以下方式创建日志器:
    ILogger _logger = SoraLogger.CreateLogger<MyClass>();
```

- **`Sora.Core`** — 无日志（纯类型库）
- **`Sora.Entities`** — `Microsoft.Extensions.Logging.Abstractions`（仅接口）
- **`Sora`（门面层）** — `Serilog` + `Serilog.Extensions.Logging` + `Serilog.Sinks.Console`
- **适配器** — 通过传递依赖使用 `ILogger`，无额外包引用

---

## 相关文档

- [← 返回 README](../README.md)
- [测试说明](TESTING.md) — 测试中的日志级别控制（`SORA_LOG_LEVEL_OVERRIDE`）
