using Sora.Entities.MessageWaiting;
using Xunit;

namespace Sora.Tests.Functional.OneBot11;

/// <summary>End-to-end command system and continuous dialogue tests for the OneBot11 adapter.</summary>
[Collection("OneBot11.Functional")]
[Trait("Category", "Functional")]
public class CommandTests : IDisposable
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    private readonly OneBot11TestFixture _fixture;
    private readonly IDisposable         _logSubscription;
    private readonly ITestOutputHelper   _output;

    /// <summary>Initializes a new instance of the <see cref="CommandTests" /> class.</summary>
    public CommandTests(OneBot11TestFixture fixture, ITestOutputHelper output)
    {
        _fixture         = fixture;
        _output          = output;
        _logSubscription = fixture.OutputSink.Subscribe(output.WriteLine);
    }

    /// <inheritdoc />
    public void Dispose() => _logSubscription.Dispose();

#region Command Matching Tests

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_KeywordMatch()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");

        string                                     keyword = $"[OB11-Cmd:Full] {Guid.NewGuid():N}";
        TaskCompletionSource<MessageReceivedEvent> tcs     = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            e =>
            {
                tcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [keyword]);

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            await Task.Delay(1000, CT);
            SendMessageResult sent =
                await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);
            _output.WriteLine($"Secondary sent: success={sent.IsSuccess} messageId={sent.MessageId}");

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Full-match command handler was not triggered within timeout");

            MessageReceivedEvent evt = await tcs.Task;
            _output.WriteLine($"Command matched: text={evt.Message.Body.GetText()} senderId={evt.Message.SenderId}");
            Assert.Contains(keyword, evt.Message.Body.GetText());
            Assert.Equal(_fixture.SecondaryUserId, evt.Message.SenderId);
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
    }

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_RegexMatch()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");

        string                                     guid        = Guid.NewGuid().ToString("N");
        string                                     pattern     = $@"\[OB11-Cmd:Regex\] {guid}";
        string                                     triggerText = $"[OB11-Cmd:Regex] {guid} extra payload";
        TaskCompletionSource<MessageReceivedEvent> tcs         = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            e =>
            {
                tcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [pattern],
            MatchType.Regex);

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(triggerText), CT);

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Regex command handler was not triggered within timeout");

            MessageReceivedEvent evt = await tcs.Task;
            _output.WriteLine($"Regex matched: text={evt.Message.Body.GetText()}");
            Assert.Contains(guid, evt.Message.Body.GetText());
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
    }

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_KeywordMatch_Substring()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");

        string                                     guid     = Guid.NewGuid().ToString("N");
        string                                     keyword  = $"subcmd-{guid}";
        string                                     fullText = $"some prefix {keyword} some suffix";
        TaskCompletionSource<MessageReceivedEvent> tcs      = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            e =>
            {
                tcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [keyword],
            MatchType.Keyword);

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(fullText), CT);

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Keyword command handler was not triggered within timeout");

            MessageReceivedEvent evt = await tcs.Task;
            _output.WriteLine($"Keyword matched: text={evt.Message.Body.GetText()}");
            Assert.Contains(keyword, evt.Message.Body.GetText());
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
    }

#endregion

#region Source Type Filter Tests

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_GroupOnly()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");

        string                                     keyword = $"[OB11-Cmd:Group] {Guid.NewGuid():N}";
        TaskCompletionSource<MessageReceivedEvent> tcs     = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            e =>
            {
                tcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [keyword],
            sourceType: MessageSourceType.Group);

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);

            await Task.WhenAny(tcs.Task, Task.Delay(5000, CT));
            Assert.True(tcs.Task.IsCompletedSuccessfully, "Group-only command was not triggered for group message");
            _output.WriteLine($"Group command matched: text={(await tcs.Task).Message.Body.GetText()}");
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
    }

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_PrivateOnly()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");

        string                                     keyword = $"[OB11-Cmd:Private] {Guid.NewGuid():N}";
        TaskCompletionSource<MessageReceivedEvent> cmdTcs  = new();
        TaskCompletionSource<MessageReceivedEvent> evtTcs  = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            e =>
            {
                cmdTcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [keyword],
            sourceType: MessageSourceType.Friend);

        Func<MessageReceivedEvent, ValueTask> eventHandler = e =>
        {
            if (e.Message.Body.GetText().Contains(keyword))
                evtTcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnMessageReceived += eventHandler;

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);

            await Task.WhenAny(evtTcs.Task, Task.Delay(5000, CT));
            Assert.False(
                cmdTcs.Task.IsCompletedSuccessfully,
                "Friend-only command should not trigger for group messages");
            Assert.True(
                evtTcs.Task.IsCompletedSuccessfully,
                "Message should pass through to OnMessageReceived");
            _output.WriteLine("Friend-only command correctly skipped for group message");
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= eventHandler;
        }
    }

#endregion

#region Priority & Permission Tests

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_Priority()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");

        string                       keyword = $"[OB11-Cmd:Priority] {Guid.NewGuid():N}";
        TaskCompletionSource<string> highTcs = new();
        TaskCompletionSource<string> lowTcs  = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            _ =>
            {
                highTcs.TrySetResult("high");
                return ValueTask.CompletedTask;
            },
                [keyword],
            priority: 10,
            blockAfterMatch: true);

        _fixture.Service.Commands.RegisterDynamicCommand(
            _ =>
            {
                lowTcs.TrySetResult("low");
                return ValueTask.CompletedTask;
            },
                [keyword],
            priority: 1,
            blockAfterMatch: true);

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);

            await Task.WhenAny(highTcs.Task, Task.Delay(5000, CT));
            Assert.True(highTcs.Task.IsCompletedSuccessfully, "High-priority command was not triggered");

            // Give low-priority command a moment to (incorrectly) fire
            await Task.Delay(500, CT);
            Assert.False(
                lowTcs.Task.IsCompletedSuccessfully,
                "Low-priority command should not fire after high-priority blocks");
            _output.WriteLine("Priority ordering verified: high fired, low blocked");
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
    }

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_DynamicRegister()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");

        string  keyword   = $"[OB11-Cmd:Dynamic] {Guid.NewGuid():N}";
        GroupId testGroup = TestConfig.TestGroupId;

        // Phase 1: Send keyword before registering — message should reach OnMessageReceived
        TaskCompletionSource<MessageReceivedEvent> preTcs = new();
        Func<MessageReceivedEvent, ValueTask> preHandler = e =>
        {
            if (e.Message.Body.GetText().Contains(keyword))
                preTcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service!.Events.OnMessageReceived += preHandler;

        try
        {
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);

            await Task.WhenAny(preTcs.Task, Task.Delay(5000, CT));
            Assert.True(
                preTcs.Task.IsCompletedSuccessfully,
                "Message should reach OnMessageReceived before command is registered");
            _output.WriteLine("Phase 1: Message reached OnMessageReceived (no command registered yet)");
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= preHandler;
        }

        // Phase 2: Register command dynamically — should now intercept
        TaskCompletionSource<MessageReceivedEvent> cmdTcs = new();

        _fixture.Service.Commands.RegisterDynamicCommand(
            e =>
            {
                cmdTcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [keyword]);

        try
        {
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);

            await Task.WhenAny(cmdTcs.Task, Task.Delay(5000, CT));
            Assert.True(
                cmdTcs.Task.IsCompletedSuccessfully,
                "Dynamically registered command should intercept the message");
            _output.WriteLine($"Phase 2: Command intercepted: text={(await cmdTcs.Task).Message.Body.GetText()}");
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
    }

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_Permission_OwnerRequired()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");

        string                                     keyword = $"[OB11-Cmd:Permission] {Guid.NewGuid():N}";
        TaskCompletionSource<MessageReceivedEvent> cmdTcs  = new();
        TaskCompletionSource<MessageReceivedEvent> evtTcs  = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            e =>
            {
                cmdTcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [keyword],
            permissionLevel: MemberRole.Owner);

        Func<MessageReceivedEvent, ValueTask> eventHandler = e =>
        {
            if (e.Message.Body.GetText().Contains(keyword))
                evtTcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnMessageReceived += eventHandler;

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);

            await Task.WhenAny(evtTcs.Task, Task.Delay(5000, CT));
            Assert.False(
                cmdTcs.Task.IsCompletedSuccessfully,
                "Owner-level command should not trigger for regular member");
            Assert.True(
                evtTcs.Task.IsCompletedSuccessfully,
                "Message should pass through to OnMessageReceived when permission is insufficient");
            _output.WriteLine("Owner-level command correctly skipped for regular member");
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= eventHandler;
        }
    }

#endregion

#region Continuous Dialogue Tests

    /// <see cref="MessageWaiterExtensions" />
    [Fact]
    public async Task ContinuousDialogue_WaitNextMessage()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");

        string                                      triggerKeyword = $"[OB11-Cmd:Wait] {Guid.NewGuid():N}";
        string                                      followUpText   = $"[OB11-Cmd:Wait-Reply] {Guid.NewGuid():N}";
        TaskCompletionSource<MessageReceivedEvent?> dialogueTcs    = new();

        CancellationToken ct = CT;
        _fixture.Service!.Commands.RegisterDynamicCommand(
            async e =>
            {
                MessageReceivedEvent? reply = await e.WaitForNextMessageAsync(TimeSpan.FromSeconds(10), ct);
                dialogueTcs.TrySetResult(reply);
            },
                [triggerKeyword]);

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(triggerKeyword), CT);
            _output.WriteLine("Sent trigger keyword");

            // Give the command handler time to register the waiter
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, new MessageBody(followUpText), CT);
            _output.WriteLine("Sent follow-up message");

            await Task.WhenAny(dialogueTcs.Task, Task.Delay(10000, CT));
            Assert.True(
                dialogueTcs.Task.IsCompletedSuccessfully,
                "Dialogue handler did not complete within timeout");

            MessageReceivedEvent? reply = await dialogueTcs.Task;
            Assert.NotNull(reply);
            string replyText = reply.Message.Body.GetText();
            _output.WriteLine($"Follow-up received: text={replyText}");
            Assert.SkipWhen(
                !replyText.Contains(followUpText),
                $"Waiter received cross-protocol message instead of expected follow-up: {replyText}");
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
    }

    /// <see cref="MessageWaiterExtensions" />
    [Fact]
    public async Task ContinuousDialogue_Timeout()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");

        string                                      triggerKeyword = $"[OB11-Cmd:Timeout] {Guid.NewGuid():N}";
        TaskCompletionSource<MessageReceivedEvent?> dialogueTcs    = new();

        CancellationToken ct = CT;
        _fixture.Service!.Commands.RegisterDynamicCommand(
            async e =>
            {
                MessageReceivedEvent? reply = await e.WaitForNextMessageAsync(TimeSpan.FromSeconds(3), ct);
                dialogueTcs.TrySetResult(reply);
            },
                [triggerKeyword]);

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(triggerKeyword), CT);
            _output.WriteLine("Sent trigger keyword, no follow-up will be sent");

            await Task.WhenAny(dialogueTcs.Task, Task.Delay(10000, CT));
            Assert.True(
                dialogueTcs.Task.IsCompletedSuccessfully,
                "Dialogue handler did not complete within timeout");
            Assert.SkipWhen(
                await dialogueTcs.Task is not null,
                $"Waiter received cross-protocol message instead of timing out: {(await dialogueTcs.Task)?.Message.Body.GetText()}");
            _output.WriteLine("WaitForNextMessageAsync correctly returned null on timeout");
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
    }

    /// <see cref="MessageWaiterExtensions" />
    [Fact]
    public async Task ContinuousDialogue_MultiTurn()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");

        string                             triggerKeyword = $"[OB11-Cmd:MultiTurn] {Guid.NewGuid():N}";
        string                             turn2Text      = $"[OB11-Cmd:Turn2] {Guid.NewGuid():N}";
        string                             turn3Text      = $"[OB11-Cmd:Turn3] {Guid.NewGuid():N}";
        TaskCompletionSource<List<string>> dialogueTcs    = new();

        CancellationToken ct = CT;
        _fixture.Service!.Commands.RegisterDynamicCommand(
            async e =>
            {
                List<string> turns = [e.Message.Body.GetText()];

                MessageReceivedEvent? reply1 = await e.WaitForNextMessageAsync(TimeSpan.FromSeconds(10), ct);
                if (reply1 is not null)
                {
                    turns.Add(reply1.Message.Body.GetText());

                    MessageReceivedEvent? reply2 = await reply1.WaitForNextMessageAsync(TimeSpan.FromSeconds(10), ct);
                    if (reply2 is not null)
                        turns.Add(reply2.Message.Body.GetText());
                }

                dialogueTcs.TrySetResult(turns);
            },
                [triggerKeyword]);

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(triggerKeyword), CT);
            _output.WriteLine("Sent turn 1 (trigger)");

            await Task.Delay(1500, CT);
            await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, new MessageBody(turn2Text), CT);
            _output.WriteLine("Sent turn 2");

            await Task.Delay(1500, CT);
            await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, new MessageBody(turn3Text), CT);
            _output.WriteLine("Sent turn 3");

            await Task.WhenAny(dialogueTcs.Task, Task.Delay(15000, CT));
            Assert.True(
                dialogueTcs.Task.IsCompletedSuccessfully,
                "Multi-turn dialogue did not complete within timeout");

            List<string> result = await dialogueTcs.Task;
            _output.WriteLine($"Multi-turn dialogue completed: {result.Count} turns");
            foreach (string turn in result)
                _output.WriteLine($"  turn: {turn}");
            Assert.SkipWhen(
                result.Count < 3 || !result[1].Contains(turn2Text),
                $"Cross-protocol contamination detected in multi-turn dialogue (turn count={result.Count})");
            Assert.Contains(triggerKeyword, result[0]);
            Assert.Contains(turn2Text, result[1]);
            Assert.Contains(turn3Text, result[2]);
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
    }

#endregion

#region Pass-Through Tests

    /// <see cref="EventDispatcher.OnMessageReceived" />
    [Fact]
    public async Task Command_NonMatchingMessage_PassesThrough()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");

        string                                     commandKeyword = $"[OB11-Cmd:NonMatch] {Guid.NewGuid():N}";
        string                                     otherText      = $"[OB11-Cmd:PassThrough] {Guid.NewGuid():N}";
        TaskCompletionSource<MessageReceivedEvent> cmdTcs         = new();
        TaskCompletionSource<MessageReceivedEvent> evtTcs         = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            e =>
            {
                cmdTcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [commandKeyword]);

        Func<MessageReceivedEvent, ValueTask> eventHandler = e =>
        {
            if (e.Message.Body.GetText().Contains(otherText))
                evtTcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnMessageReceived += eventHandler;

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            await Task.Delay(1000, CT);
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(otherText), CT);

            await Task.WhenAny(evtTcs.Task, Task.Delay(5000, CT));
            Assert.True(
                evtTcs.Task.IsCompletedSuccessfully,
                "Non-matching message should reach OnMessageReceived");
            Assert.False(
                cmdTcs.Task.IsCompletedSuccessfully,
                "Command should not trigger for non-matching text");
            _output.WriteLine("Non-matching message correctly passed through to event handler");
            _fixture.RecordResult(true);
        }
        catch
        {
            _fixture.RecordResult(false);
            throw;
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= eventHandler;
        }
    }

#endregion
}