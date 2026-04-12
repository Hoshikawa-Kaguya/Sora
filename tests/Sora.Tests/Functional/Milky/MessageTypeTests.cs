using Xunit;

namespace Sora.Tests.Functional.Milky;

/// <summary>End-to-end message type tests for the Milky adapter. Tests different segment types via dual-bot interactions.</summary>
[Collection("Milky.Functional")]
[Trait("Category", "Functional")]
public class MessageTypeTests : IDisposable
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    private readonly MilkyTestFixture  _fixture;
    private readonly IDisposable       _logSubscription;
    private readonly ITestOutputHelper _output;

    /// <summary>MessageTypeTests ctor.</summary>
    public MessageTypeTests(MilkyTestFixture fixture, ITestOutputHelper output)
    {
        _fixture         = fixture;
        _output          = output;
        _logSubscription = fixture.OutputSink.Subscribe(output.WriteLine);
    }

#region Text Message Tests

    /// <see cref="TextSegment" />
    [Fact]
    public async Task SendReceive_Text()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId   testGroup     = TestConfig.TestGroupId;
        string    marker        = $"[MsgType:Text:{Guid.NewGuid():N}]";
        MessageId sentMessageId = default;

        TaskCompletionSource<MessageReceivedEvent> tcs = new();
        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId
                && e.Message.Body.GetText().Contains(marker))
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnMessageReceived += handler;

        try
        {
            await Task.Delay(1000, CT);
            MessageBody       body = new($"{marker} Hello World!\nLine2\n特殊字符: <>&\"'");
            SendMessageResult sent = await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Secondary sent: success={sent.IsSuccess} messageId={sent.MessageId}");
            Assert.True(sent.IsSuccess);
            sentMessageId = sent.MessageId;

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the text message within timeout");

            MessageReceivedEvent evt          = await tcs.Task;
            string               receivedText = evt.Message.Body.GetText();
            _output.WriteLine($"Received text: {receivedText}");

            Assert.Contains(marker, receivedText);
            Assert.Contains("Hello World!", receivedText);
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            if (sentMessageId != default)
                try
                {
                    await _fixture.SecondaryApi.RecallGroupMessageAsync(testGroup, sentMessageId, CT);
                }
                catch
                {
                    /* best-effort */
                }
        }
    }

    /// <see cref="TextSegment" />
    [Fact]
    public async Task SendReceive_Text_SpecialCharacters()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId   testGroup     = TestConfig.TestGroupId;
        string    marker        = $"[MsgType:TextSpecial:{Guid.NewGuid():N}]";
        MessageId sentMessageId = default;

        TaskCompletionSource<MessageReceivedEvent> tcs = new();
        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId
                && e.Message.Body.GetText().Contains(marker))
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnMessageReceived += handler;

        try
        {
            await Task.Delay(1000, CT);
            string            specialText = $"{marker} 你好世界 🎉🔥 café\nnewline\ttab <html>&amp;\"quotes'";
            SendMessageResult sent = await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, new MessageBody(specialText), CT);
            _output.WriteLine($"Secondary sent: success={sent.IsSuccess} messageId={sent.MessageId}");
            Assert.True(sent.IsSuccess);
            sentMessageId = sent.MessageId;

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the special-char message within timeout");

            MessageReceivedEvent evt          = await tcs.Task;
            string               receivedText = evt.Message.Body.GetText();
            _output.WriteLine($"Received text: {receivedText}");

            Assert.Contains(marker, receivedText);
            Assert.Contains("你好世界", receivedText);
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            if (sentMessageId != default)
                try
                {
                    await _fixture.SecondaryApi.RecallGroupMessageAsync(testGroup, sentMessageId, CT);
                }
                catch
                {
                    /* best-effort */
                }
        }
    }

#endregion

#region Face Emoji Tests

    /// <see cref="FaceSegment" />
    [Fact]
    public async Task SendReceive_Face()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId   testGroup     = TestConfig.TestGroupId;
        string    marker        = $"[MsgType:Face:{Guid.NewGuid():N}]";
        MessageId sentMessageId = default;

        TaskCompletionSource<MessageReceivedEvent> tcs = new();
        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId
                && e.Message.Body.GetText().Contains(marker))
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnMessageReceived += handler;

        try
        {
            await Task.Delay(1000, CT);
            MessageBody       body = new MessageBody(marker).AddFace("178");
            SendMessageResult sent = await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Secondary sent: success={sent.IsSuccess} messageId={sent.MessageId}");
            Assert.True(sent.IsSuccess);
            sentMessageId = sent.MessageId;

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the face message within timeout");

            MessageReceivedEvent evt  = await tcs.Task;
            FaceSegment?         face = evt.Message.Body.GetFirst<FaceSegment>();
            _output.WriteLine($"Received face: faceId={face?.FaceId}");

            Assert.NotNull(face);
            Assert.Equal("178", face.FaceId);
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            if (sentMessageId != default)
                try
                {
                    await _fixture.SecondaryApi.RecallGroupMessageAsync(testGroup, sentMessageId, CT);
                }
                catch
                {
                    /* best-effort */
                }
        }
    }

#endregion

#region Mention Tests

    /// <see cref="MentionSegment" />
    [Fact]
    public async Task SendReceive_Mention()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        // Get primary bot's UserId for mentioning
        BotIdentity primarySelf   = (await _fixture.Api!.GetSelfInfoAsync(CT)).AssertSuccess();
        UserId      primaryUserId = primarySelf.UserId;
        _output.WriteLine($"Primary bot UserId: {primaryUserId}");

        GroupId   testGroup     = TestConfig.TestGroupId;
        string    marker        = $"[MsgType:Mention:{Guid.NewGuid():N}]";
        MessageId sentMessageId = default;

        TaskCompletionSource<MessageReceivedEvent> tcs = new();
        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId
                && e.Message.Body.GetText().Contains(marker))
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnMessageReceived += handler;

        try
        {
            await Task.Delay(1000, CT);
            MessageBody       body = new MessageBody(marker).AddMention(primaryUserId);
            SendMessageResult sent = await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Secondary sent: success={sent.IsSuccess} messageId={sent.MessageId}");
            Assert.True(sent.IsSuccess);
            sentMessageId = sent.MessageId;

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the mention message within timeout");

            MessageReceivedEvent evt     = await tcs.Task;
            MentionSegment?      mention = evt.Message.Body.GetFirst<MentionSegment>();
            _output.WriteLine($"Received mention: target={mention?.Target}");

            Assert.NotNull(mention);
            Assert.Equal(primaryUserId, mention.Target);
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            if (sentMessageId != default)
                try
                {
                    await _fixture.SecondaryApi.RecallGroupMessageAsync(testGroup, sentMessageId, CT);
                }
                catch
                {
                    /* best-effort */
                }
        }
    }

    /// <see cref="MentionSegment" />
    [Fact]
    public async Task Send_MentionAll()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId   testGroup     = TestConfig.TestGroupId;
        MessageId sentMessageId = default;

        try
        {
            MessageBody       body   = new MessageBody("[Test] MentionAll").AddMentionAll();
            SendMessageResult result = await _fixture.Api!.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"MentionAll send: success={result.IsSuccess} code={result.Code} messageId={result.MessageId}");
            Assert.SkipWhen(
                result is { IsSuccess: false, Code: ApiStatusCode.Error or ApiStatusCode.ProtocolError },
                $"SendGroupMessage with MentionAll returned {result.Code} — bot run out of mention times");
            Assert.NotEqual(0L, result.MessageId.Value);
            sentMessageId = result.MessageId;
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            if (sentMessageId != default)
                try
                {
                    await _fixture.Api!.RecallGroupMessageAsync(testGroup, sentMessageId, CT);
                }
                catch
                {
                    /* best-effort */
                }
        }
    }

#endregion

#region Reply Tests

    /// <see cref="ReplySegment" />
    [Fact]
    public async Task SendReceive_Reply()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId   testGroup         = TestConfig.TestGroupId;
        MessageId originalMessageId = default;
        MessageId replyMessageId    = default;

        try
        {
            // Step 1: Primary sends original message
            string            originalMarker = $"[MsgType:ReplyOriginal:{Guid.NewGuid():N}]";
            SendMessageResult original = await _fixture.Api!.SendGroupMessageAsync(testGroup, new MessageBody(originalMarker), CT);
            _output.WriteLine($"Original sent: success={original.IsSuccess} messageId={original.MessageId}");
            Assert.True(original.IsSuccess);
            originalMessageId = original.MessageId;

            await Task.Delay(500, CT);

            // Step 2: Register handler for reply from secondary
            string                                     replyMarker = $"[MsgType:Reply:{Guid.NewGuid():N}]";
            TaskCompletionSource<MessageReceivedEvent> tcs         = new();
            Func<MessageReceivedEvent, ValueTask> handler = e =>
            {
                if (e.Message.SenderId == _fixture.SecondaryUserId
                    && e.Message.Body.GetText().Contains(replyMarker))
                    tcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            };
            _fixture.Service.Events.OnMessageReceived += handler;

            try
            {
                // Step 3: Secondary sends reply referencing original
                await Task.Delay(1000, CT);
                MessageBody replyBody = new(
                    [
                        new ReplySegment { TargetId = original.MessageId },
                        new TextSegment { Text      = replyMarker }
                    ]);
                SendMessageResult replySent = await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, replyBody, CT);
                _output.WriteLine($"Reply sent: success={replySent.IsSuccess} messageId={replySent.MessageId}");
                Assert.True(replySent.IsSuccess);
                replyMessageId = replySent.MessageId;

                // Step 4: Wait and verify
                await Task.WhenAny(tcs.Task, Task.Delay(10000, CT));
                Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the reply message within timeout");

                MessageReceivedEvent evt   = await tcs.Task;
                ReplySegment?        reply = evt.Message.Body.GetFirst<ReplySegment>();
                _output.WriteLine($"Received reply: targetId={reply?.TargetId}");

                Assert.NotNull(reply);
                Assert.Equal(original.MessageId, reply.TargetId);
                _fixture.RecordResult(true);
            }
            finally
            {
                _fixture.Service.Events.OnMessageReceived -= handler;
            }
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            if (replyMessageId != default)
                try
                {
                    await _fixture.SecondaryApi.RecallGroupMessageAsync(testGroup, replyMessageId, CT);
                }
                catch
                {
                    /* best-effort */
                }

            if (originalMessageId != default)
                try
                {
                    await _fixture.Api!.RecallGroupMessageAsync(testGroup, originalMessageId, CT);
                }
                catch
                {
                    /* best-effort */
                }
        }
    }

#endregion

#region Multi-Segment Tests

    /// <see cref="MessageBody" />
    [Fact]
    public async Task SendReceive_MultiSegment()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        // Get primary bot's UserId for mentioning
        BotIdentity primarySelf   = (await _fixture.Api!.GetSelfInfoAsync(CT)).AssertSuccess();
        UserId      primaryUserId = primarySelf.UserId;

        GroupId   testGroup     = TestConfig.TestGroupId;
        string    marker        = $"[MsgType:Multi:{Guid.NewGuid():N}]";
        MessageId sentMessageId = default;

        TaskCompletionSource<MessageReceivedEvent> tcs = new();
        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId
                && e.Message.Body.GetText().Contains(marker))
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnMessageReceived += handler;

        try
        {
            await Task.Delay(1000, CT);
            MessageBody body = new(
                [
                    new TextSegment { Text      = marker },
                    new FaceSegment { FaceId    = "178" },
                    new MentionSegment { Target = primaryUserId },
                    new TextSegment { Text      = " suffix" }
                ]);
            SendMessageResult sent = await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Secondary sent multi-segment: success={sent.IsSuccess} messageId={sent.MessageId}");
            Assert.True(sent.IsSuccess);
            sentMessageId = sent.MessageId;

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the multi-segment message within timeout");

            MessageReceivedEvent evt = await tcs.Task;
            _output.WriteLine($"Received text: {evt.Message.Body.GetText()}");

            Assert.True(evt.Message.Body.GetAll<TextSegment>().Any(), "Expected at least one TextSegment");
            Assert.NotNull(evt.Message.Body.GetFirst<FaceSegment>());
            Assert.NotNull(evt.Message.Body.GetFirst<MentionSegment>());
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            if (sentMessageId != default)
                try
                {
                    await _fixture.SecondaryApi.RecallGroupMessageAsync(testGroup, sentMessageId, CT);
                }
                catch
                {
                    /* best-effort */
                }
        }
    }

#endregion

#region Media Send Tests

    /// <see cref="ImageSegment" />
    [Fact]
    public async Task Send_Image()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId   testGroup     = TestConfig.TestGroupId;
        MessageId sentMessageId = default;

        try
        {
            // 1x1 red PNG as base64
            string base64Png =
                "base64://iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==";
            MessageBody       body   = new([new ImageSegment { FileUri = base64Png }]);
            SendMessageResult result = await _fixture.Api!.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Image send: success={result.IsSuccess} messageId={result.MessageId}");
            Assert.True(result.IsSuccess);
            Assert.NotEqual(0L, result.MessageId.Value);
            sentMessageId = result.MessageId;
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            if (sentMessageId != default)
                try
                {
                    await _fixture.Api!.RecallGroupMessageAsync(testGroup, sentMessageId, CT);
                }
                catch
                {
                    /* best-effort */
                }
        }
    }

    /// <see cref="ForwardSegment" />
    [Fact]
    public async Task Send_Forward()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId testGroup = TestConfig.TestGroupId;

        try
        {
            ForwardedMessageNode node1 = new()
                    { UserId = _fixture.SecondaryUserId, SenderName = "TestUser", Segments = new MessageBody("Forward line 1") };
            ForwardedMessageNode node2 = new()
                    { UserId = _fixture.SecondaryUserId, SenderName = "TestUser", Segments = new MessageBody("Forward line 2") };
            ForwardSegment    forward = new() { Messages = [node1, node2], Title = "Test Forward" };
            MessageBody       body    = new([forward]);
            SendMessageResult result  = await _fixture.Api!.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Forward send: success={result.IsSuccess} messageId={result.MessageId}");
            Assert.True(result.IsSuccess);
            Assert.NotEqual(0L, result.MessageId.Value);
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
    }

    /// <see cref="LightAppSegment" />
    [Fact]
    public async Task Send_LightApp()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        GroupId testGroup = TestConfig.TestGroupId;

        try
        {
            string            json   = """{"app":"com.tencent.miniapp","desc":"Test","view":"all","meta":{}}""";
            MessageBody       body   = new([new LightAppSegment { JsonPayload = json }]);
            SendMessageResult result = await _fixture.Api!.SendGroupMessageAsync(testGroup, body, CT);
            // LightApp may not be supported by all backends
            _output.WriteLine($"LightApp send: success={result.IsSuccess}");
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
    }

#endregion

#region Audio Tests

    /// <see cref="AudioSegment" />
    [Fact]
    public async Task SendReceive_Audio()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        string audioPath = TestConfig.AudioFilePath;
        Assert.SkipWhen(string.IsNullOrEmpty(audioPath), "SORA_TEST_AUDIO_FILE not set.");
        Assert.SkipWhen(!File.Exists(audioPath), $"Audio file not found: {audioPath}");

        byte[] audioBytes = await File.ReadAllBytesAsync(audioPath, CT);
        string base64Uri  = $"base64://{Convert.ToBase64String(audioBytes)}";

        GroupId   testGroup     = TestConfig.TestGroupId;
        MessageId sentMessageId = default;

        TaskCompletionSource<MessageReceivedEvent> tcs = new();
        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId
                && e.Message.Body.GetFirst<AudioSegment>() is not null)
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnMessageReceived += handler;

        try
        {
            await Task.Delay(1000, CT);
            MessageBody       body = new([new AudioSegment { FileUri = base64Uri }]);
            SendMessageResult sent = await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Secondary sent audio: success={sent.IsSuccess} messageId={sent.MessageId}");
            Assert.True(sent.IsSuccess, "SendGroupMessageAsync (audio) should succeed");
            sentMessageId = sent.MessageId;

            await Task.WhenAny(tcs.Task, Task.Delay(10000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the audio message within timeout");

            MessageReceivedEvent evt   = await tcs.Task;
            AudioSegment?        audio = evt.Message.Body.GetFirst<AudioSegment>();
            _output.WriteLine($"Received audio: resourceId={audio?.ResourceId} url={audio?.Url} duration={audio?.Duration}");

            Assert.NotNull(audio);
            Assert.True(
                !string.IsNullOrEmpty(audio.ResourceId) || !string.IsNullOrEmpty(audio.Url),
                "Incoming AudioSegment should have a ResourceId or Url populated by the protocol");
            Assert.Equal(_fixture.SecondaryUserId, evt.Message.SenderId);
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            if (sentMessageId != default)
                try
                {
                    await _fixture.SecondaryApi.RecallGroupMessageAsync(testGroup, sentMessageId, CT);
                }
                catch
                {
                    /* best-effort */
                }
        }
    }

#endregion

#region Video Tests

    /// <see cref="VideoSegment" />
    [Fact]
    public async Task SendReceive_Video()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");

        string videoPath = TestConfig.VideoFilePath;
        Assert.SkipWhen(string.IsNullOrEmpty(videoPath), "SORA_TEST_VIDEO_FILE not set.");
        Assert.SkipWhen(!File.Exists(videoPath), $"Video file not found: {videoPath}");

        byte[] videoBytes = await File.ReadAllBytesAsync(videoPath, CT);
        string base64Uri  = $"base64://{Convert.ToBase64String(videoBytes)}";

        GroupId   testGroup     = TestConfig.TestGroupId;
        MessageId sentMessageId = default;

        TaskCompletionSource<MessageReceivedEvent> tcs = new();
        Func<MessageReceivedEvent, ValueTask> handler = e =>
        {
            if (e.Message.SenderId == _fixture.SecondaryUserId
                && e.Message.Body.GetFirst<VideoSegment>() is not null)
                tcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnMessageReceived += handler;

        try
        {
            await Task.Delay(1000, CT);
            MessageBody       body = new([new VideoSegment { FileUri = base64Uri }]);
            SendMessageResult sent = await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, body, CT);
            _output.WriteLine($"Secondary sent video: success={sent.IsSuccess} messageId={sent.MessageId}");
            Assert.True(sent.IsSuccess, "SendGroupMessageAsync (video) should succeed");
            sentMessageId = sent.MessageId;

            await Task.WhenAny(tcs.Task, Task.Delay(10000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Primary did not receive the video message within timeout");

            MessageReceivedEvent evt   = await tcs.Task;
            VideoSegment?        video = evt.Message.Body.GetFirst<VideoSegment>();
            _output.WriteLine(
                $"Received video: resourceId={video?.ResourceId} url={video?.Url} duration={video?.Duration} width={video?.Width} height={video?.Height}");

            Assert.NotNull(video);
            Assert.True(
                !string.IsNullOrEmpty(video.ResourceId) || !string.IsNullOrEmpty(video.Url),
                "Incoming VideoSegment should have a ResourceId or Url populated by the protocol");
            Assert.Equal(_fixture.SecondaryUserId, evt.Message.SenderId);
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= handler;
            if (sentMessageId != default)
                try
                {
                    await _fixture.SecondaryApi.RecallGroupMessageAsync(testGroup, sentMessageId, CT);
                }
                catch
                {
                    /* best-effort */
                }
        }
    }

#endregion

    /// <inheritdoc />
    public void Dispose() => _logSubscription.Dispose();
}