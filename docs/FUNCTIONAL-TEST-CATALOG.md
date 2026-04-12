# 功能性测试目录

本文档列出所有功能性（E2E）测试，按协议和测试类别分组。每个测试标注是否需要双账号、测试方式及被测内容。

> **图例**
>
> - 🟢 **单账号** — 仅需 Primary Bot 即可运行
> - 🔵 **双账号** — 需要 Primary Bot + Secondary Bot 协作
> - ⚠️ **特殊前置条件** — 除账号外还有额外环境要求

---

## Milky 协议

### ApiTests（64 项，均为 🟢 单账号）

API 测试通过 Primary Bot 直接调用协议 API 并验证返回结果，不涉及事件监听。

| # | 测试名 | 被测内容 | 测试方式 | 备注 |
|---|--------|----------|----------|------|
| 1 | `GetSelfInfo` | `IBotApi.GetSelfInfoAsync` | 调用 API，验证返回 UserId/Nickname 非空 | |
| 2 | `GetImplInfo` | `IBotApi.GetImplInfoAsync` | 调用 API，验证实现信息非空 | |
| 3 | `GetCookies` | `IBotApi.GetCookiesAsync` | 调用 API，验证返回状态 | |
| 4 | `GetCsrfToken` | `IBotApi.GetCsrfTokenAsync` | 调用 API，验证返回状态 | |
| 5 | `SendGroupMessage` | `IBotApi.SendGroupMessageAsync` | 发送群文本消息，验证 IsSuccess + MessageId | |
| 6 | `SendGroupMessage_WithFace` | 发送表情段消息 | 发送 Face Segment 群消息，验证成功 | |
| 7 | `SendGroupMessage_WithMention` | 发送 @mention 消息 | 发送 Mention Segment，验证成功 | |
| 8 | `SendGroupMessage_WithReply` | 发送回复消息 | 先发送一条消息取得 MessageId，再发送 Reply Segment | |
| 9 | `SendPrivateMessage` | `IBotApi.SendPrivateMessageAsync` | 向 Secondary Bot 发送私聊，验证成功 | |
| 10 | `SendAndRecallGroupMessage` | 发送 + 撤回群消息 | 先发送再调用 RecallGroupMessageAsync | |
| 11 | `SendAndRecallPrivateMessage` | 发送 + 撤回私聊消息 | 先发送再调用 RecallPrivateMessageAsync | |
| 12 | `GetMessage` | `IBotApi.GetMessageAsync` | 先发送消息，再按 MessageId 获取 | |
| 13 | `GetForwardMessages` | `IBotApi.GetForwardMessagesAsync` | 发送合并转发消息后获取内容 | |
| 14 | `GetHistoryMessages` | `IBotApi.GetHistoryMessagesAsync` | 获取群历史消息列表 | |
| 15 | `MarkMessageAsRead` | `IBotApi.MarkMessageAsReadAsync` | 发送消息后标记已读 | |
| 16 | `GetUserInfo` | `IBotApi.GetUserInfoAsync` | 获取指定用户信息 | |
| 17 | `GetUserProfile` | `IBotApi.GetUserProfileAsync` | 获取用户资料卡 | |
| 18 | `GetFriendInfo` | `IBotApi.GetFriendInfoAsync` | 获取好友详细信息 | |
| 19 | `GetFriendList` | `IBotApi.GetFriendListAsync` | 获取好友列表 | |
| 20 | `GetFriendRequests` | `IBotApi.GetFriendRequestsAsync` | 获取好友申请列表 | |
| 21 | `HandleFriendRequest_NoPending` | `IBotApi.HandleFriendRequestAsync` | 传入无效参数，验证错误处理 | |
| 22 | `DeleteFriend_ProtocolSupport` | `IBotApi.DeleteFriendAsync` | 删除非好友用户，验证返回 ProtocolNotFound | |
| 23 | `SendFriendNudge` | `IBotApi.SendFriendNudgeAsync` | 向 Secondary Bot 发送好友戳一戳 | |
| 24 | `GetGroupInfo` | `IBotApi.GetGroupInfoAsync` | 获取群信息 | |
| 25 | `GetGroupList` | `IBotApi.GetGroupListAsync` | 获取群列表 | |
| 26 | `GetGroupMemberInfo` | `IBotApi.GetGroupMemberInfoAsync` | 获取群成员信息 | |
| 27 | `GetGroupMemberList` | `IBotApi.GetGroupMemberListAsync` | 获取群成员列表 | |
| 28 | `GetGroupNotifications` | `IBotApi.GetGroupNotificationsAsync` | 获取群系统通知 | |
| 29 | `SetGroupName_AndRestore` | `IBotApi.SetGroupNameAsync` | 修改群名后恢复 | ⚠️ Bot 需为群管理员 |
| 30 | `SetGroupMemberCard_AndRestore` | `IBotApi.SetGroupMemberCardAsync` | 修改群名片后恢复 | ⚠️ Bot 需为群管理员 |
| 31 | `SetGroupMemberSpecialTitle` | `IBotApi.SetGroupMemberSpecialTitleAsync` | 设置群特殊头衔 | ⚠️ Bot 需为群主 |
| 32 | `SetGroupAdmin_ProtocolSupport` | `IBotApi.SetGroupAdminAsync` | 设置管理员（协议支持检测） | ⚠️ Bot 需为群主 |
| 33 | `KickGroupMember_ProtocolSupport` | `IBotApi.KickGroupMemberAsync` | 踢出群成员（使用无效 ID） | |
| 34 | `LeaveGroup_ProtocolSupport` | `IBotApi.LeaveGroupAsync` | 退群（使用无效群 ID） | |
| 35 | `MuteGroupMember_ZeroDuration` | `IBotApi.MuteGroupMemberAsync` | 以 0 时长禁言（解除禁言） | ⚠️ Bot 需为群管理员 |
| 36 | `MuteAndUnmuteGroupAll` | `IBotApi.MuteGroupAllAsync` | 全员禁言后解除 | ⚠️ Bot 需为群管理员 |
| 37 | `HandleGroupRequest_InvalidParams` | `IBotApi.HandleGroupRequestAsync` | 传入无效参数验证错误处理 | |
| 38 | `HandleGroupInvitation_Accept_ProtocolSupport` | `IBotApi.HandleGroupInvitationAsync` | 接受群邀请（协议支持检测） | |
| 39 | `HandleGroupInvitation_Reject_ProtocolSupport` | `IBotApi.HandleGroupInvitationAsync` | 拒绝群邀请（协议支持检测） | |
| 40 | `SetGroupAvatar` | `IBotApi.SetGroupAvatarAsync` | 设置群头像 | ⚠️ Bot 需为群管理员；需要图片文件 |
| 41 | `SendGroupNudge` | `IBotApi.SendGroupNudgeAsync` | 发送群戳一戳 | |
| 42 | `GetGroupAnnouncements` | `IBotApi.GetGroupAnnouncementsAsync` | 获取群公告列表 | |
| 43 | `Announcements_CreateAndDelete` | 创建 + 删除群公告 | 创建公告后删除 | ⚠️ Bot 需为群管理员 |
| 44 | `GetGroupEssenceMessages` | `IBotApi.GetGroupEssenceMessagesAsync` | 获取精华消息列表 | |
| 45 | `EssenceMessages_SetAndUnset` | 设置 + 取消精华消息 | 发送消息 → 设为精华 → 取消 | ⚠️ Bot 需为群管理员 |
| 46 | `GetGroupFiles` | `IBotApi.GetGroupFilesAsync` | 获取群文件列表 | |
| 47 | `GetGroupFileDownloadUrl` | `IBotApi.GetGroupFileDownloadUrlAsync` | 获取群文件下载链接 | |
| 48 | `GetPrivateFileDownloadUrl` | `IBotApi.GetPrivateFileDownloadUrlAsync` | 获取私聊文件下载链接 | |
| 49 | `CreateAndDeleteGroupFolder` | 创建 + 删除群文件夹 | 创建文件夹后删除 | |
| 50 | `DeleteGroupFile` | `IBotApi.DeleteGroupFileAsync` | 删除群文件 | |
| 51 | `RenameGroupFile` | `IBotApi.RenameGroupFileAsync` | 重命名群文件 | |
| 52 | `RenameGroupFolder` | `IBotApi.RenameGroupFolderAsync` | 重命名群文件夹 | |
| 53 | `MoveGroupFile` | `IBotApi.MoveGroupFileAsync` | 移动群文件到指定文件夹 | |
| 54 | `UploadGroupFile` | `IBotApi.UploadGroupFileAsync` | 上传群文件 | |
| 55 | `UploadPrivateFile` | `IBotApi.UploadPrivateFileAsync` | 上传私聊文件 | |
| 56 | `GetResourceTempUrl` | `IBotApi.GetResourceTempUrlAsync` | 获取资源临时 URL | |
| 57 | `GetCustomFaceUrlList` | `IBotApi.GetCustomFaceUrlListAsync` | 获取自定义表情列表 | |
| 58 | `SetNickname_AndRestore` | `IBotApi.SetNicknameAsync` | 修改昵称后恢复 | |
| 59 | `SetBio_AndRestore` | `IBotApi.SetBioAsync` | 修改签名后恢复 | |
| 60 | `SetAvatar` | `IBotApi.SetAvatarAsync` | 设置头像 | ⚠️ 需要头像图片文件 |
| 61 | `SendProfileLike` | `IBotApi.SendProfileLikeAsync` | 点赞资料卡 | |
| 62 | `SendGroupMessageReaction` | `IBotApi.SendGroupMessageReactionAsync` | 对群消息发表回应 | |
| 63 | `GetPeerPins_ReturnsResult` | `IMilkyExtApi.GetPeerPinsAsync` | 获取会话置顶列表 | |
| 64 | `SetPeerPin_PinAndUnpin` | `IMilkyExtApi.SetPeerPinAsync` | 置顶 + 取消置顶会话 | |

### EventTests（14 项，13 🔵 双账号 + 1 🟢 单账号）

事件测试通过一个 Bot 执行操作、另一个 Bot 的 Service 监听事件来验证事件交付。`Event_PeerPinChanged` 仅使用 Primary Bot 操作和监听。

| # | 测试名 | 被测内容 | 测试方式 | 双账号 | 备注 |
|---|--------|----------|----------|--------|------|
| 1 | `Event_MessageReceived` | `EventDispatcher.OnMessageReceived` | Primary 发送群消息 → Secondary Service 监听消息事件 | 🔵 | |
| 2 | `Event_MessageReceived_FromSecondary` | `EventDispatcher.OnMessageReceived` | Secondary 发送群消息 → Primary Service 监听消息事件 | 🔵 | |
| 3 | `Event_MessageDeleted` | `EventDispatcher.OnMessageDeleted` | Primary 发送+撤回消息 → Secondary Service 监听撤回事件 | 🔵 | |
| 4 | `Event_GroupNudge_FromSecondary` | `EventDispatcher.OnNudge` | Secondary 向 Primary 发送群戳一戳 → Primary Service 监听 | 🔵 | |
| 5 | `Event_FileUpload_DualBot` | `EventDispatcher.OnFileUpload` | Secondary 上传群文件 → Primary Service 监听上传事件 | 🔵 | |
| 6 | `Event_GroupReaction_DualBot` | `EventDispatcher.OnGroupReaction` | Secondary 发送消息回应 → Primary Service 监听回应事件 | 🔵 | |
| 7 | `Event_GroupAdminChanged_DualBot` | `EventDispatcher.OnGroupAdminChanged` | Primary 设置 Secondary 为管理员 → Secondary Service 监听 | 🔵 | ⚠️ Primary Bot 需为群主 |
| 8 | `Event_GroupEssenceChanged` | `EventDispatcher.OnGroupEssenceChanged` | Primary 设置精华消息 → Secondary Service 监听精华变更事件 | 🔵 | ⚠️ Bot 需为群管理员 |
| 9 | `Event_GroupMute` | `EventDispatcher.OnGroupMute` | Primary 全员禁言 → Secondary Service 监听禁言事件 | 🔵 | ⚠️ Bot 需为群管理员 |
| 10 | `Event_GroupNameChanged` | `EventDispatcher.OnGroupNameChanged` | Primary 修改群名 → Secondary Service 监听群名变更事件 | 🔵 | ⚠️ Bot 需为群管理员 |
| 11 | `Event_GroupNudge` | `EventDispatcher.OnNudge` | Primary 发送群戳一戳 → Secondary Service 监听戳一戳事件 | 🔵 | |
| 12 | `Event_PeerPinChanged` | `EventDispatcher.OnPeerPinChanged` | Primary 置顶群会话 → Primary Service 监听置顶变更事件 | 🟢 | |
| 13 | `Event_FriendNudge_FromSecondary` | `EventDispatcher.OnNudge`（好友） | Secondary 向 Primary 发送好友戳一戳 → Secondary Service 监听 | 🔵 | |
| 14 | `Event_FriendFileUpload_DualBot` | `EventDispatcher.OnFileUpload`（好友） | Secondary 向 Primary 上传私聊文件 → Primary Service 监听 | 🔵 | |

### CommandTests（11 项，均为 🔵 双账号）

命令测试通过 Secondary Bot 发送触发消息、Primary Bot 的 CommandManager 接收并处理来验证命令系统。

| # | 测试名 | 被测内容 | 测试方式 | 备注 |
|---|--------|----------|----------|------|
| 1 | `Command_KeywordMatch` | 关键词完全匹配 | Primary 注册命令 → Secondary 发送关键词 → 验证命令触发 | |
| 2 | `Command_RegexMatch` | 正则匹配 | Primary 注册正则命令 → Secondary 发送匹配文本 → 验证触发 | |
| 3 | `Command_KeywordMatch_Substring` | 关键词子串匹配 | 注册子串匹配命令 → 发送含关键词消息 → 验证触发 | |
| 4 | `Command_GroupOnly` | 群聊限定命令 | 注册群聊命令 → 分别在群/私聊发送 → 仅群聊触发 | |
| 5 | `Command_PrivateOnly` | 私聊限定命令 | 注册私聊命令 → 分别在群/私聊发送 → 仅私聊触发 | |
| 6 | `Command_Priority` | 命令优先级 | 注册高低优先级命令 → 验证高优先级先执行 | |
| 7 | `Command_Permission_OwnerRequired` | 权限检查 | 注册仅群主命令 → Secondary（非群主）发送 → 验证不触发 | |
| 8 | `ContinuousDialogue_WaitNextMessage` | 连续对话 - 等待下条消息 | 触发命令 → 命令 handler 调用 WaitForNextMessage → Secondary 发送跟进 | |
| 9 | `ContinuousDialogue_Timeout` | 连续对话 - 超时 | 触发命令 → handler 等待 → 不发跟进 → 验证超时返回 null | |
| 10 | `ContinuousDialogue_MultiTurn` | 连续对话 - 多轮 | 触发命令 → handler 连续两次 WaitForNextMessage → Secondary 依次发送 | |
| 11 | `Command_NonMatchingMessage_PassesThrough` | 非匹配消息穿透 | 注册命令 → 发送不匹配消息 → 验证 EventChain 未被中断 | |

### MessageTypeTests（12 项，均为 🔵 双账号）

消息类型测试通过 Primary 发送特定类型消息、Secondary Service 监听接收来验证消息段序列化/反序列化。

| # | 测试名 | 被测内容 | 测试方式 | 备注 |
|---|--------|----------|----------|------|
| 1 | `SendReceive_Text` | 纯文本消息 | Primary 发送文本 → Secondary 接收 → 验证文本一致 | |
| 2 | `SendReceive_Text_SpecialCharacters` | 特殊字符文本 | 发送含 emoji/CJK/换行等 → 验证原样接收 | |
| 3 | `SendReceive_Face` | 表情消息 | 发送 FaceSegment → 接收 → 验证 FaceId | |
| 4 | `SendReceive_Mention` | @提及消息 | 发送 MentionSegment → 接收 → 验证目标 UserId | |
| 5 | `Send_MentionAll` | @全体成员 | 发送 MentionAllSegment → 验证发送成功 | ⚠️ Bot 需有 @全体 剩余配额 |
| 6 | `SendReceive_Reply` | 回复消息 | 先发消息获取 ID → 发送 ReplySegment → 接收验证 | |
| 7 | `SendReceive_MultiSegment` | 多段消息 | 发送 Text+Face+Mention 组合 → 接收 → 验证各段 | |
| 8 | `Send_Image` | 图片消息 | 发送 ImageSegment → 验证发送成功 | ⚠️ 需要测试图片文件 |
| 9 | `Send_Forward` | 合并转发消息 | 发送 ForwardSegment → 验证发送成功 | |
| 10 | `Send_LightApp` | 轻应用消息 | 发送 LightAppSegment → 验证发送成功 | |
| 11 | `SendReceive_Audio` | 语音消息 | 发送 AudioSegment → 接收验证 | ⚠️ 需要 `SORA_TEST_AUDIO_FILE` |
| 12 | `SendReceive_Video` | 视频消息 | 发送 VideoSegment → 接收验证 | ⚠️ 需要 `SORA_TEST_VIDEO_FILE` |

---

## OneBot v11 协议

### ApiTests（68 项，66 🟢 单账号 + 2 🔵 双账号）

| # | 测试名 | 被测内容 | 测试方式 | 双账号 | 备注 |
|---|--------|----------|----------|--------|------|
| 1 | `GetSelfInfo` | `IBotApi.GetSelfInfoAsync` | 调用 API，验证返回 | 🟢 | |
| 2 | `GetCookies` | `IBotApi.GetCookiesAsync` | 调用 API，验证状态 | 🟢 | |
| 3 | `GetCsrfToken` | `IBotApi.GetCsrfTokenAsync` | 调用 API，验证状态 | 🟢 | |
| 4 | `GetImplInfo` | `IBotApi.GetImplInfoAsync` | 调用 API，验证实现信息 | 🟢 | |
| 5 | `GetCustomFaceUrlList` | `IBotApi.GetCustomFaceUrlListAsync` | 获取自定义表情列表 | 🟢 | |
| 6 | `SendGroupMessage` | `IBotApi.SendGroupMessageAsync` | 发送群文本消息 | 🟢 | |
| 7 | `SendGroupMessage_WithMention` | 发送 @mention 群消息 | 发送 MentionSegment | 🟢 | |
| 8 | `SendPrivateMessage` | `IBotApi.SendPrivateMessageAsync` | 发送私聊消息 | 🟢 | |
| 9 | `SendAndRecallGroupMessage` | 发送 + 撤回群消息 | 先发送再撤回 | 🟢 | |
| 10 | `SendAndRecallPrivateMessage` | 发送 + 撤回私聊消息 | 先发送再撤回 | 🟢 | |
| 11 | `GetMessage` | `IBotApi.GetMessageAsync` | 按 MessageId 获取消息 | 🟢 | |
| 12 | `GetForwardMessages` | `IBotApi.GetForwardMessagesAsync` | 获取合并转发消息内容 | 🟢 | |
| 13 | `GetHistoryMessages` | `IBotApi.GetHistoryMessagesAsync` | 获取群历史消息 | 🟢 | |
| 14 | `MarkMessageAsRead` | `IBotApi.MarkMessageAsReadAsync` | 标记消息已读 | 🟢 | |
| 15 | `GetUserInfo` | `IBotApi.GetUserInfoAsync` | 获取用户信息 | 🟢 | |
| 16 | `GetFriendInfo` | `IBotApi.GetFriendInfoAsync` | 获取好友信息 | 🟢 | |
| 17 | `GetFriendList` | `IBotApi.GetFriendListAsync` | 获取好友列表 | 🟢 | |
| 18 | `GetFriendRequests` | `IBotApi.GetFriendRequestsAsync` | 获取好友申请列表 | 🟢 | |
| 19 | `HandleFriendRequest_NoPending` | `IBotApi.HandleFriendRequestAsync` | 无效参数错误处理 | 🟢 | |
| 20 | `DeleteFriend_ProtocolSupport` | `IBotApi.DeleteFriendAsync` | 删除好友协议支持检测 | 🟢 | |
| 21 | `SendFriendNudge` | `IBotApi.SendFriendNudgeAsync` | 好友戳一戳 | 🟢 | |
| 22 | `GetUserProfile` | `IBotApi.GetUserProfileAsync` | 获取用户资料卡 | 🟢 | |
| 23 | `GetGroupInfo` | `IBotApi.GetGroupInfoAsync` | 获取群信息 | 🟢 | |
| 24 | `GetGroupList` | `IBotApi.GetGroupListAsync` | 获取群列表 | 🟢 | |
| 25 | `GetGroupMemberInfo` | `IBotApi.GetGroupMemberInfoAsync` | 获取群成员信息 | 🟢 | |
| 26 | `GetGroupMemberList` | `IBotApi.GetGroupMemberListAsync` | 获取群成员列表 | 🟢 | |
| 27 | `GetGroupNotifications` | `IBotApi.GetGroupNotificationsAsync` | 获取群系统通知 | 🟢 | |
| 28 | `SetGroupName_AndRestore` | `IBotApi.SetGroupNameAsync` | 修改群名后恢复 | 🟢 | ⚠️ Bot 需为群管理员 |
| 29 | `SetGroupMemberCard_AndRestore` | `IBotApi.SetGroupMemberCardAsync` | 修改群名片后恢复 | 🟢 | ⚠️ Bot 需为群管理员 |
| 30 | `SetGroupMemberSpecialTitle` | `IBotApi.SetGroupMemberSpecialTitleAsync` | 群特殊头衔 | 🟢 | ⚠️ Bot 需为群主 |
| 31 | `SetGroupAdmin_ProtocolSupport` | `IBotApi.SetGroupAdminAsync` | 设置管理员 | 🟢 | ⚠️ Bot 需为群主 |
| 32 | `KickGroupMember_ProtocolSupport` | `IBotApi.KickGroupMemberAsync` | 踢出成员 | 🟢 | |
| 33 | `LeaveGroup_ProtocolSupport` | `IBotApi.LeaveGroupAsync` | 退群 | 🟢 | |
| 34 | `MuteGroupMember_ZeroDuration` | `IBotApi.MuteGroupMemberAsync` | 0 时长禁言 | 🟢 | ⚠️ Bot 需为群管理员 |
| 35 | `MuteAndUnmuteGroupAll` | `IBotApi.MuteGroupAllAsync` | 全员禁言/解除 | 🟢 | ⚠️ Bot 需为群管理员 |
| 36 | `HandleGroupRequest_NoPending` | `IBotApi.HandleGroupRequestAsync` | 无效参数处理 | 🟢 | |
| 37 | `HandleGroupInvitation_Accept` | `IBotApi.HandleGroupInvitationAsync` | 接受群邀请 | 🟢 | |
| 38 | `HandleGroupInvitation_Reject` | `IBotApi.HandleGroupInvitationAsync` | 拒绝群邀请 | 🟢 | |
| 39 | `SetGroupAvatar` | `IBotApi.SetGroupAvatarAsync` | 设置群头像 | 🟢 | ⚠️ Bot 需为群管理员 |
| 40 | `SendGroupNudge` | `IBotApi.SendGroupNudgeAsync` | 群戳一戳 | 🟢 | |
| 41 | `SetGroupEssenceMessage_NotSupported` | `IBotApi.SetGroupEssenceMessageAsync` | OB11 精华消息（协议支持检测） | 🟢 | |
| 42 | `GetGroupAnnouncements` | `IBotApi.GetGroupAnnouncementsAsync` | 群公告列表 | 🟢 | |
| 43 | `Announcements_CreateAndDelete` | 创建 + 删除群公告 | 创建后删除 | 🟢 | ⚠️ Bot 需为群管理员 |
| 44 | `DeleteGroupAnnouncement_InvalidId` | `IBotApi.DeleteGroupAnnouncementAsync` | 无效 ID 错误处理 | 🟢 | |
| 45 | `GetGroupEssenceMessages` | `IBotApi.GetGroupEssenceMessagesAsync` | 精华消息列表 | 🟢 | |
| 46 | `GetGroupFiles` | `IBotApi.GetGroupFilesAsync` | 群文件列表 | 🟢 | |
| 47 | `GetGroupFileDownloadUrl` | `IBotApi.GetGroupFileDownloadUrlAsync` | 群文件下载链接 | 🟢 | |
| 48 | `CreateAndDeleteGroupFolder` | 创建 + 删除群文件夹 | 创建后删除 | 🟢 | |
| 49 | `DeleteGroupFile` | `IBotApi.DeleteGroupFileAsync` | 删除群文件 | 🟢 | |
| 50 | `RenameGroupFile_NotSupported` | `IBotApi.RenameGroupFileAsync` | OB11 不支持检测 | 🟢 | |
| 51 | `RenameGroupFolder_NotSupported` | `IBotApi.RenameGroupFolderAsync` | OB11 不支持检测 | 🟢 | |
| 52 | `MoveGroupFile_NotSupported` | `IBotApi.MoveGroupFileAsync` | OB11 不支持检测 | 🟢 | |
| 53 | `UploadGroupFile` | `IBotApi.UploadGroupFileAsync` | 上传群文件 | 🟢 | |
| 54 | `UploadPrivateFile_NotSupported` | `IBotApi.UploadPrivateFileAsync` | OB11 不支持检测 | 🟢 | |
| 55 | `GetPrivateFileDownloadUrl_NotSupported` | `IBotApi.GetPrivateFileDownloadUrlAsync` | OB11 不支持检测 | 🟢 | |
| 56 | `GetResourceTempUrl` | `IBotApi.GetResourceTempUrlAsync` | 资源临时 URL | 🟢 | |
| 57 | `SetNickname_AndRestore` | `IBotApi.SetNicknameAsync` | 修改昵称后恢复 | 🟢 | |
| 58 | `SetBio_AndRestore` | `IBotApi.SetBioAsync` | 修改签名后恢复 | 🟢 | |
| 59 | `SetAvatar` | `IBotApi.SetAvatarAsync` | 设置头像 | 🟢 | ⚠️ 需要头像图片 |
| 60 | `SendProfileLike` | `IBotApi.SendProfileLikeAsync` | 资料卡点赞 | 🟢 | |
| 61 | `SendGroupMessageReaction_NotSupported` | `IBotApi.SendGroupMessageReactionAsync` | OB11 不支持检测 | 🟢 | |
| 62 | `ExtApi_FetchCustomFace` | `IOb11ExtApi.FetchCustomFaceAsync` | OB11 扩展：自定义表情 | 🟢 | |
| 63 | `ExtApi_GetFriendsWithCategory` | `IOb11ExtApi.GetFriendsWithCategoryAsync` | OB11 扩展：分组好友 | 🟢 | |
| 64 | `ExtApi_GetGroupShutList` | `IOb11ExtApi.GetGroupShutListAsync` | OB11 扩展：禁言列表 | 🟢 | |
| 65 | `ExtApi_SetFriendRemark` | `IOb11ExtApi.SetFriendRemarkAsync` | OB11 扩展：设置好友备注 | 🔵 | 需操作 Secondary Bot |
| 66 | `ExtApi_SetGroupRemark` | `IOb11ExtApi.SetGroupRemarkAsync` | OB11 扩展：设置群备注 | 🟢 | |
| 67 | `ExtApi_ForwardGroupSingleMsg` | `IOb11ExtApi.ForwardGroupSingleMsgAsync` | OB11 扩展：转发群消息 | 🟢 | |
| 68 | `ExtApi_ForwardFriendSingleMsg` | `IOb11ExtApi.ForwardFriendSingleMsgAsync` | OB11 扩展：转发好友消息 | 🔵 | 需发送给 Secondary Bot |

### EventTests（11 项，均为 🔵 双账号）

| # | 测试名 | 被测内容 | 测试方式 | 备注 |
|---|--------|----------|----------|------|
| 1 | `Event_MessageReceived` | `EventDispatcher.OnMessageReceived` | Primary 发送群消息 → Secondary Service 监听消息事件 | |
| 2 | `Event_MessageReceived_FromSecondary` | `EventDispatcher.OnMessageReceived` | Secondary 发送群消息 → Primary Service 监听消息事件 | |
| 3 | `Event_MessageDeleted` | `EventDispatcher.OnMessageDeleted` | Primary 发送+撤回 → Secondary Service 监听撤回事件 | |
| 4 | `Event_GroupAdminChanged_Automated` | `EventDispatcher.OnGroupAdminChanged` | Primary 设 Secondary 管理员 → Secondary Service 监听 | ⚠️ Primary Bot 需为群主 |
| 5 | `Event_GroupMute` | `EventDispatcher.OnGroupMute` | Primary 全员禁言 → Secondary Service 监听禁言事件 | ⚠️ Bot 需为群管理员 |
| 6 | `Event_GroupNameChanged` | `EventDispatcher.OnGroupNameChanged` | Primary 改群名 → Secondary Service 监听 | ⚠️ Bot 需为群管理员；OB11 协议无此事件 |
| 7 | `Event_GroupNudge` | `EventDispatcher.OnNudge` | Primary 群戳一戳 → Secondary Service 监听 | |
| 8 | `Event_GroupNudge_FromSecondary` | `EventDispatcher.OnNudge` | Secondary 群戳一戳 Primary → Primary Service 监听 | |
| 9 | `Event_FileUpload_FromSecondary` | `EventDispatcher.OnFileUpload` | Secondary 上传群文件 → Primary Service 监听 | |
| 10 | `Event_GroupReaction_FromSecondary` | `EventDispatcher.OnGroupReaction` | Secondary 消息回应 → Primary Service 监听 | |
| 11 | `Event_GroupEssenceChanged` | `EventDispatcher.OnGroupEssenceChanged` | Primary 设精华 → Secondary Service 监听 | ⚠️ Bot 需为群管理员 |

### CommandTests（12 项，均为 🔵 双账号）

| # | 测试名 | 被测内容 | 测试方式 | 备注 |
|---|--------|----------|----------|------|
| 1 | `Command_KeywordMatch` | 关键词完全匹配 | Primary 注册 → Secondary 触发 → 验证 | |
| 2 | `Command_RegexMatch` | 正则匹配 | Primary 注册 → Secondary 触发 → 验证 | |
| 3 | `Command_KeywordMatch_Substring` | 关键词子串匹配 | 注册子串匹配 → 发送含关键词消息 | |
| 4 | `Command_GroupOnly` | 群聊限定命令 | 分别在群/私聊触发 → 仅群触发 | |
| 5 | `Command_PrivateOnly` | 私聊限定命令 | 分别在群/私聊触发 → 仅私聊触发 | |
| 6 | `Command_Priority` | 命令优先级 | 注册高低优先级 → 验证执行顺序 | |
| 7 | `Command_DynamicRegister` | 动态命令注册 | 运行时注册 → 触发 → 验证 → 注销 | |
| 8 | `Command_Permission_OwnerRequired` | 权限检查 | 仅群主命令 → 非群主触发 → 不响应 | |
| 9 | `ContinuousDialogue_WaitNextMessage` | 连续对话 - 等待 | 触发 → WaitForNextMessage → 发送跟进 | |
| 10 | `ContinuousDialogue_Timeout` | 连续对话 - 超时 | 触发 → 等待 → 不发跟进 → 验证超时 null | |
| 11 | `ContinuousDialogue_MultiTurn` | 连续对话 - 多轮 | 触发 → 两轮 WaitForNextMessage → 依次发送 | |
| 12 | `Command_NonMatchingMessage_PassesThrough` | 非匹配消息穿透 | 发送不匹配消息 → EventChain 未中断 | |

### MessageTypeTests（10 项，均为 🔵 双账号）

| # | 测试名 | 被测内容 | 测试方式 | 备注 |
|---|--------|----------|----------|------|
| 1 | `SendReceive_Text` | 纯文本消息 | Primary 发送 → Secondary 接收验证 | |
| 2 | `SendReceive_Text_SpecialCharacters` | 特殊字符文本 | 发送 emoji/CJK/换行 → 原样接收 | |
| 3 | `SendReceive_Face` | 表情消息 | 发送 FaceSegment → 验证 FaceId | |
| 4 | `SendReceive_Mention` | @提及消息 | 发送 MentionSegment → 验证目标 | |
| 5 | `Send_MentionAll` | @全体成员 | 发送 → 验证成功 | ⚠️ Bot 需有配额 |
| 6 | `SendReceive_Reply` | 回复消息 | 发送 ReplySegment → 接收验证 | |
| 7 | `SendReceive_MultiSegment` | 多段组合消息 | 发送多段 → 接收验证各段 | |
| 8 | `Send_Image` | 图片消息 | 发送 ImageSegment → 验证成功 | ⚠️ 需要测试图片 |
| 9 | `Send_Forward` | 合并转发消息 | 发送 ForwardSegment → 验证成功 | |
| 10 | `Send_LightApp` | 轻应用消息 | 发送 LightAppSegment → 验证成功 | |

---

## 统计汇总

| 协议 | 测试类 | 总数 | 🟢 单账号 | 🔵 双账号 |
|------|--------|------|-----------|-----------|
| **Milky** | ApiTests | 64 | 64 | 0 |
| **Milky** | EventTests | 14 | 1 | 13 |
| **Milky** | CommandTests | 11 | 0 | 11 |
| **Milky** | MessageTypeTests | 12 | 0 | 12 |
| **OB11** | ApiTests | 68 | 66 | 2 |
| **OB11** | EventTests | 11 | 0 | 11 |
| **OB11** | CommandTests | 12 | 0 | 12 |
| **OB11** | MessageTypeTests | 10 | 0 | 10 |
| **合计** | | **202** | **131** | **71** |

## 环境变量

| 变量 | 说明 | 影响范围 |
|------|------|----------|
| `SORA_TEST_MILKY_HOST` | Milky 主 Bot 地址 | 所有 Milky 测试 |
| `SORA_TEST_MILKY_SECONDARY_HOST` | Milky 副 Bot 地址 | 所有 Milky 🔵 双账号测试 |
| `SORA_TEST_OB11_HOST` | OB11 主 Bot 地址 | 所有 OB11 测试 |
| `SORA_TEST_OB11_SECONDARY_HOST` | OB11 副 Bot 地址 | 所有 OB11 🔵 双账号测试 |
| `SORA_TEST_GROUP_ID` | 测试群号 | 所有群相关测试 |
| `SORA_TEST_AUDIO_FILE` | 测试音频文件路径 | Audio 消息类型测试 |
| `SORA_TEST_VIDEO_FILE` | 测试视频文件路径 | Video 消息类型测试 |

## 特殊前置条件

| 条件 | 影响的测试 |
|------|-----------|
| **Primary Bot 为群主** | SetGroupAdmin, SetGroupMemberSpecialTitle, Event_GroupAdminChanged |
| **Primary Bot 为群管理员** | SetGroupName, SetGroupMemberCard, MuteGroupMember, MuteGroupAll, SetGroupAvatar, Announcements, EssenceMessages, Event_GroupMute, Event_GroupNameChanged, Event_GroupEssenceChanged |
| **Bot @全体 配额未耗尽** | Send_MentionAll |
| **两 Bot 互为好友** | SendPrivateMessage, SendFriendNudge, FriendNudge/FileUpload 事件测试 |

---

## 相关文档

- [← 返回 README](../README.md)
- [测试说明](TESTING.md) — 完整测试架构与环境变量
- [已移除测试](REMOVED_TESTS.md) — 无法自动化的测试及原因
