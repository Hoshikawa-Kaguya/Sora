# 日志配置

Sora 通过 Microsoft.Extensions.Logging 的 `ILogger` / `ILoggerFactory` 输出日志，默认后端为 Serilog。`Sora.Entities.SoraLogger` 为所有服务、适配器和命令组件提供共享日志工厂。

## 初始化顺序

将日志配置放在应用入口最先执行，在创建适配器、命令组件或调用其他会获取 Logger 的 Sora 方法之前完成。显式配置一旦成功就固定工厂，即使尚未创建 Logger 或服务，再次配置也会抛出 `InvalidOperationException`。

未显式配置时，首次获取 Logger 自动创建最低级别为 `Information` 的 Serilog Console 工厂。此后也不能重新配置。多个服务共享工厂，服务构造失败、停止或释放均不重新开放配置。

初始化构建失败会抛出 `InvalidOperationException`，`InnerException` 保留原始原因，且不会发布失败结果。调用者可修正配置后重试。空参数使用 `ArgumentNullException`；普通日志写入的后端异常保持后端本身的行为。

## 默认配置与定制

```csharp
using Microsoft.Extensions.Logging;
using Sora;
using Sora.Adapter.Milky;
using Sora.Entities;

SoraLogger.Configure(SoraLogger.CreateDefaultLoggerConfiguration(LogLevel.Debug));

await using SoraService service = SoraServiceFactory.Instance.CreateMilkyService(new MilkyConfig
{
    Host = "127.0.0.1",
    Port = 3000
});
```

`CreateDefaultLoggerConfiguration` 每次返回新的 `Serilog.LoggerConfiguration`，可继续添加 sink 和过滤规则；调用它不会初始化或锁定全局日志。默认配置包含 JsonNet 解构及 Console 模板：

```text
[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}
```

也可以直接提供完整的 Serilog 配置：

```csharp
using Serilog;
using Sora.Entities;

SoraLogger.Configure(new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.Console());
```

通过配置对象创建的工厂由 Sora 按进程共享，单个服务释放不会关闭它。需要控制缓冲日志的刷新或关闭时，使用自行持有的工厂。

## 外部工厂与资源所有权

`Configure(ILoggerFactory)` 接受自定义工厂，保留其原有过滤规则和输出目标。工厂由调用者拥有；Sora 不修改其配置，也不释放它。工厂必须在所有使用它的服务和日志器完成工作后再释放。

```csharp
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Sora;
using Sora.Adapter.Milky;
using Sora.Entities;

using ILoggerFactory factory = new SerilogLoggerFactory(
    SoraLogger.CreateDefaultLoggerConfiguration(LogLevel.Debug).CreateLogger(),
    dispose: true);
SoraLogger.Configure(factory);

await using SoraService service = SoraServiceFactory.Instance.CreateMilkyService(new MilkyConfig());
// 应用在此运行服务；作用域退出时先释放 service，再关闭 factory。
```

其他 MEL 兼容工厂也使用同一个 `Configure(ILoggerFactory)` 入口。配置锁定约束的是 Sora 配置 API；调用者直接更改自己后端的运行时设置属于该后端的契约。

## 显式静默

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Sora.Entities;

SoraLogger.Configure(NullLoggerFactory.Instance);
```

显式静默也是一次成功配置，后续获取 Logger 和创建服务继续使用该工厂。

## 日志级别

| 级别 | 内容 |
| --- | --- |
| Trace | 协议原始 JSON、HTTP 请求和响应体 |
| Debug | 事件分发、指令匹配、连接细节和 API 调用 |
| Information | 服务、适配器和命令注册的关键生命周期事件 |
| Warning | 可以恢复的问题 |
| Error | API、处理器或指令执行失败 |

## 项目归属

- `Sora.Core`：共享基础类型。
- `Sora.Entities`：SoraLogger、MEL 日志抽象、默认 Serilog 工厂及其 Console/JsonNet 依赖。
- `Sora`、`Sora.Command` 和两个适配器：通过 SoraLogger 获取日志器，复用共享工厂。

## 测试日志

测试脚本的 `-LogLevel` 通过 `SORA_TEST_LOG_LEVEL_OVERRIDE` 传给测试进程。测试启动代码读取一次该变量，并在首次使用 Sora 前显式配置日志；默认级别为 Debug。变量读取仅位于测试项目，应用日志仍由应用配置代码决定。详见[测试说明](TESTING.md)。

[返回 README](../README.md)

OneBot11 的 Debug 连接日志记录完整 URL，包括配置的 query access token；这是连接诊断的既定输出。Milky HTTP 失败保留协议响应的错误消息，调用者可按返回码和消息定位拒绝原因。
