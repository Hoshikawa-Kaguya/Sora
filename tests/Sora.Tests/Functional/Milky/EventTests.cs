using System.Text;
using Xunit;

namespace Sora.Tests.Functional.Milky;

/// <summary>Functional event tests for the Milky adapter.</summary>
[Collection("Milky.Functional")]
[Trait("Category", "Functional")]
public class EventTests : IDisposable
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    private readonly MilkyTestFixture  _fixture;
    private readonly IDisposable       _logSubscription;
    private readonly ITestOutputHelper _output;
    private          IBotApi           Api      => _fixture.Api!;
    private          IMilkyExtApi      MilkyExt => Api.GetExtension<IMilkyExtApi>()!;

    /// <summary>EventTests ctor.</summary>
    public EventTests(MilkyTestFixture fixture, ITestOutputHelper output)
    {
        _fixture         = fixture;
        _output          = output;
        _logSubscription = fixture.OutputSink.Subscribe(output.WriteLine);
    }

#region Message Event Tests

    /// <see cref="EventDispatcher.OnMessageReceived" />
    [Fact]
    public async Task Event_MessageReceived()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryService is null, "Secondary service not available");

        TaskCompletionSource<MessageReceivedEvent> tcs = new();
        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.SecondaryService!.Events.OnMessageReceived += handler;

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            SendMessageResult sent =
                await Api.SendGroupMessageAsync(
                    testGroup,
                    new MessageBody("[Milky Event Test] message_received trigger"),
                    CT);
            _output.WriteLine($"Sent: success={sent.IsSuccess} messageId={sent.MessageId}");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(
                tcs.Task.IsCompletedSuccessfully,
                "OnMessageReceived not triggered within timeout — protocol should deliver self-sent group messages");
            MessageReceivedEvent evt = await tcs.Task;
            _output.WriteLine($"MessageReceived: text={evt.Message.Body.GetText()} senderId={evt.Message.SenderId}");
            Assert.NotNull(evt.Message);
        }
        finally
        {
            _fixture.SecondaryService!.Events.OnMessageReceived -= handler;
        }
    }

    /// <see cref="EventDispatcher.OnMessageReceived" />
    [Fact]
    public async Task Event_MessageReceived_FromSecondary()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        TaskCompletionSource<MessageReceivedEvent> tcs = new();
        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnMessageReceived += handler;

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;

            await Task.Delay(1000, CT);
            SendMessageResult sent =
                await _fixture.SecondaryApi.SendGroupMessageAsync(
                    testGroup,
                    new MessageBody("[Milky Event Test] message_received from secondary"),
                    CT);
            _output.WriteLine($"Secondary sent: success={sent.IsSuccess} messageId={sent.MessageId}");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(
                tcs.Task.IsCompletedSuccessfully,
                "OnMessageReceived not triggered within timeout — protocol should deliver group messages from other bots");
            MessageReceivedEvent evt = await tcs.Task;
            _output.WriteLine($"MessageReceived: text={evt.Message.Body.GetText()} senderId={evt.Message.SenderId}");
            Assert.NotNull(evt.Message);
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
        }
    }

    /// <see cref="EventDispatcher.OnMessageDeleted" />
    [Fact]
    public async Task Event_MessageDeleted()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryService is null, "Secondary service not available");

        TaskCompletionSource<MessageDeletedEvent> tcs = new();
        Func<MessageDeletedEvent, ValueTask> handler = e =>
        {
            tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.SecondaryService!.Events.OnMessageDeleted += handler;

        try
        {
            GroupId           testGroup = TestConfig.TestGroupId;
            SendMessageResult sent      = await Api.SendGroupMessageAsync(testGroup, "[Milky Event Test] will be recalled", CT);
            Assert.True(sent.IsSuccess);
            _output.WriteLine($"Sent messageId={sent.MessageId}");

            await Task.Delay(1000, CT);
            ApiResult recallResult = await Api.RecallGroupMessageAsync(testGroup, sent.MessageId, CT);
            _output.WriteLine($"Recall: success={recallResult.IsSuccess}");
            Assert.True(recallResult.IsSuccess, "RecallGroupMessageAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "OnMessageDeleted not triggered within timeout");
            MessageDeletedEvent evt = await tcs.Task;
            _output.WriteLine($"MessageDeleted: messageId={evt.MessageId} senderId={evt.SenderId} operatorId={evt.OperatorId}");
            Assert.Equal(sent.MessageId, evt.MessageId);
        }
        finally
        {
            _fixture.SecondaryService!.Events.OnMessageDeleted -= handler;
        }
    }

#endregion

#region Dual-Bot Event Tests

    /// <see cref="EventDispatcher.OnNudge" />
    [Fact]
    public async Task Event_GroupNudge_FromSecondary()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        BotIdentity primarySelf = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

        TaskCompletionSource<NudgeEvent> tcs = new();
        Func<NudgeEvent, ValueTask> handler = e =>
        {
            tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnNudge += handler;

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;

            await Task.Delay(1000, CT);
            ApiResult nudgeResult = await _fixture.SecondaryApi.SendGroupNudgeAsync(testGroup, primarySelf.UserId, CT);
            _output.WriteLine($"Secondary SendGroupNudge: success={nudgeResult.IsSuccess}");
            Assert.True(nudgeResult.IsSuccess, "SendGroupNudgeAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(8000, CT));
            Assert.True(
                tcs.Task.IsCompletedSuccessfully,
                "OnNudge not triggered within timeout — protocol should deliver group nudge events");
            NudgeEvent evt = await tcs.Task;
            _output.WriteLine($"Nudge: senderId={evt.SenderId} receiverId={evt.ReceiverId} action={evt.ActionText}");
            Assert.Equal(_fixture.SecondaryUserId, evt.SenderId);
            Assert.Equal(primarySelf.UserId, evt.ReceiverId);
        }
        finally
        {
            _fixture.Service.Events.OnNudge -= handler;
        }
    }

    /// <see cref="EventDispatcher.OnFileUpload" />
    [Fact]
    public async Task Event_FileUpload_DualBot()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        TaskCompletionSource<FileUploadEvent> tcs = new();
        Func<FileUploadEvent, ValueTask> handler = e =>
        {
            tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnFileUpload += handler;

        try
        {
            GroupId testGroup   = TestConfig.TestGroupId;
            string  fileContent = Convert.ToBase64String(Encoding.UTF8.GetBytes("dual-bot upload test content"));
            string  base64Uri   = $"base64://{fileContent}";

            await Task.Delay(1000, CT);
            ApiResult<string> uploadResult =
                await _fixture.SecondaryApi.UploadGroupFileAsync(testGroup, base64Uri, "secondary_upload_test.txt", ct: CT);
            _output.WriteLine($"Secondary UploadGroupFile: success={uploadResult.IsSuccess}");
            Assert.True(uploadResult.IsSuccess, "UploadGroupFileAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "FileUpload event not received within timeout");
            FileUploadEvent evt = await tcs.Task;
            _output.WriteLine($"FileUpload: fileName={evt.FileName} size={evt.FileSize} userId={evt.UserId}");
            Assert.True(evt.FileSize > 0, "FileSize should be greater than 0");
        }
        finally
        {
            _fixture.Service.Events.OnFileUpload -= handler;
        }
    }

    /// <see cref="EventDispatcher.OnGroupReaction" />
    [Fact]
    public async Task Event_GroupReaction_DualBot()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId           testGroup = TestConfig.TestGroupId;
        SendMessageResult sent      = await Api.SendGroupMessageAsync(testGroup, "[Milky Event Test] reaction target", CT);
        Assert.True(sent.IsSuccess);
        _output.WriteLine($"Sent messageId={sent.MessageId}");

        TaskCompletionSource<GroupReactionEvent> tcs = new();
        Func<GroupReactionEvent, ValueTask> handler = e =>
        {
            tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnGroupReaction += handler;

        try
        {
            await Task.Delay(1000, CT);
            ApiResult reactionResult =
                await _fixture.SecondaryApi.SendGroupMessageReactionAsync(testGroup, sent.MessageId, "1", ct: CT);
            _output.WriteLine($"Secondary SendReaction: success={reactionResult.IsSuccess}");
            Assert.True(reactionResult.IsSuccess, "SendGroupMessageReactionAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "GroupReaction event not received within timeout");
            GroupReactionEvent evt = await tcs.Task;
            _output.WriteLine($"GroupReaction: groupId={evt.GroupId} userId={evt.UserId} faceId={evt.FaceId} isAdd={evt.IsAdd}");
            Assert.Equal(testGroup, evt.GroupId);
            Assert.True(evt.IsAdd);
        }
        finally
        {
            _fixture.Service.Events.OnGroupReaction -= handler;
        }
    }

    /// <see cref="EventDispatcher.OnGroupAdminChanged" />
    [Fact]
    public async Task Event_GroupAdminChanged_DualBot()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryService is null, "Secondary service not available");

        TaskCompletionSource<GroupAdminChangedEvent> tcs = new();
        Func<GroupAdminChangedEvent, ValueTask> handler = e =>
        {
            tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.SecondaryService.Events.OnGroupAdminChanged += handler;

        GroupId testGroup = TestConfig.TestGroupId;

        try
        {
            await Task.Delay(1000, CT);
            ApiResult setResult = await Api.SetGroupAdminAsync(testGroup, _fixture.SecondaryUserId, true, CT);
            _output.WriteLine($"SetGroupAdmin(true): success={setResult.IsSuccess}");
            Assert.SkipWhen(!setResult.IsSuccess, "SetGroupAdmin failed — bot may not be group owner");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "GroupAdminChanged event not received within timeout");
            GroupAdminChangedEvent evt = await tcs.Task;
            _output.WriteLine($"GroupAdminChanged: groupId={evt.GroupId} userId={evt.UserId} isSet={evt.IsSet}");
            Assert.Equal(testGroup, evt.GroupId);
            Assert.Equal(_fixture.SecondaryUserId, evt.UserId);
            Assert.True(evt.IsSet);
        }
        finally
        {
            try
            {
                await Api.SetGroupAdminAsync(testGroup, _fixture.SecondaryUserId, false, CT);
            }
            catch
            {
                /* best-effort */
            }

            _fixture.SecondaryService.Events.OnGroupAdminChanged -= handler;
        }
    }

#endregion

#region Group Notice Event Tests

    /// <see cref="EventDispatcher.OnGroupEssenceChanged" />
    [Fact]
    public async Task Event_GroupEssenceChanged()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryService is null, "Secondary service not available");

        TaskCompletionSource<GroupEssenceChangedEvent> tcs = new();
        Func<GroupEssenceChangedEvent, ValueTask> handler = e =>
        {
            tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.SecondaryService!.Events.OnGroupEssenceChanged += handler;

        GroupId           testGroup = TestConfig.TestGroupId;
        SendMessageResult sent      = await Api.SendGroupMessageAsync(testGroup, "[Milky Event Test] essence candidate", CT);
        Assert.True(sent.IsSuccess);
        _output.WriteLine($"Sent messageId={sent.MessageId}");

        try
        {
            await Task.Delay(1000, CT);
            ApiResult setResult = await Api.SetGroupEssenceMessageAsync(testGroup, sent.MessageId, ct: CT);
            _output.WriteLine($"SetEssence: success={setResult.IsSuccess}");
            Assert.True(setResult.IsSuccess, "SetGroupEssenceMessageAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "OnGroupEssenceChanged not triggered within timeout");
            GroupEssenceChangedEvent evt = await tcs.Task;
            _output.WriteLine($"GroupEssenceChanged: messageId={evt.MessageId} isSet={evt.IsSet} operatorId={evt.OperatorId}");
            Assert.Equal(testGroup, evt.GroupId);
            Assert.True(evt.IsSet);
        }
        finally
        {
            try
            {
                await Api.SetGroupEssenceMessageAsync(testGroup, sent.MessageId, false, CT);
            }
            catch
            {
                /* best-effort */
            }

            _fixture.SecondaryService!.Events.OnGroupEssenceChanged -= handler;
        }
    }

    /// <see cref="EventDispatcher.OnGroupMute" />
    [Fact]
    public async Task Event_GroupMute()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryService is null, "Secondary service not available");

        TaskCompletionSource<GroupMuteEvent> tcs = new();
        Func<GroupMuteEvent, ValueTask> handler = e =>
        {
            tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.SecondaryService!.Events.OnGroupMute += handler;

        GroupId testGroup = TestConfig.TestGroupId;

        try
        {
            ApiResult muteResult = await Api.MuteGroupAllAsync(testGroup, true, CT);
            _output.WriteLine($"MuteAll: success={muteResult.IsSuccess}");
            Assert.True(muteResult.IsSuccess, "MuteGroupAllAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "OnGroupMute not triggered within timeout");
            GroupMuteEvent evt = await tcs.Task;
            _output.WriteLine($"GroupMute: groupId={evt.GroupId} isWholeGroup={evt.IsWholeGroup} duration={evt.DurationSeconds}");
            Assert.Equal(testGroup, evt.GroupId);
            Assert.True(evt.IsWholeGroup);
        }
        finally
        {
            try
            {
                await Api.MuteGroupAllAsync(testGroup, false, CT);
            }
            catch
            {
                /* best-effort */
            }

            _fixture.SecondaryService!.Events.OnGroupMute -= handler;
        }
    }

    /// <see cref="EventDispatcher.OnGroupNameChanged" />
    [Fact]
    public async Task Event_GroupNameChanged()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryService is null, "Secondary service not available");

        GroupId   testGroup    = TestConfig.TestGroupId;
        GroupInfo originalInfo = (await Api.GetGroupInfoAsync(testGroup, ct: CT)).AssertSuccess();
        string    originalName = originalInfo.GroupName;
        _output.WriteLine($"Original name: {originalName}");

        string uniqueTag = Guid.NewGuid().ToString("N")[..8];
        string tempName  = $"[Milky NameTest] {uniqueTag}";

        TaskCompletionSource<GroupNameChangedEvent> tcs = new();
        Func<GroupNameChangedEvent, ValueTask> handler = e =>
        {
            if (e.NewName.Contains(uniqueTag))
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.SecondaryService!.Events.OnGroupNameChanged += handler;

        try
        {
            ApiResult setResult = await Api.SetGroupNameAsync(testGroup, tempName, CT);
            _output.WriteLine($"SetName: success={setResult.IsSuccess} tempName={tempName}");
            Assert.True(setResult.IsSuccess, "SetGroupNameAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(
                tcs.Task.IsCompletedSuccessfully,
                "OnGroupNameChanged not triggered within timeout — protocol should deliver group name change events");
            GroupNameChangedEvent evt = await tcs.Task;
            _output.WriteLine($"GroupNameChanged: newName={evt.NewName} operatorId={evt.OperatorId}");
            Assert.Contains(uniqueTag, evt.NewName);
        }
        finally
        {
            _fixture.SecondaryService!.Events.OnGroupNameChanged -= handler;
            try
            {
                ApiResult restore = await Api.SetGroupNameAsync(testGroup, originalName, CT);
                _output.WriteLine($"Restore group name: success={restore.IsSuccess}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Restore group name failed: {ex.Message}");
            }
        }
    }

    /// <see cref="EventDispatcher.OnNudge" />
    [Fact]
    public async Task Event_GroupNudge()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryService is null, "Secondary service not available");

        TaskCompletionSource<NudgeEvent> tcs = new();
        Func<NudgeEvent, ValueTask> handler = e =>
        {
            tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.SecondaryService!.Events.OnNudge += handler;

        try
        {
            BotIdentity self = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

            GroupId   testGroup   = TestConfig.TestGroupId;
            ApiResult nudgeResult = await Api.SendGroupNudgeAsync(testGroup, self.UserId, CT);
            _output.WriteLine($"SendGroupNudge: success={nudgeResult.IsSuccess}");
            Assert.True(nudgeResult.IsSuccess, "SendGroupNudgeAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "OnNudge not triggered within timeout");
            NudgeEvent evt = await tcs.Task;
            _output.WriteLine($"Nudge: senderId={evt.SenderId} receiverId={evt.ReceiverId} action={evt.ActionText}");
            Assert.Equal(self.UserId, evt.SenderId);
            Assert.Equal(self.UserId, evt.ReceiverId);
        }
        finally
        {
            _fixture.SecondaryService!.Events.OnNudge -= handler;
        }
    }

    /// <see cref="EventDispatcher.OnPeerPinChanged" />
    [Fact]
    public async Task Event_PeerPinChanged()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(TestConfig.TestGroupId == 0, "SORA_TEST_GROUP_ID not set");

        GroupId                                   testGroup = TestConfig.TestGroupId;
        TaskCompletionSource<PeerPinChangedEvent> tcs       = new();

        Func<PeerPinChangedEvent, ValueTask> handler = evt =>
        {
            tcs.TrySetResult(evt);
            return ValueTask.CompletedTask;
        };

        _fixture.Service!.Events.OnPeerPinChanged += handler;
        try
        {
            ApiResult pinResult = await MilkyExt.SetPeerPinAsync(MessageSourceType.Group, testGroup, true, CT);
            _output.WriteLine($"SetPeerPin: code={pinResult.Code}");
            Assert.True(pinResult.IsSuccess, "SetPeerPinAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "PeerPinChanged event not received within timeout");
            _output.WriteLine("PeerPinChanged event received");
        }
        finally
        {
            try
            {
                await MilkyExt.SetPeerPinAsync(MessageSourceType.Group, testGroup, false, CT);
            }
            catch
            {
                /* best-effort */
            }

            _fixture.Service!.Events.OnPeerPinChanged -= handler;
        }
    }

#endregion

#region Friend Event Tests

    /// <see cref="EventDispatcher.OnNudge" />
    [Fact]
    public async Task Event_FriendNudge_FromSecondary()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryService is null, "Secondary service not available");

        BotIdentity primarySelf   = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
        UserId      primaryUserId = primarySelf.UserId;

        TaskCompletionSource<NudgeEvent> tcs = new();
        Func<NudgeEvent, ValueTask> handler = e =>
        {
            if (e.SourceType == MessageSourceType.Friend)
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.SecondaryService.Events.OnNudge += handler;

        try
        {
            await Task.Delay(4000, CT);
            ApiResult nudgeResult = await _fixture.SecondaryApi.SendFriendNudgeAsync(primaryUserId, CT);
            _output.WriteLine($"Secondary SendFriendNudge: success={nudgeResult.IsSuccess}");
            Assert.True(nudgeResult.IsSuccess, "SendFriendNudgeAsync should succeed");
            await Task.WhenAny(tcs.Task, Task.Delay(10000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "OnNudge (friend) not triggered within timeout");

            NudgeEvent evt = await tcs.Task;
            _output.WriteLine(
                $"FriendNudge: senderId={evt.SenderId} receiverId={evt.ReceiverId} sourceType={evt.SourceType} action={evt.ActionText}");
            Assert.Equal(_fixture.SecondaryUserId, evt.SenderId);
            Assert.Equal(primaryUserId, evt.ReceiverId);
            Assert.Equal(MessageSourceType.Friend, evt.SourceType);
        }
        finally
        {
            _fixture.SecondaryService.Events.OnNudge -= handler;
        }
    }

    /// <see cref="EventDispatcher.OnFileUpload" />
    [Fact]
    public async Task Event_FriendFileUpload_DualBot()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        BotIdentity primarySelf   = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
        UserId      primaryUserId = primarySelf.UserId;

        TaskCompletionSource<FileUploadEvent> tcs = new();
        Func<FileUploadEvent, ValueTask> handler = e =>
        {
            if (e.SourceType == MessageSourceType.Friend)
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnFileUpload += handler;

        try
        {
            const string testFileName = "sora_friend_upload_test.txt";
            string       fileContent  = Convert.ToBase64String(Encoding.UTF8.GetBytes("Milky friend file upload test"));
            string       base64Uri    = $"base64://{fileContent}";

            await Task.Delay(1000, CT);
            ApiResult<string> uploadResult =
                await _fixture.SecondaryApi.UploadPrivateFileAsync(primaryUserId, base64Uri, testFileName, CT);
            _output.WriteLine($"Secondary UploadPrivateFile: success={uploadResult.IsSuccess}");
            Assert.True(uploadResult.IsSuccess, "UploadPrivateFileAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(10000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "OnFileUpload (friend) not triggered within timeout");

            FileUploadEvent evt = await tcs.Task;
            _output.WriteLine(
                $"FriendFileUpload: fileName={evt.FileName} size={evt.FileSize} userId={evt.UserId} sourceType={evt.SourceType}");
            Assert.Equal(MessageSourceType.Friend, evt.SourceType);
            Assert.Equal(_fixture.SecondaryUserId, evt.UserId);
            Assert.Equal(testFileName, evt.FileName);
            Assert.True(evt.FileSize > 0, "FileSize should be greater than 0");
        }
        finally
        {
            _fixture.Service.Events.OnFileUpload -= handler;
        }
    }

#endregion

    /// <inheritdoc />
    public void Dispose() => _logSubscription.Dispose();
}