# Sora 测试说明

## 概述

Sora 框架采用**双机器人架构**进行端到端测试。两个独立的 QQ 机器人账号（Primary Bot 和 Secondary Bot）互相协作，完全自动化测试流程，无需人工介入。

## 测试架构

### 双机器人角色分工

| 角色 | 职责 |
|------|------|
| **Primary Bot** | 主测试执行者。发送大部分消息、调用 API、注册命令处理器。绝大多数测试的发起方。 |
| **Secondary Bot** | 辅助验证者。触发事件、发送触发消息、在群里交互；同时在 Primary 执行操作时充当事件监听方（协议端通常不会将操作者自身产生的事件回传给操作者）。 |

Secondary Bot 仅在以下场景使用：
1. **触发事件** — Primary 监听来自"外部用户"的事件（消息、戳一戳、文件上传、Reaction 等）
2. **事件监听** — Primary 执行操作（撤回消息、设置管理员、禁言等）时，由 Secondary Service 监听事件（协议端通常不向操作者自身投递事件通知）
3. **私聊对象** — Primary 的私聊 API 测试需要目标用户
4. **数据验证** — 验证 Primary 发送的消息被正确处理
5. **命令触发** — 向群/私聊发送消息以触发 Primary 注册的命令
6. **连续对话** — 在 Primary 的命令等待后续消息时发送跟进

### 测试分类

| 分类 | 定位 | 范围 | 网络需求 |
|------|------|------|----------|
| **Unit** | 框架内部逻辑 | Converter、Entity、Command 解析、Config 等 | 无 |
| **Functional** | **端到端测试** | API / 事件 / 命令（含连续对话） / 消息类型（[完整清单](FUNCTIONAL-TEST-CATALOG.md)） | 需要协议端 |

### 测试可靠性要求

- **单元测试不允许失败。** 单元测试在隔离环境中运行，无外部依赖。失败一定意味着代码缺陷，必须立即修复。
- **功能测试允许失败，但必须有明确的失败原因。** 功能测试是端到端测试，验证从协议端到框架端的完整链路可靠性。因协议端不可用、网络问题或不支持的功能导致的失败是可接受的——但失败消息必须清楚地标明原因。测试绝不能在出错时静默通过或被悄悄跳过。

### 功能测试设计原则

- **`Assert.SkipWhen()` 仅用于环境前置条件**（缺少环境变量、未配置 Secondary Bot 等）。协议端未能投递预期事件或返回异常结果时，测试必须**失败**而非跳过。跳过会掩盖回归问题，造成测试套件健康的假象。
- **超时 = 失败，而非跳过。** 通过 `TaskCompletionSource` 等待事件时，超时意味着协议端未投递预期事件，这是测试失败，不是预期行为。
- **正确的事件超时模式：**
  ```csharp
  await Task.WhenAny(tcs.Task, Task.Delay(timeout));
  Assert.True(tcs.Task.IsCompletedSuccessfully, "Event X 未在超时内收到");
  EventType evt = tcs.Task.Result;  // 安全——Assert 已验证完成状态
  ```
  绝不能在未断言 `IsCompletedSuccessfully` 的情况下访问 `.Result`——超时时会无限挂起。
- **禁止假通过。** 不得使用 `if (completed) { assert } else { 仅日志 }` 模式——它在超时时静默通过，完全跳过了验证。

### 网络延迟容忍

所有跨机器人交互遵循以下延迟策略，避免网络延迟导致测试误报：

| 场景 | 策略 |
|------|------|
| 事件监听 | `TaskCompletionSource` + `Task.WhenAny(tcs.Task, Task.Delay(timeout))`，timeout ≥ 5 秒 |
| API 操作后验证 | 操作后等待 ≥ 1 秒再验证 |
| 命令触发 | Secondary 发送消息后 Primary 等待 ≥ 3 秒 |
| 媒体消息 | 发送/接收超时 ≥ 10 秒 |

## 文件结构

```
tests/
├── Sora.Tests/
│   ├── Functional/
│   │   ├── Milky/
│   │   │   ├── MilkyTestFixture.cs    ← 双机器人连接管理
│   │   │   ├── ApiTests.cs            ← API 端到端测试
│   │   │   ├── EventTests.cs          ← 事件端到端测试
│   │   │   ├── MessageTypeTests.cs    ← 消息段类型测试
│   │   │   └── CommandTests.cs        ← 命令与对话测试
│   │   └── OneBot11/
│   │       ├── OneBot11TestFixture.cs
│   │       ├── ApiTests.cs
│   │       ├── EventTests.cs
│   │       ├── MessageTypeTests.cs
│   │       └── CommandTests.cs
│   ├── Unit/
│   │   ├── UnitTestFixtures.cs            ← 单元测试 Collection Fixtures
│   │   ├── Adapters/
│   │   │   └── ConfigTests.cs
│   │   ├── Command/
│   │   │   ├── CommandMatcherTests.cs
│   │   │   └── CommandScanningTests.cs
│   │   ├── Core/
│   │   │   ├── IdTypeTests.cs
│   │   │   └── EntityTests.cs
│   │   ├── Entities/
│   │   │   ├── SoraLoggerTests.cs
│   │   │   ├── SegmentOperatorTests.cs
│   │   │   ├── MessageWaiterTests.cs
│   │   │   ├── MessageBodyTests.cs
│   │   │   ├── EventDispatcherTests.cs
│   │   │   └── BotConnectionTests.cs
│   │   ├── Milky/
│   │   │   ├── SseParsingTests.cs
│   │   │   ├── MessageConverterTests.cs
│   │   │   ├── EventConverterTests.cs
│   │   │   └── EntityConverterTests.cs
│   │   └── OneBot11/
│   │       ├── MessageConverterTests.cs
│   │       └── EventConverterTests.cs
│   ├── Helpers/
│   │   ├── TestConfig.cs              ← 环境变量配置
│   │   ├── TestOutputSink.cs          ← 日志转发到 xUnit 输出
│   │   └── TestTimingStore.cs         ← 计时统计
│   └── GlobalUsings.cs
├── scripts/
│   ├── Run-Tests.ps1                  ← 主测试脚本
│   └── Run-Tests-Local.ps1            ← 本地便捷脚本
├── test.runsettings                   ← CI 用（无环境变量）
└── test.local.runsettings             ← IDE 用（含双机器人配置）
```

## 环境变量

### 控制开关

| 变量 | 说明 |
|------|------|
| `SORA_TEST_FUNCTIONAL` | `true` 时运行功能测试，否则全部跳过 |
| `SORA_LOG_LEVEL_OVERRIDE` | 框架日志级别（Trace/Debug/Info/Warn/Error/Fatal/None） |
| `SORA_TEST_RESULTS_DIR` | TRX 测试结果输出目录（由 Run-Tests.ps1 自动设置） |

### 协议端配置（两端共用 PORT/TOKEN/PREFIX，仅 HOST 不同）

| 变量 | 说明 |
|------|------|
| `SORA_TEST_MILKY_PRIMARY_HOST` | Milky Primary Bot 地址 |
| `SORA_TEST_MILKY_SECONDARY_HOST` | Milky Secondary Bot 地址 |
| `SORA_TEST_MILKY_PORT` | Milky 端口（默认 3010） |
| `SORA_TEST_MILKY_TOKEN` | Milky 访问令牌 |
| `SORA_TEST_MILKY_PREFIX` | Milky URL 前缀 |
| `SORA_TEST_OB11_PRIMARY_HOST` | OB11 Primary Bot 地址 |
| `SORA_TEST_OB11_SECONDARY_HOST` | OB11 Secondary Bot 地址 |
| `SORA_TEST_OB11_PORT` | OB11 端口（默认 3001） |
| `SORA_TEST_OB11_TOKEN` | OB11 访问令牌 |

### 测试目标与资源

| 变量 | 说明 |
|------|------|
| `SORA_TEST_GROUP_ID` | 测试群号（功能测试必须） |
| `SORA_TEST_PRIMARY_BOT_AVATAR` | Primary Bot 头像图片路径 |
| `SORA_TEST_SECONDARY_BOT_AVATAR` | Secondary Bot 头像图片路径 |
| `SORA_TEST_GROUP_AVATAR` | 群头像图片路径 |
| `SORA_TEST_AUDIO_FILE` | 音频文件路径（本地读取后 base64 编码传递给协议端） |
| `SORA_TEST_VIDEO_FILE` | 视频文件路径（本地读取后 base64 编码传递给协议端） |

### 跳过逻辑

- 仅配置 PRIMARY → 运行单机器人测试，跳过需要双机器人的测试
- 同时配置 PRIMARY + SECONDARY → 运行所有测试
- 未配置任何 HOST → 跳过该协议的所有功能测试

## Run-Tests.ps1 参数

### 基础参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `-Category` | Unit/Functional/All | All | 测试类别 |
| `-Filter` | string | (空) | 额外过滤，追加到类别过滤器 |
| `-Configuration` | Debug/Release | Release | 构建配置 |
| `-ResultsDir` | string | `$SolutionRoot/TestResults` | TRX 输出目录 |
| `-NoBuild` | switch | false | 跳过构建 |
| `-LogLevel` | Trace~None | Debug | 日志级别 |
| `-WaitDebugger` | switch | false | 等待调试器附加 |
| `-Coverage` | switch | false | 收集代码覆盖率并生成 HTML 报告 |
| `-EnableReport` | switch | false | 发送测试报告到群（需 `-MilkyPrimaryHost`） |

### 协议端参数

| 参数 | 说明 |
|------|------|
| `-Ob11PrimaryHost` | OB11 Primary Bot 地址 |
| `-Ob11SecondaryHost` | OB11 Secondary Bot 地址 |
| `-Ob11Port` | OB11 端口（默认 3001） |
| `-Ob11Token` | OB11 令牌 |
| `-MilkyPrimaryHost` | Milky Primary Bot 地址 |
| `-MilkySecondaryHost` | Milky Secondary Bot 地址 |
| `-MilkyPort` | Milky 端口（默认 3010） |
| `-MilkyToken` | Milky 令牌 |
| `-MilkyPrefix` | Milky URL 前缀 |
| `-GroupId` | 测试群号 |
| `-PrimaryBotAvatar` | Primary 头像路径 |
| `-SecondaryBotAvatar` | Secondary 头像路径 |
| `-GroupAvatarPath` | 群头像路径 |

## 使用示例

### 仅运行单元测试
```powershell
.\tests\scripts\Run-Tests.ps1 -Category Unit
```

### 双机器人功能测试（Milky）
```powershell
.\tests\scripts\Run-Tests.ps1 -Category Functional `
    -MilkyPrimaryHost <primary-host> -MilkySecondaryHost <secondary-host> `
    -MilkyToken <token> -MilkyPrefix <prefix> -GroupId <group-id>
```

### 完整测试 + 代码覆盖率
```powershell
.\tests\scripts\Run-Tests.ps1 -Category All -Coverage `
    -MilkyPrimaryHost <primary-host> -MilkySecondaryHost <secondary-host> `
    -MilkyToken <token> -MilkyPrefix <prefix> `
    -Ob11PrimaryHost <primary-host> -Ob11SecondaryHost <secondary-host> `
    -Ob11Token <token> -GroupId <group-id>
```
> 测试完成后在 `TestResults/CoverageReport/` 生成 HTML 覆盖率报告。

### 运行单个测试
```powershell
.\tests\scripts\Run-Tests.ps1 -Category Functional `
    -Filter "FullyQualifiedName~MessageTypeTests.SendReceive_Text" `
    -MilkyPrimaryHost <primary-host> -MilkySecondaryHost <secondary-host> `
    -MilkyToken <token> -MilkyPrefix <prefix> -GroupId <group-id>
```

### 运行某个测试类
```powershell
.\tests\scripts\Run-Tests.ps1 -Filter "FullyQualifiedName~Milky.CommandTests" `
    -MilkyPrimaryHost <primary-host> -MilkySecondaryHost <secondary-host> `
    -MilkyToken <token> -MilkyPrefix <prefix> -GroupId <group-id>
```

### 完整测试 + 群报告
```powershell
.\tests\scripts\Run-Tests.ps1 -EnableReport `
    -MilkyPrimaryHost <primary-host> -MilkySecondaryHost <secondary-host> `
    -MilkyToken <token> -MilkyPrefix <prefix> -GroupId <group-id>
```

### 调试模式
```powershell
.\tests\scripts\Run-Tests.ps1 -Category Unit -WaitDebugger -Configuration Debug
```

## IDE 中运行功能测试

### Rider / Visual Studio

使用 `.runsettings` 文件注入环境变量，无需脚本即可在 IDE 中直接运行任何测试：

1. 编辑本地配置（已在 `.gitignore` 中排除）：
   ```
   tests/test.local.runsettings
   ```
   填入双机器人的 HOST、PORT、TOKEN 等环境变量。

2. **Rider**: Settings → Build, Execution, Deployment → Unit Testing → Test Runner → 勾选 Run Settings 并选择 `tests/test.local.runsettings`

3. **Visual Studio**: Test → Configure Run Settings → Select Solution Wide runsettings File → 选择 `tests/test.local.runsettings`

4. 配置生效后，右键运行任意测试（包括单个方法）均可自动注入环境变量。

> **注意**：修改连接地址时，同时更新 `test.local.runsettings` 和 `Run-Tests-Local.ps1`。

## 代码覆盖率

使用 `-Coverage` 开关启用覆盖率收集：

```powershell
.\tests\scripts\Run-Tests.ps1 -Category All -Coverage ...
```

流程：
1. `coverlet.collector` 在测试运行时收集覆盖率数据（Cobertura XML 格式）
2. 测试完成后 `reportgenerator` 生成 HTML 报告
3. 报告输出到 `TestResults/CoverageReport/index.html`
4. 控制台输出覆盖率摘要

排除范围：测试项目 (`tests/`)、工具项目 (`tools/`)。

## 测试报告

启用 `-EnableReport` 后（需要 `-MilkyPrimaryHost` 和 `-GroupId`），测试完成时会：
1. 通过 Milky Primary Bot 向测试群发送文字报告
2. 创建时间戳命名的群文件夹
3. 上传 TRX 结果文件到该文件夹

## Filter 表达式

```powershell
# 按测试名
-Filter "FullyQualifiedName~SendReceive_Text"

# 按测试类
-Filter "FullyQualifiedName~Milky.MessageTypeTests"

# 多条件（OR）
-Filter "FullyQualifiedName~SetNickname|FullyQualifiedName~SetBio"
```

## 已移除的测试

部分无法自动化的事件测试已从代码中移除，详见 [REMOVED_TESTS.md](REMOVED_TESTS.md)。

完整的功能测试清单请参阅 [功能测试目录](FUNCTIONAL-TEST-CATALOG.md)。

---

## 相关文档

- [← 返回 README](../README.md)
- [本地测试配置](TESTING-LOCAL.md) — 本地环境快速配置参考
- [功能测试目录](FUNCTIONAL-TEST-CATALOG.md) — 所有 E2E 测试的完整清单
- [已移除测试](REMOVED_TESTS.md) — 无法自动化的测试及原因
- [日志配置](LOGGING.md) — 测试中的日志级别控制
