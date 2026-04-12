using Xunit;

namespace Sora.Tests.Functional.OneBot11;

/// <summary>Functional event tests for the OneBot11 adapter.</summary>
[Collection("OneBot11.Functional")]
[Trait("Category", "Functional")]
public class EventTests : IDisposable
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    private readonly OneBot11TestFixture _fixture;
    private readonly IDisposable         _logSubscription;
    private readonly ITestOutputHelper   _output;
    private          IBotApi             Api => _fixture.Api!;

    /// <summary>Initializes a new instance of the <see cref="EventTests" /> class.</summary>
    public EventTests(OneBot11TestFixture fixture, ITestOutputHelper output)
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
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
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
                    new MessageBody("[OB11 Event Test] message_received trigger"),
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
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
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
                    new MessageBody("[OB11 Event Test] from secondary bot"),
                    CT);
            _output.WriteLine($"Secondary sent: success={sent.IsSuccess} messageId={sent.MessageId}");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(
                tcs.Task.IsCompletedSuccessfully,
                "OnMessageReceived not triggered within timeout — protocol should deliver group messages from other bots");
            MessageReceivedEvent evt = await tcs.Task;
            _output.WriteLine($"MessageReceived: text={evt.Message.Body.GetText()} senderId={evt.Message.SenderId}");
            Assert.NotNull(evt.Message);
            Assert.Equal(_fixture.SecondaryUserId, evt.Message.SenderId);
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
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
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
            SendMessageResult sent      = await Api.SendGroupMessageAsync(testGroup, "[OB11 Event Test] will be recalled", CT);
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

#region Group Notice Event Tests

    /// <see cref="EventDispatcher.OnGroupAdminChanged" />
    [Fact]
    public async Task Event_GroupAdminChanged_Automated()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryService is null, "SecondaryService not available");

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
            ApiResult setResult = await Api.SetGroupAdminAsync(testGroup, _fixture.SecondaryUserId, true, CT);
            _output.WriteLine($"SetAdmin: success={setResult.IsSuccess}");
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

            _fixture.SecondaryService!.Events.OnGroupAdminChanged -= handler;
        }
    }

    /// <see cref="EventDispatcher.OnGroupMute" />
    [Fact]
    public async Task Event_GroupMute()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
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
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryService is null, "Secondary service not available");

        GroupId   testGroup     = TestConfig.TestGroupId;
        GroupInfo originalGroup = (await Api.GetGroupInfoAsync(testGroup, ct: CT)).AssertSuccess();
        string    originalName  = originalGroup.GroupName;
        _output.WriteLine($"Original name: {originalName}");

        string uniqueTag = Guid.NewGuid().ToString("N")[..8];
        string tempName  = $"[OB11 NameTest] {uniqueTag}";

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
            await Task.Delay(1000, CT);
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
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
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
            BotIdentity selfInfo = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

            GroupId   testGroup   = TestConfig.TestGroupId;
            ApiResult nudgeResult = await Api.SendGroupNudgeAsync(testGroup, selfInfo.UserId, CT);
            _output.WriteLine($"SendGroupNudge: success={nudgeResult.IsSuccess}");
            Assert.True(nudgeResult.IsSuccess, "SendGroupNudgeAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "OnNudge not triggered within timeout");
            NudgeEvent evt = await tcs.Task;
            _output.WriteLine($"Nudge: senderId={evt.SenderId} receiverId={evt.ReceiverId} action={evt.ActionText}");
            Assert.Equal(selfInfo.UserId, evt.SenderId);
            Assert.Equal(selfInfo.UserId, evt.ReceiverId);
        }
        finally
        {
            _fixture.SecondaryService!.Events.OnNudge -= handler;
        }
    }

    /// <see cref="EventDispatcher.OnNudge" />
    [Fact]
    public async Task Event_GroupNudge_FromSecondary()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        TaskCompletionSource<NudgeEvent> tcs = new();
        Func<NudgeEvent, ValueTask> handler = e =>
        {
            tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnNudge += handler;

        try
        {
            BotIdentity selfInfo = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

            GroupId testGroup = TestConfig.TestGroupId;

            await Task.Delay(1000, CT);
            ApiResult nudgeResult =
                await _fixture.SecondaryApi.SendGroupNudgeAsync(testGroup, selfInfo.UserId, CT);
            _output.WriteLine($"Secondary SendGroupNudge: success={nudgeResult.IsSuccess}");
            Assert.True(nudgeResult.IsSuccess, "SendGroupNudgeAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "OnNudge (from secondary) not triggered within timeout");
            NudgeEvent evt = await tcs.Task;
            _output.WriteLine($"Nudge: senderId={evt.SenderId} receiverId={evt.ReceiverId} action={evt.ActionText}");
            // OB11 protocol may report senderId differently for nudge events
            Assert.True(evt.SenderId.Value > 0, "SenderId should be a valid user ID");
            Assert.True(evt.ReceiverId.Value > 0, "ReceiverId should be a valid user ID");
        }
        finally
        {
            _fixture.Service.Events.OnNudge -= handler;
        }
    }

    /// <see cref="EventDispatcher.OnFileUpload" />
    [Fact]
    public async Task Event_FileUpload_FromSecondary()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
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
            GroupId testGroup = TestConfig.TestGroupId;
            string  base64Uri = "base64://U29yYSBPQjExIGR1YWwtYm90IGV2ZW50IHRlc3QgZmlsZQ==";

            await Task.Delay(1000, CT);
            ApiResult<string> uploadResult =
                await _fixture.SecondaryApi.UploadGroupFileAsync(testGroup, base64Uri, "ob11_secondary_test.txt", ct: CT);
            _output.WriteLine($"Secondary UploadGroupFile: success={uploadResult.IsSuccess}");
            Assert.True(uploadResult.IsSuccess, "UploadGroupFileAsync should succeed");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "OnFileUpload (from secondary) not triggered within timeout");
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
    public async Task Event_GroupReaction_FromSecondary()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId           testGroup = TestConfig.TestGroupId;
        SendMessageResult sent      = await Api.SendGroupMessageAsync(testGroup, "[OB11 Event Test] reaction target", CT);
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
            Assert.True(tcs.Task.IsCompletedSuccessfully, "OnGroupReaction (from secondary) not triggered within timeout");
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

#endregion

#region Group Essence Event Tests

    /// <see cref="EventDispatcher.OnGroupEssenceChanged" />
    [Fact]
    public async Task Event_GroupEssenceChanged()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryService is null, "Secondary service not available");

        GroupId           testGroup = TestConfig.TestGroupId;
        SendMessageResult sent      = await Api.SendGroupMessageAsync(testGroup, "[OB11 Event Test] essence candidate", CT);
        Assert.True(sent.IsSuccess);
        _output.WriteLine($"Sent messageId={sent.MessageId}");

        TaskCompletionSource<GroupEssenceChangedEvent> tcs = new();
        Func<GroupEssenceChangedEvent, ValueTask> handler = e =>
        {
            if (e.MessageId == sent.MessageId)
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.SecondaryService!.Events.OnGroupEssenceChanged += handler;

        try
        {
            await Task.Delay(1000, CT);
            ApiResult setResult = await Api.SetGroupEssenceMessageAsync(testGroup, sent.MessageId, ct: CT);
            _output.WriteLine($"SetEssence: success={setResult.IsSuccess}");
            Assert.SkipWhen(!setResult.IsSuccess, "SetGroupEssenceMessage not supported by this OB11 implementation");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(
                tcs.Task.IsCompletedSuccessfully,
                "OnGroupEssenceChanged not triggered within timeout — protocol should deliver essence change events");

            GroupEssenceChangedEvent evt = await tcs.Task;
            _output.WriteLine(
                $"GroupEssenceChanged: groupId={evt.GroupId} messageId={evt.MessageId} isSet={evt.IsSet} operatorId={evt.OperatorId}");
            Assert.Equal(testGroup, evt.GroupId);
            Assert.Equal(sent.MessageId, evt.MessageId);
            Assert.True(evt.IsSet);
        }
        finally
        {
            // Best-effort cleanup: remove essence
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

#endregion

    /// <summary>Releases the test output subscription.</summary>
    public void Dispose() => _logSubscription.Dispose();
}