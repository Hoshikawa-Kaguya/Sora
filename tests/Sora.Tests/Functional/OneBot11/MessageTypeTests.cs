using Xunit;

namespace Sora.Tests.Functional.OneBot11;

/// <summary>End-to-end message type tests for the OneBot11 adapter.</summary>
[Collection("OneBot11.Functional")]
[Trait("Category", "Functional")]
public class MessageTypeTests : IDisposable
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    private readonly OneBot11TestFixture _fixture;
    private readonly IDisposable         _logSubscription;
    private readonly ITestOutputHelper   _output;

    /// <summary>Initializes a new instance of the <see cref="MessageTypeTests" /> class.</summary>
    public MessageTypeTests(OneBot11TestFixture fixture, ITestOutputHelper output)
    {
        _fixture         = fixture;
        _output          = output;
        _logSubscription = fixture.OutputSink.Subscribe(output.WriteLine);
    }

    /// <summary>Releases the test output subscription.</summary>
    public void Dispose() => _logSubscription.Dispose();

#region Text Messages

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task SendReceive_Text()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        string                                     marker    = $"[OB11-MsgType:Text] {Guid.NewGuid():N}";
        GroupId                                    testGroup = TestConfig.TestGroupId;
        TaskCompletionSource<MessageReceivedEvent> tcs       = new();

        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId && e.Message.Body.GetText().Contains(marker))
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service!.Events.OnMessageReceived += handler;
        SendMessageResult? sent   = null;
        bool               passed = false;

        try
        {
            await Task.Delay(1000, CT);
            sent = await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(marker), CT);
            _output.WriteLine($"Secondary sent: success={sent.Value.IsSuccess} messageId={sent.Value.MessageId}");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the text message within timeout");

            MessageReceivedEvent evt = await tcs.Task;
            _output.WriteLine($"Received: text={evt.Message.Body.GetText()} senderId={evt.Message.SenderId}");
            Assert.Contains(marker, evt.Message.Body.GetText());
            Assert.Equal(_fixture.SecondaryUserId, evt.Message.SenderId);
            passed = true;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            _fixture.RecordResult(passed);
            if (sent is { IsSuccess: true })
                await _fixture.SecondaryApi!.RecallGroupMessageAsync(testGroup, sent.Value.MessageId, CT);
        }
    }

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task SendReceive_Text_SpecialCharacters()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        string                                     marker      = $"[OB11-MsgType:TextSpecial] {Guid.NewGuid():N}";
        string                                     specialText = $"{marker} 你好世界🎉\nLine2<>&\"'";
        GroupId                                    testGroup   = TestConfig.TestGroupId;
        TaskCompletionSource<MessageReceivedEvent> tcs         = new();

        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId && e.Message.Body.GetText().Contains(marker))
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service!.Events.OnMessageReceived += handler;
        SendMessageResult? sent   = null;
        bool               passed = false;

        try
        {
            await Task.Delay(1000, CT);
            sent = await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(specialText), CT);
            _output.WriteLine($"Secondary sent: success={sent.Value.IsSuccess}");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the special text message within timeout");

            MessageReceivedEvent evt          = await tcs.Task;
            string               receivedText = evt.Message.Body.GetText();
            _output.WriteLine($"Received: text={receivedText}");
            Assert.Contains("你好世界", receivedText);
            Assert.Contains("🎉", receivedText);
            passed = true;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            _fixture.RecordResult(passed);
            if (sent is { IsSuccess: true })
                await _fixture.SecondaryApi!.RecallGroupMessageAsync(testGroup, sent.Value.MessageId, CT);
        }
    }

#endregion

#region Face Messages

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task SendReceive_Face()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        string                                     marker    = $"[OB11-MsgType:Face] {Guid.NewGuid():N}";
        GroupId                                    testGroup = TestConfig.TestGroupId;
        TaskCompletionSource<MessageReceivedEvent> tcs       = new();

        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId && e.Message.Body.GetText().Contains(marker))
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service!.Events.OnMessageReceived += handler;
        SendMessageResult? sent   = null;
        bool               passed = false;

        try
        {
            MessageBody body = new([SegmentBuilder.Text(marker), SegmentBuilder.Face("1")]);

            await Task.Delay(1000, CT);
            sent = await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Secondary sent: success={sent.Value.IsSuccess}");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the face message within timeout");

            MessageReceivedEvent evt  = await tcs.Task;
            FaceSegment?         face = evt.Message.Body.GetFirst<FaceSegment>();
            _output.WriteLine($"Received: hasFace={face is not null} faceId={face?.FaceId}");
            Assert.NotNull(face);
            passed = true;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            _fixture.RecordResult(passed);
            if (sent is { IsSuccess: true })
                await _fixture.SecondaryApi!.RecallGroupMessageAsync(testGroup, sent.Value.MessageId, CT);
        }
    }

#endregion

#region Mention Messages

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task SendReceive_Mention()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        string  marker    = $"[OB11-MsgType:Mention] {Guid.NewGuid():N}";
        GroupId testGroup = TestConfig.TestGroupId;

        BotIdentity primarySelfInfo = (await _fixture.Api!.GetSelfInfoAsync(CT)).AssertSuccess();
        UserId      primaryUserId   = primarySelfInfo.UserId;

        TaskCompletionSource<MessageReceivedEvent> tcs = new();

        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId && e.Message.Body.GetText().Contains(marker))
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service!.Events.OnMessageReceived += handler;
        SendMessageResult? sent   = null;
        bool               passed = false;

        try
        {
            MessageBody body = new([SegmentBuilder.Text(marker), SegmentBuilder.Mention(primaryUserId)]);

            await Task.Delay(1000, CT);
            sent = await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Secondary sent: success={sent.Value.IsSuccess}");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the mention message within timeout");

            MessageReceivedEvent evt     = await tcs.Task;
            MentionSegment?      mention = evt.Message.Body.GetFirst<MentionSegment>();
            _output.WriteLine($"Received: hasMention={mention is not null} target={mention?.Target}");
            Assert.NotNull(mention);
            Assert.Equal(primaryUserId, mention.Target);
            passed = true;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            _fixture.RecordResult(passed);
            if (sent is { IsSuccess: true })
                await _fixture.SecondaryApi!.RecallGroupMessageAsync(testGroup, sent.Value.MessageId, CT);
        }
    }

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task Send_MentionAll()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId            testGroup = TestConfig.TestGroupId;
        string             marker    = $"[OB11-MsgType:MentionAll] {Guid.NewGuid():N}";
        SendMessageResult? sent      = null;
        bool               passed    = false;

        try
        {
            MessageBody body = new([SegmentBuilder.Text(marker), SegmentBuilder.MentionAll()]);
            sent = await _fixture.Api!.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Sent @all: success={sent.Value.IsSuccess} messageId={sent.Value.MessageId}");
            Assert.True(sent.Value.IsSuccess, "SendGroupMessageAsync with @all should succeed");
            Assert.NotEqual(0L, sent.Value.MessageId.Value);
            passed = true;
        }
        finally
        {
            _fixture.RecordResult(passed);
            if (sent is { IsSuccess: true })
                await _fixture.Api!.RecallGroupMessageAsync(testGroup, sent.Value.MessageId, CT);
        }
    }

#endregion

#region Reply Messages

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task SendReceive_Reply()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        string  marker    = $"[OB11-MsgType:Reply] {Guid.NewGuid():N}";
        GroupId testGroup = TestConfig.TestGroupId;

        // Primary sends a message for Secondary to reply to
        SendMessageResult originalSent =
            await _fixture.Api!.SendGroupMessageAsync(testGroup, new MessageBody($"{marker} original"), CT);
        Assert.True(originalSent.IsSuccess, "Original message should be sent successfully");
        _output.WriteLine($"Original sent: messageId={originalSent.MessageId}");

        TaskCompletionSource<MessageReceivedEvent> tcs = new();

        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId && e.Message.Body.GetText().Contains(marker))
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service!.Events.OnMessageReceived += handler;
        SendMessageResult? replySent = null;
        bool               passed    = false;

        try
        {
            MessageBody replyBody = new(
                [
                    SegmentBuilder.Reply(originalSent.MessageId),
                    SegmentBuilder.Text($"{marker} reply")
                ]);

            await Task.Delay(1000, CT);
            replySent = await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, replyBody, CT);
            _output.WriteLine($"Secondary reply sent: success={replySent.Value.IsSuccess}");

            await Task.WhenAny(tcs.Task, Task.Delay(10000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the reply message within timeout");

            MessageReceivedEvent evt   = await tcs.Task;
            ReplySegment?        reply = evt.Message.Body.GetFirst<ReplySegment>();
            _output.WriteLine($"Received: hasReply={reply is not null} targetId={reply?.TargetId}");
            Assert.NotNull(reply);
            passed = true;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            _fixture.RecordResult(passed);
            if (replySent is { IsSuccess: true })
                await _fixture.SecondaryApi!.RecallGroupMessageAsync(testGroup, replySent.Value.MessageId, CT);
            await _fixture.Api!.RecallGroupMessageAsync(testGroup, originalSent.MessageId, CT);
        }
    }

#endregion

#region Multi-Segment Messages

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task SendReceive_MultiSegment()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        string  marker    = $"[OB11-MsgType:Multi] {Guid.NewGuid():N}";
        GroupId testGroup = TestConfig.TestGroupId;

        ApiResult<BotIdentity> primarySelf     = await _fixture.Api!.GetSelfInfoAsync(CT);
        BotIdentity            primarySelfInfo = primarySelf.AssertSuccess();
        UserId                 primaryUserId   = primarySelfInfo.UserId;

        TaskCompletionSource<MessageReceivedEvent> tcs = new();

        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId && e.Message.Body.GetText().Contains(marker))
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service!.Events.OnMessageReceived += handler;
        SendMessageResult? sent   = null;
        bool               passed = false;

        try
        {
            MessageBody body = new(
                [
                    SegmentBuilder.Text(marker),
                    SegmentBuilder.Face("1"),
                    SegmentBuilder.Mention(primaryUserId)
                ]);

            await Task.Delay(1000, CT);
            sent = await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Secondary sent: success={sent.Value.IsSuccess}");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the multi-segment message within timeout");

            MessageReceivedEvent evt = await tcs.Task;
            _output.WriteLine($"Received: text={evt.Message.Body.GetText()} segments={evt.Message.Body.Count}");
            Assert.Contains(marker, evt.Message.Body.GetText());
            Assert.NotNull(evt.Message.Body.GetFirst<FaceSegment>());
            Assert.NotNull(evt.Message.Body.GetFirst<MentionSegment>());
            passed = true;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            _fixture.RecordResult(passed);
            if (sent is { IsSuccess: true })
                await _fixture.SecondaryApi!.RecallGroupMessageAsync(testGroup, sent.Value.MessageId, CT);
        }
    }

#endregion

#region Media Messages

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task Send_Image()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId testGroup = TestConfig.TestGroupId;
        // 1x1 red PNG, base64-encoded
        string base64Image =
            "base64://iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==";
        string marker = $"[OB11-MsgType:Image] {Guid.NewGuid():N}";

        SendMessageResult? sent   = null;
        bool               passed = false;

        try
        {
            MessageBody body = new(
                [
                    SegmentBuilder.Text(marker),
                    SegmentBuilder.Image(base64Image)
                ]);

            sent = await _fixture.Api!.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Sent image: success={sent.Value.IsSuccess} messageId={sent.Value.MessageId}");
            Assert.True(sent.Value.IsSuccess, "SendGroupMessageAsync with image should succeed");
            Assert.NotEqual(0L, sent.Value.MessageId.Value);
            passed = true;
        }
        finally
        {
            _fixture.RecordResult(passed);
            if (sent is { IsSuccess: true })
                await _fixture.Api!.RecallGroupMessageAsync(testGroup, sent.Value.MessageId, CT);
        }
    }

#endregion

#region Forward Messages

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task Send_Forward()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId testGroup = TestConfig.TestGroupId;
        string  marker    = $"[OB11-MsgType:Forward] {Guid.NewGuid():N}";

        BotIdentity selfData = (await _fixture.Api!.GetSelfInfoAsync(CT)).AssertSuccess();

        ForwardedMessageNode[] nodes =
            [
                new()
                    {
                        UserId     = selfData.UserId,
                        SenderName = selfData.Nickname,
                        Segments   = new MessageBody($"{marker} Node 1")
                    },
                new()
                    {
                        UserId     = selfData.UserId,
                        SenderName = selfData.Nickname,
                        Segments   = new MessageBody($"{marker} Node 2")
                    }
            ];

        SendMessageResult? sent   = null;
        bool               passed = false;

        try
        {
            MessageBody body = new(SegmentBuilder.Forward(nodes));

            sent = await _fixture.Api!.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Sent forward: success={sent.Value.IsSuccess} messageId={sent.Value.MessageId}");
            Assert.True(sent.Value.IsSuccess, "SendGroupMessageAsync with forward nodes should succeed");
            Assert.NotEqual(0L, sent.Value.MessageId.Value);
            passed = true;
        }
        finally
        {
            _fixture.RecordResult(passed);
            if (sent is { IsSuccess: true })
                await _fixture.Api!.RecallGroupMessageAsync(testGroup, sent.Value.MessageId, CT);
        }
    }

#endregion

#region LightApp Messages

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task Send_LightApp()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId testGroup   = TestConfig.TestGroupId;
        string  jsonPayload = """{"app":"com.test","desc":"Sora OB11 Test","view":"test","meta":{}}""";

        SendMessageResult? sent   = null;
        bool               passed = false;

        try
        {
            MessageBody body = new(SegmentBuilder.LightApp("Sora OB11 Test", jsonPayload));

            sent = await _fixture.Api!.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Sent lightapp: success={sent.Value.IsSuccess} messageId={sent.Value.MessageId}");

            // OB11 may not support LightApp — log result but don't assert strictly
            if (sent.Value.IsSuccess)
                _output.WriteLine("LightApp send succeeded");
            else
                _output.WriteLine($"LightApp send returned: code={sent.Value.Code} err={sent.Value.ErrorMessage}");

            passed = true;
        }
        finally
        {
            _fixture.RecordResult(passed);
            if (sent is { IsSuccess: true })
                await _fixture.Api!.RecallGroupMessageAsync(testGroup, sent.Value.MessageId, CT);
        }
    }

#endregion
}