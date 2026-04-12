using System.Collections.Concurrent;
using Xunit;

namespace Sora.Tests.Unit.Command;

/// <summary>Test command group with re-entry protection enabled.</summary>
[CommandGroup(Name = "reentry-test")]
public static class ReentryCommands
{
    /// <summary>Signals that the command has started execution.</summary>
    public static TaskCompletionSource? Started { get; set; }

    /// <summary>Gate that controls when the command finishes.</summary>
    public static TaskCompletionSource? Gate { get; set; }

    /// <summary>Tracks how many times the command actually executed.</summary>
    public static int ExecutionCount;

    /// <see cref="CommandAttribute.PreventReentry" />
    [Command(Expressions = ["slow"], MatchType = MatchType.Full, PreventReentry = true)]
    public static async ValueTask SlowCommand(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref ExecutionCount);
        Started?.TrySetResult();
        if (Gate is not null)
            await Gate.Task;
    }

    /// <see cref="CommandAttribute.ReentryMessage" />
    [Command(
        Expressions = ["slow-msg"],
        MatchType = MatchType.Full,
        PreventReentry = true,
        ReentryMessage = "请等待上一条指令执行完毕")]
    public static async ValueTask SlowCommandWithMessage(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref ExecutionCount);
        Started?.TrySetResult();
        if (Gate is not null)
            await Gate.Task;
    }

    /// <see cref="CommandAttribute.PreventReentry" />
    [Command(Expressions = ["fast"], MatchType = MatchType.Full, PreventReentry = false)]
    public static async ValueTask FastCommand(MessageReceivedEvent e)
    {
        Interlocked.Increment(ref ExecutionCount);
        await ValueTask.CompletedTask;
    }

    public static void Reset()
    {
        Started        = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Gate           = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ExecutionCount = 0;
    }
}

/// <summary>Minimal stub that records SendGroupMessageAsync / SendFriendMessageAsync calls.</summary>
internal sealed class RecordingBotApi : IBotApi
{
    public ConcurrentBag<(GroupId GroupId, string Text)> GroupMessages  { get; } = [];
    public ConcurrentBag<(UserId UserId, string Text)>   FriendMessages { get; } = [];

    public T? GetExtension<T>() where T : class, IAdapterExtension => null;

    public ValueTask<SendMessageResult> SendGroupMessageAsync(GroupId groupId, MessageBody message, CancellationToken ct = default)
    {
        GroupMessages.Add((groupId, message.GetText()));
        return new ValueTask<SendMessageResult>(new SendMessageResult());
    }

    public ValueTask<SendMessageResult> SendFriendMessageAsync(UserId userId, MessageBody message, CancellationToken ct = default)
    {
        FriendMessages.Add((userId, message.GetText()));
        return new ValueTask<SendMessageResult>(new SendMessageResult());
    }

    // All remaining IBotApi members return default — unused in these tests.
    public ValueTask<ApiResult<BotIdentity>> GetSelfInfoAsync(CancellationToken ct = default) => default!;
    public ValueTask<ApiResult<ImplInfo>> GetImplInfoAsync(CancellationToken ct = default) => default!;
    public ValueTask<ApiResult<string>> GetCookiesAsync(string domain, CancellationToken ct = default) => default!;
    public ValueTask<ApiResult<string>> GetCsrfTokenAsync(CancellationToken ct = default) => default!;
    public ValueTask<ApiResult<string>> GetResourceTempUrlAsync(string resourceId, CancellationToken ct = default) => default!;
    public ValueTask<ApiResult<IReadOnlyList<string>>> GetCustomFaceUrlListAsync(CancellationToken ct = default) => default!;

    public ValueTask<ApiResult<MessageContext>> GetMessageAsync(
        MessageSourceType scene,
        long              peerId,
        MessageId         messageSeq,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult<HistoryMessagesResult>> GetHistoryMessagesAsync(
        MessageSourceType scene,
        long              peerId,
        MessageId?        startMessageSeq = null,
        int               limit           = 20,
        CancellationToken ct              = default) =>
        default!;

    public ValueTask<ApiResult<IReadOnlyList<MessageContext>>> GetForwardMessagesAsync(
        string            forwardId,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> RecallGroupMessageAsync(GroupId groupId, MessageId messageId, CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> RecallPrivateMessageAsync(UserId userId, MessageId messageId, CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> MarkMessageAsReadAsync(
        MessageSourceType scene,
        long              peerId,
        MessageId         messageSeq,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult<UserInfo>> GetUserInfoAsync(UserId userId, bool noCache = false, CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult<UserProfile>> GetUserProfileAsync(UserId userId, CancellationToken ct = default) => default!;

    public ValueTask<ApiResult<FriendInfo>> GetFriendInfoAsync(UserId userId, bool noCache = false, CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult<IReadOnlyList<FriendInfo>>> GetFriendListAsync(bool noCache = false, CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult<IReadOnlyList<FriendRequestInfo>>> GetFriendRequestsAsync(
        int               limit      = 20,
        bool              isFiltered = false,
        CancellationToken ct         = default) =>
        default!;

    public ValueTask<ApiResult> HandleFriendRequestAsync(
        UserId            fromUserId,
        bool              isFiltered,
        bool              approve,
        string            remark = "",
        CancellationToken ct     = default) =>
        default!;

    public ValueTask<ApiResult> DeleteFriendAsync(UserId    userId, CancellationToken ct = default) => default!;
    public ValueTask<ApiResult> SendFriendNudgeAsync(UserId userId, CancellationToken ct = default) => default!;

    public ValueTask<ApiResult<GroupInfo>> GetGroupInfoAsync(GroupId groupId, bool noCache = false, CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult<IReadOnlyList<GroupInfo>>> GetGroupListAsync(bool noCache = false, CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult<GroupMemberInfo>> GetGroupMemberInfoAsync(
        GroupId           groupId,
        UserId            userId,
        bool              noCache = false,
        CancellationToken ct      = default) =>
        default!;

    public ValueTask<ApiResult<IReadOnlyList<GroupMemberInfo>>> GetGroupMemberListAsync(
        GroupId           groupId,
        bool              noCache = false,
        CancellationToken ct      = default) =>
        default!;

    public ValueTask<ApiResult<IReadOnlyList<GroupAnnouncementInfo>>> GetGroupAnnouncementsAsync(
        GroupId           groupId,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult<GroupEssenceMessagesPage>> GetGroupEssenceMessagesAsync(
        GroupId           groupId,
        int               pageIndex,
        int               pageSize,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult<GroupNotificationsResult>> GetGroupNotificationsAsync(
        long?             startNotificationSeq = null,
        bool              isFiltered           = false,
        int               limit                = 20,
        CancellationToken ct                   = default) =>
        default!;

    public ValueTask<ApiResult> HandleGroupRequestAsync(
        GroupId                   groupId,
        long                      notificationSeq,
        GroupJoinNotificationType joinNotificationType,
        bool                      isFiltered,
        bool                      approve,
        string                    reason = "",
        CancellationToken         ct     = default) =>
        default!;

    public ValueTask<ApiResult> HandleGroupInvitationAsync(
        GroupId           groupId,
        long              invitationSeq,
        bool              approve,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> SetGroupNameAsync(GroupId   groupId, string name,     CancellationToken ct = default) => default!;
    public ValueTask<ApiResult> SetGroupAvatarAsync(GroupId groupId, string imageUri, CancellationToken ct = default) => default!;

    public ValueTask<ApiResult> SetGroupAdminAsync(GroupId groupId, UserId userId, bool enable, CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> SetGroupMemberCardAsync(GroupId groupId, UserId userId, string card, CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> SetGroupMemberSpecialTitleAsync(
        GroupId           groupId,
        UserId            userId,
        string            title,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> SetGroupEssenceMessageAsync(
        GroupId           groupId,
        MessageId         messageSeq,
        bool              isSet = true,
        CancellationToken ct    = default) =>
        default!;

    public ValueTask<ApiResult> SendGroupAnnouncementAsync(
        GroupId           groupId,
        string            content,
        string?           imageUri = null,
        CancellationToken ct       = default) =>
        default!;

    public ValueTask<ApiResult> DeleteGroupAnnouncementAsync(GroupId groupId, string announcementId, CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> KickGroupMemberAsync(
        GroupId           groupId,
        UserId            userId,
        bool              rejectFuture = false,
        CancellationToken ct           = default) =>
        default!;

    public ValueTask<ApiResult> MuteGroupMemberAsync(
        GroupId           groupId,
        UserId            userId,
        int               durationSeconds,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> MuteGroupAllAsync(GroupId   groupId, bool enable, CancellationToken ct = default) => default!;
    public ValueTask<ApiResult> LeaveGroupAsync(GroupId     groupId, CancellationToken ct = default) => default!;
    public ValueTask<ApiResult> SendGroupNudgeAsync(GroupId groupId, UserId userId, CancellationToken ct = default) => default!;

    public ValueTask<ApiResult<GroupFilesResult>> GetGroupFilesAsync(
        GroupId           groupId,
        string            parentFolderId = "/",
        CancellationToken ct             = default) =>
        default!;

    public ValueTask<ApiResult<string>> GetGroupFileDownloadUrlAsync(GroupId groupId, string fileId, CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult<string>> GetPrivateFileDownloadUrlAsync(
        UserId            userId,
        string            fileId,
        string            fileHash,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult<string>> CreateGroupFolderAsync(GroupId groupId, string folderName, CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult<string>> UploadGroupFileAsync(
        GroupId           groupId,
        string            fileUri,
        string            fileName,
        string            parentFolderId = "/",
        CancellationToken ct             = default) =>
        default!;

    public ValueTask<ApiResult<string>> UploadPrivateFileAsync(
        UserId            userId,
        string            fileUri,
        string            fileName,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> DeleteGroupFileAsync(GroupId   groupId, string fileId,   CancellationToken ct = default) => default!;
    public ValueTask<ApiResult> DeleteGroupFolderAsync(GroupId groupId, string folderId, CancellationToken ct = default) => default!;

    public ValueTask<ApiResult> MoveGroupFileAsync(
        GroupId           groupId,
        string            fileId,
        string            parentFolderId,
        string            targetFolderId,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> RenameGroupFileAsync(
        GroupId           groupId,
        string            fileId,
        string            parentFolderId,
        string            newFileName,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> RenameGroupFolderAsync(
        GroupId           groupId,
        string            folderId,
        string            newName,
        CancellationToken ct = default) =>
        default!;

    public ValueTask<ApiResult> SetAvatarAsync(string       uri,      CancellationToken ct = default) => default!;
    public ValueTask<ApiResult> SetBioAsync(string          bio,      CancellationToken ct = default) => default!;
    public ValueTask<ApiResult> SetNicknameAsync(string     nickname, CancellationToken ct = default) => default!;
    public ValueTask<ApiResult> SendProfileLikeAsync(UserId userId,   int count = 1, CancellationToken ct = default) => default!;

    public ValueTask<ApiResult> SendGroupMessageReactionAsync(
        GroupId           groupId,
        MessageId         messageSeq,
        string            faceId,
        bool              isAdd        = true,
        string            reactionType = "face",
        CancellationToken ct           = default) =>
        default!;
}

/// <summary>Tests for <see cref="CommandManager" /> re-entry protection.</summary>
[Collection("Command.Unit")]
[Trait("Category", "Unit")]
public class CommandReentryTests : IDisposable
{
    private readonly CommandManager _manager = new();

    public CommandReentryTests()
    {
        ReentryCommands.Reset();
        _manager.ScanType(typeof(ReentryCommands));
    }

    public void Dispose() => ReentryCommands.Reset();

    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

#region Re-entry Protection Tests

    /// <see cref="CommandAttribute.PreventReentry" />
    [Fact]
    public async Task PreventReentry_BlocksSecondInvocation_SameUser()
    {
        MessageReceivedEvent evt1     = CreateTestEvent("slow");
        Task                 firstRun = _manager.HandleMessageEventAsync(evt1, CT).AsTask();

        await ReentryCommands.Started!.Task.WaitAsync(TimeSpan.FromSeconds(5), CT);

        // Same user triggers again — should be blocked
        MessageReceivedEvent evt2 = CreateTestEvent("slow");
        await _manager.HandleMessageEventAsync(evt2, CT);

        ReentryCommands.Gate!.TrySetResult();
        await firstRun.WaitAsync(TimeSpan.FromSeconds(5), CT);

        Assert.Equal(1, ReentryCommands.ExecutionCount);
    }

    /// <see cref="CommandAttribute.PreventReentry" />
    [Fact]
    public async Task PreventReentry_AllowsDifferentUser()
    {
        MessageReceivedEvent evt1     = CreateTestEvent("slow");
        Task                 firstRun = _manager.HandleMessageEventAsync(evt1, CT).AsTask();

        await ReentryCommands.Started!.Task.WaitAsync(TimeSpan.FromSeconds(5), CT);

        // Reset Started for the second user
        ReentryCommands.Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Different user triggers the same command — should be allowed
        MessageReceivedEvent evt2      = CreateTestEvent("slow", 300L);
        Task                 secondRun = _manager.HandleMessageEventAsync(evt2, CT).AsTask();

        await ReentryCommands.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), CT);

        ReentryCommands.Gate!.TrySetResult();
        await Task.WhenAll(firstRun, secondRun).WaitAsync(TimeSpan.FromSeconds(5), CT);

        Assert.Equal(2, ReentryCommands.ExecutionCount);
    }

    /// <see cref="CommandAttribute.PreventReentry" />
    [Fact]
    public async Task PreventReentry_Disabled_AllowsReentry()
    {
        // "fast" command does NOT have PreventReentry
        ReentryCommands.Gate    = null;
        ReentryCommands.Started = null;

        MessageReceivedEvent evt1 = CreateTestEvent("fast");
        await _manager.HandleMessageEventAsync(evt1, CT);

        MessageReceivedEvent evt2 = CreateTestEvent("fast");
        await _manager.HandleMessageEventAsync(evt2, CT);

        Assert.Equal(2, ReentryCommands.ExecutionCount);
    }

    /// <see cref="CommandAttribute.PreventReentry" />
    [Fact]
    public async Task PreventReentry_ReleasesAfterCompletion()
    {
        // First run completes immediately
        ReentryCommands.Gate!.TrySetResult();
        MessageReceivedEvent evt1 = CreateTestEvent("slow");
        await _manager.HandleMessageEventAsync(evt1, CT);

        // Reset for the second run
        ReentryCommands.Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ReentryCommands.Gate    = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ReentryCommands.Gate.TrySetResult();

        // Same user triggers again after first one completed — should succeed
        MessageReceivedEvent evt2 = CreateTestEvent("slow");
        await _manager.HandleMessageEventAsync(evt2, CT);

        Assert.Equal(2, ReentryCommands.ExecutionCount);
    }

    /// <see cref="CommandAttribute.PreventReentry" />
    [Fact]
    public async Task PreventReentry_SameUserDifferentCommand_Allowed()
    {
        // Capture the original gate before modifying static fields
        TaskCompletionSource originalGate = ReentryCommands.Gate!;

        MessageReceivedEvent evt1     = CreateTestEvent("slow");
        Task                 firstRun = _manager.HandleMessageEventAsync(evt1, CT).AsTask();

        await ReentryCommands.Started!.Task.WaitAsync(TimeSpan.FromSeconds(5), CT);

        // Same user triggers a DIFFERENT command — should be allowed
        ReentryCommands.Started = null;
        ReentryCommands.Gate    = null;
        MessageReceivedEvent evt2 = CreateTestEvent("fast");
        await _manager.HandleMessageEventAsync(evt2, CT);

        // Release the slow command via the original gate
        originalGate.TrySetResult();
        await firstRun.WaitAsync(TimeSpan.FromSeconds(5), CT);

        // Both commands executed: slow (1) + fast (1) = 2
        Assert.Equal(2, ReentryCommands.ExecutionCount);
    }

#endregion

#region ReentryMessage Tests

    /// <see cref="CommandAttribute.ReentryMessage" />
    [Fact]
    public async Task ReentryMessage_SendsReplyWhenBlocked()
    {
        RecordingBotApi api = new();

        MessageReceivedEvent evt1     = CreateTestEvent("slow-msg", api: api);
        Task                 firstRun = _manager.HandleMessageEventAsync(evt1, CT).AsTask();

        await ReentryCommands.Started!.Task.WaitAsync(TimeSpan.FromSeconds(5), CT);

        // Same user triggers again — should be blocked and reply sent
        MessageReceivedEvent evt2 = CreateTestEvent("slow-msg", api: api);
        await _manager.HandleMessageEventAsync(evt2, CT);

        // Allow fire-and-forget reply to complete
        await Task.Delay(200, CT);

        ReentryCommands.Gate!.TrySetResult();
        await firstRun.WaitAsync(TimeSpan.FromSeconds(5), CT);

        Assert.Equal(1, ReentryCommands.ExecutionCount);
        Assert.Single(api.GroupMessages);
        Assert.Equal("请等待上一条指令执行完毕", api.GroupMessages.First().Text);
    }

    /// <see cref="CommandAttribute.ReentryMessage" />
    [Fact]
    public async Task ReentryMessage_NoReplyWhenMessageEmpty()
    {
        RecordingBotApi api = new();

        MessageReceivedEvent evt1     = CreateTestEvent("slow", api: api);
        Task                 firstRun = _manager.HandleMessageEventAsync(evt1, CT).AsTask();

        await ReentryCommands.Started!.Task.WaitAsync(TimeSpan.FromSeconds(5), CT);

        // Same user triggers again — blocked but NO reply (ReentryMessage is empty)
        MessageReceivedEvent evt2 = CreateTestEvent("slow", api: api);
        await _manager.HandleMessageEventAsync(evt2, CT);

        await Task.Delay(200, CT);

        ReentryCommands.Gate!.TrySetResult();
        await firstRun.WaitAsync(TimeSpan.FromSeconds(5), CT);

        Assert.Equal(1, ReentryCommands.ExecutionCount);
        Assert.Empty(api.GroupMessages);
    }

#endregion

#region Dynamic Command Re-entry Tests

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task DynamicCommand_PreventReentry_Works()
    {
        CommandManager       manager   = new();
        TaskCompletionSource started   = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource gate      = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int                  execCount = 0;

        manager.RegisterDynamicCommand(
            async _ =>
            {
                Interlocked.Increment(ref execCount);
                started.TrySetResult();
                await gate.Task;
            },
                ["dyntest"],
            preventReentry: true);

        MessageReceivedEvent evt1     = CreateTestEvent("dyntest");
        Task                 firstRun = manager.HandleMessageEventAsync(evt1, CT).AsTask();

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), CT);

        MessageReceivedEvent evt2 = CreateTestEvent("dyntest");
        await manager.HandleMessageEventAsync(evt2, CT);

        gate.TrySetResult();
        await firstRun.WaitAsync(TimeSpan.FromSeconds(5), CT);

        Assert.Equal(1, execCount);
    }

#endregion

#region Helpers

    private static MessageReceivedEvent CreateTestEvent(
        string   text,
        long     senderId = 200L,
        long     groupId  = 100L,
        IBotApi? api      = null) =>
        new()
            {
                Api          = api ?? null!,
                ConnectionId = Guid.Empty,
                SelfId       = 1L,
                Time         = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = groupId,
                        SenderId   = senderId,
                        Body       = new MessageBody(text)
                    },
                Member = new GroupMemberInfo { UserId = senderId, GroupId = groupId, Role = MemberRole.Member }
            };

#endregion
}