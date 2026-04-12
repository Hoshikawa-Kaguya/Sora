# 已移除的测试用例说明

本文档记录了在双机器人自动化重构中被移除的功能测试，以及移除的原因。

## 背景

Sora 框架的功能测试从 v2 开始采用双机器人端到端架构：
- **Primary Bot**: 主测试执行者，发送消息、调用 API、注册命令处理器
- **Secondary Bot**: 模拟人工操作，触发事件、验证数据

以下事件测试因无法在双机器人架构下安全自动化而被移除。

---

## 移除的测试列表

### FriendAdded / FriendRequest

| 原测试名 | 协议 |
|----------|------|
| `Event_FriendAdded_Interactive` | Milky, OneBot11 |
| `Event_FriendRequest_Interactive` | Milky, OneBot11 |

**原始行为**: 在群内发送提示消息，等待人工添加机器人为好友 / 发送好友请求。

**无法自动化的原因**:
- 两个测试 Bot 在测试环境中通常已是好友关系
- 触发好友添加/请求事件需要先删除好友再重新添加
- 删除好友操作是**不可逆的破坏性操作**，可能导致：
  - 后续私聊相关测试失败（好友关系未及时恢复）
  - 好友验证流程中的不确定等待时间
  - 部分协议端不支持程序化删除好友
  - 账号被风控
- 好友关系恢复不可靠：删除后重新添加可能需要对方确认，造成测试死锁

### MemberJoined / MemberLeft

| 原测试名 | 协议 |
|----------|------|
| `Event_MemberJoined_Interactive` | Milky, OneBot11 |
| `Event_MemberLeft_Interactive` | Milky, OneBot11 |

**原始行为**: 在群内发送提示消息，等待人工让用户加入/退出测试群。

**无法自动化的原因**:
- 触发加入事件需要 Secondary Bot 先离开群，再重新加入
- **没有 "加入群" API** — 群加入只能通过客户端操作或群邀请
- 踢出 Secondary Bot (`SetGroupKickAsync`) 会导致：
  - 所有依赖 Secondary Bot 在群内的后续测试失败
  - 群验证设置可能阻止自动重新加入
  - 踢出记录可能影响后续测试的群成员状态
  - 账号被风控
- 测试执行顺序不确定（xUnit 并行），无法保证 MemberLeft 在其他群相关测试之后运行

### GroupJoinRequest

| 原测试名 | 协议 |
|----------|------|
| `Event_GroupJoinRequest_Interactive` | Milky, OneBot11 |

**原始行为**: 在群内发送提示消息，等待人工让用户申请加入测试群。

**无法自动化的原因**:
- 需要测试群设置为 "需要审批" 模式
- Secondary Bot 需要先退出群，然后发送加群申请
- 退出群后没有可靠的 "申请加群" API
- 群审批模式的切换可能影响其他测试
- 与 MemberJoined/Left 存在相同的破坏性问题
- 容易触发账号风控

### GroupInvitation

| 原测试名 | 协议 |
|----------|------|
| `Event_GroupInvitation_Interactive` | Milky, OneBot11 |

**原始行为**: 在群内发送提示消息，等待人工邀请机器人加入一个群。

**无法自动化的原因**:
- 需要一个 **额外的群** 作为邀请目标
- Secondary Bot 需要是该额外群的管理员才能发送邀请
- 测试环境配置复杂度显著增加（需要管理两个群的状态）
- 邀请后 Primary Bot 加入新群会产生副作用，需要在测试后退出
- 跨群操作的清理逻辑复杂且不可靠
- 容易触发账号风控

---

## 替代覆盖方案

虽然这些事件的端到端测试被移除，相关代码路径仍通过以下方式覆盖：

1. **单元测试**: 事件转换器（EventConverter）的单元测试验证 JSON → Event 对象的解析逻辑正确
2. **保留的自动化事件测试**: `Event_GroupNudge_FromSecondary`、`Event_FileUpload_FromSecondary`、`Event_GroupReaction_FromSecondary`、`Event_GroupAdminChanged_Automated` 等验证了事件接收和分发的核心链路
3. **代码覆盖率报告**: 通过覆盖率分析确认事件处理代码路径的覆盖情况

---

## 相关文档

- [← 返回 README](../README.md)
- [测试说明](TESTING.md) — 完整测试架构与环境变量
- [功能测试目录](FUNCTIONAL-TEST-CATALOG.md) — 所有 E2E 测试的完整清单
