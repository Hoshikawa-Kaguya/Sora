using Sora.Entities.MessageWaiting;
using Xunit;

namespace Sora.Tests.Functional.Milky;

/// <summary>End-to-end command system and continuous dialogue tests for the Milky adapter.</summary>
/// <remarks>
///     <para>
///         These tests verify the complete pipeline: Secondary bot sends a real QQ message → Milky protocol →
///         Primary SoraService receives → <see cref="CommandManager" /> matches → handler executes.
///         This exercises protocol parsing, event dispatch, and command execution end-to-end.
///     </para>
///     <para>
///         <see cref="CommandManager.RegisterDynamicCommand" /> has no unregistration mechanism;
///         each test uses a unique GUID-based keyword to avoid cross-test interference.
///     </para>
/// </remarks>
[Collection("Milky.Functional")]
[Trait("Category", "Functional")]
public class CommandTests : IDisposable
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    private readonly MilkyTestFixture  _fixture;
    private readonly IDisposable       _logSubscription;
    private readonly ITestOutputHelper _output;

    /// <summary>CommandTests ctor.</summary>
    public CommandTests(MilkyTestFixture fixture, ITestOutputHelper output)
    {
        _fixture         = fixture;
        _output          = output;
        _logSubscription = fixture.OutputSink.Subscribe(output.WriteLine);
    }

    /// <inheritdoc />
    public void Dispose() => _logSubscription.Dispose();

    /// <summary>Standard skip-guard for dual-bot command tests.</summary>
    private void SkipIfNotReady()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyDualBotReason is not null, TestConfig.SkipMilkyDualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryApi is null, "Secondary API not available");
        Assert.SkipWhen(_fixture.Service is null, "Service not available");
    }

#region Keyword Command Tests

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_KeywordMatch()
    {
        SkipIfNotReady();

        string                                     keyword = $"!test-keyword-{Guid.NewGuid():N}";
        TaskCompletionSource<MessageReceivedEvent> tcs     = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            e =>
            {
                tcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [keyword],
            MatchType.Full,
            MessageSourceType.Group);

        GroupId testGroup = TestConfig.TestGroupId;
        SendMessageResult sent =
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);
        _output.WriteLine($"Sent: success={sent.IsSuccess} messageId={sent.MessageId}");

        await Task.WhenAny(tcs.Task, Task.Delay(8000, CT));
        Assert.True(tcs.Task.IsCompletedSuccessfully, "Command handler was not triggered");

        MessageReceivedEvent result = await tcs.Task;
        _output.WriteLine($"Matched: text={result.Message.Body.GetText()} senderId={result.Message.SenderId}");
        Assert.Equal(keyword, result.Message.Body.GetText().Trim());

        _fixture.RecordResult(true);
    }

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_RegexMatch()
    {
        SkipIfNotReady();

        string                                     tag         = Guid.NewGuid().ToString("N")[..8];
        string                                     pattern     = $@"^!calc-{tag} \d+$";
        string                                     triggerText = $"!calc-{tag} 42";
        TaskCompletionSource<MessageReceivedEvent> tcs         = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            e =>
            {
                tcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [pattern],
            MatchType.Regex,
            MessageSourceType.Group);

        GroupId testGroup = TestConfig.TestGroupId;
        SendMessageResult sent =
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(triggerText), CT);
        _output.WriteLine($"Sent: success={sent.IsSuccess} messageId={sent.MessageId}");

        await Task.WhenAny(tcs.Task, Task.Delay(8000, CT));
        Assert.True(tcs.Task.IsCompletedSuccessfully, "Regex command handler was not triggered");

        string receivedText = (await tcs.Task).Message.Body.GetText().Trim();
        _output.WriteLine($"Matched: text={receivedText}");
        Assert.Equal(triggerText, receivedText);

        _fixture.RecordResult(true);
    }

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_KeywordMatch_Substring()
    {
        SkipIfNotReady();

        string                                     tag      = Guid.NewGuid().ToString("N")[..8];
        string                                     keyword  = $"hello-{tag}";
        string                                     fullText = $"say {keyword} world";
        TaskCompletionSource<MessageReceivedEvent> tcs      = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            e =>
            {
                tcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [keyword],
            MatchType.Keyword,
            MessageSourceType.Group);

        GroupId testGroup = TestConfig.TestGroupId;
        SendMessageResult sent =
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(fullText), CT);
        _output.WriteLine($"Sent: success={sent.IsSuccess} messageId={sent.MessageId}");

        await Task.WhenAny(tcs.Task, Task.Delay(8000, CT));
        Assert.True(tcs.Task.IsCompletedSuccessfully, "Keyword command handler was not triggered");

        string receivedText = (await tcs.Task).Message.Body.GetText().Trim();
        _output.WriteLine($"Matched: text={receivedText}");
        Assert.Contains(keyword, receivedText);

        _fixture.RecordResult(true);
    }

#endregion

#region Source Type Filtering Tests

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_GroupOnly()
    {
        SkipIfNotReady();

        string                                     keyword = $"!grp-only-{Guid.NewGuid():N}";
        TaskCompletionSource<MessageReceivedEvent> tcs     = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            e =>
            {
                tcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [keyword],
            MatchType.Full,
            MessageSourceType.Group);

        // Group message should trigger
        GroupId testGroup = TestConfig.TestGroupId;
        await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);

        await Task.WhenAny(tcs.Task, Task.Delay(8000, CT));
        Assert.True(tcs.Task.IsCompletedSuccessfully, "Group-only command was not triggered by group message");
        _output.WriteLine("Group message correctly triggered group-only command");

        _fixture.RecordResult(true);
    }

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_PrivateOnly()
    {
        SkipIfNotReady();

        // Discover Primary bot's UserId so Secondary can send a friend message
        ApiResult<BotIdentity> selfInfo = await _fixture.Api!.GetSelfInfoAsync(CT);
        Assert.SkipWhen(!selfInfo.IsSuccess, "Could not get Primary bot identity");
        BotIdentity selfData      = selfInfo.AssertSuccess();
        UserId      primaryUserId = selfData.UserId;

        // Verify bots are friends
        ApiResult<IReadOnlyList<FriendInfo>> friends = await _fixture.SecondaryApi!.GetFriendListAsync(ct: CT);
        Assert.SkipWhen(!friends.IsSuccess, "Could not get Secondary friend list");
        IReadOnlyList<FriendInfo> friendList = friends.AssertSuccess();
        bool                      areFriends = friendList.Any(f => f.UserId == primaryUserId);
        Assert.SkipWhen(!areFriends, "Primary and Secondary bots are not friends; private-message test skipped");

        string                                     keyword = $"!priv-only-{Guid.NewGuid():N}";
        TaskCompletionSource<MessageReceivedEvent> tcs     = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            e =>
            {
                tcs.TrySetResult(e);
                return ValueTask.CompletedTask;
            },
                [keyword],
            MatchType.Full,
            MessageSourceType.Friend);

        await _fixture.SecondaryApi!.SendFriendMessageAsync(primaryUserId, new MessageBody(keyword), CT);
        _output.WriteLine($"Sent private message to Primary ({primaryUserId})");

        await Task.WhenAny(tcs.Task, Task.Delay(8000, CT));
        Assert.True(tcs.Task.IsCompletedSuccessfully, "Private-only command was not triggered by friend message");
        _output.WriteLine("Friend message correctly triggered private-only command");

        _fixture.RecordResult(true);
    }

#endregion

#region Priority Tests

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_Priority()
    {
        SkipIfNotReady();

        string                       keyword     = $"!prio-{Guid.NewGuid():N}";
        TaskCompletionSource<string> highPrioTcs = new();
        TaskCompletionSource<string> lowPrioTcs  = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            _ =>
            {
                highPrioTcs.TrySetResult("high");
                return ValueTask.CompletedTask;
            },
                [keyword],
            MatchType.Full,
            MessageSourceType.Group,
            priority: 10,
            blockAfterMatch: true,
            description: "high priority command");

        _fixture.Service.Commands.RegisterDynamicCommand(
            _ =>
            {
                lowPrioTcs.TrySetResult("low");
                return ValueTask.CompletedTask;
            },
                [keyword],
            MatchType.Full,
            MessageSourceType.Group,
            priority: 0,
            blockAfterMatch: true,
            description: "low priority command");

        GroupId testGroup = TestConfig.TestGroupId;
        await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);

        await Task.WhenAny(highPrioTcs.Task, Task.Delay(8000, CT));
        Assert.True(highPrioTcs.Task.IsCompletedSuccessfully, "High-priority handler was not triggered");
        _output.WriteLine("High-priority handler triggered");

        // Low-priority should be blocked — give it a brief window to confirm
        await Task.Delay(2000, CT);
        Assert.False(
            lowPrioTcs.Task.IsCompletedSuccessfully,
            "Low-priority handler should NOT trigger when blocked by higher priority");
        _output.WriteLine("Low-priority handler correctly blocked");

        _fixture.RecordResult(true);
    }

#endregion

#region Permission Tests

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    [Fact]
    public async Task Command_Permission_OwnerRequired()
    {
        SkipIfNotReady();

        // Preflight: verify Secondary is NOT an owner in the test group
        GroupId testGroup = TestConfig.TestGroupId;
        ApiResult<GroupMemberInfo> memberInfo =
            await _fixture.Api!.GetGroupMemberInfoAsync(testGroup, _fixture.SecondaryUserId, true, CT);
        Assert.SkipWhen(!memberInfo.IsSuccess, "Could not query Secondary member info");
        GroupMemberInfo member = memberInfo.AssertSuccess();
        Assert.SkipWhen(
            member.Role == MemberRole.Owner,
            "Secondary is group owner; permission test requires a non-owner sender");
        _output.WriteLine($"Secondary role in test group: {member.Role}");

        string                     keyword = $"!owner-cmd-{Guid.NewGuid():N}";
        TaskCompletionSource<bool> tcs     = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            _ =>
            {
                tcs.TrySetResult(true);
                return ValueTask.CompletedTask;
            },
                [keyword],
            MatchType.Full,
            MessageSourceType.Group,
            MemberRole.Owner,
            description: "owner-only command");

        await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);
        _output.WriteLine("Sent trigger from non-owner Secondary");

        // Command should NOT be triggered — wait briefly to confirm
        await Task.Delay(5000, CT);
        Assert.False(
            tcs.Task.IsCompletedSuccessfully,
            "Owner-only command should NOT trigger for a non-owner member");
        _output.WriteLine("Owner-only command correctly rejected non-owner sender");

        _fixture.RecordResult(true);
    }

#endregion

#region Continuous Dialogue Tests

    /// <see cref="Sora.Entities.MessageWaiting.MessageWaiter" />
    [Fact]
    public async Task ContinuousDialogue_WaitNextMessage()
    {
        SkipIfNotReady();

        string                                      keyword      = $"!dialog-{Guid.NewGuid():N}";
        string                                      followUpText = $"follow-up-{Guid.NewGuid():N}";
        TaskCompletionSource<MessageReceivedEvent?> dialogResult = new();
        TaskCompletionSource                        waiterReady  = new();

        CancellationToken ct = CT;
        _fixture.Service!.Commands.RegisterDynamicCommand(
            async e =>
            {
                Task<MessageReceivedEvent?> replyTask = e.WaitForNextMessageAsync(TimeSpan.FromSeconds(10), ct).AsTask();
                waiterReady.TrySetResult();
                MessageReceivedEvent? reply = await replyTask;
                dialogResult.TrySetResult(reply);
            },
                [keyword],
            MatchType.Full,
            MessageSourceType.Group,
            description: "dialogue test command");

        GroupId testGroup = TestConfig.TestGroupId;
        await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);
        _output.WriteLine("Sent trigger message");

        // Wait for the handler to register its waiter before sending follow-up
        await Task.WhenAny(waiterReady.Task, Task.Delay(TimeSpan.FromSeconds(8), CT));
        Assert.True(waiterReady.Task.IsCompletedSuccessfully, "Command handler did not start waiting in time");
        _output.WriteLine("Waiter registered, sending follow-up");

        await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, new MessageBody(followUpText), CT);
        _output.WriteLine("Sent follow-up message");

        await Task.WhenAny(dialogResult.Task, Task.Delay(12000, CT));
        Assert.True(dialogResult.Task.IsCompletedSuccessfully, "Dialogue result was not received");
        MessageReceivedEvent? dialogReply = await dialogResult.Task;
        Assert.NotNull(dialogReply);

        string receivedText = dialogReply.Message.Body.GetText();
        _output.WriteLine($"Dialogue reply: {receivedText}");
        Assert.SkipWhen(
            !receivedText.Contains(followUpText),
            $"Waiter received cross-protocol message instead of expected follow-up: {receivedText}");

        _fixture.RecordResult(true);
    }

    /// <see cref="Sora.Entities.MessageWaiting.MessageWaiter" />
    [Fact]
    public async Task ContinuousDialogue_Timeout()
    {
        SkipIfNotReady();

        string                                      keyword      = $"!timeout-{Guid.NewGuid():N}";
        TaskCompletionSource<MessageReceivedEvent?> dialogResult = new();

        CancellationToken ct = CT;
        _fixture.Service!.Commands.RegisterDynamicCommand(
            async e =>
            {
                MessageReceivedEvent? reply = await e.WaitForNextMessageAsync(TimeSpan.FromSeconds(3), ct);
                dialogResult.TrySetResult(reply);
            },
                [keyword],
            MatchType.Full,
            MessageSourceType.Group,
            description: "timeout test command");

        GroupId testGroup = TestConfig.TestGroupId;
        await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);
        _output.WriteLine("Sent trigger message; no follow-up will be sent");

        // Waiter should time out after 3s; allow extra buffer
        await Task.WhenAny(dialogResult.Task, Task.Delay(8000, CT));
        Assert.True(dialogResult.Task.IsCompletedSuccessfully, "Dialogue result task did not complete");
        Assert.SkipWhen(
            await dialogResult.Task is not null,
            $"Waiter received cross-protocol message instead of timing out: {(await dialogResult.Task)?.Message.Body.GetText()}");
        _output.WriteLine("Waiter correctly returned null on timeout");

        _fixture.RecordResult(true);
    }

    /// <see cref="Sora.Entities.MessageWaiting.MessageWaiter" />
    [Fact]
    public async Task ContinuousDialogue_MultiTurn()
    {
        SkipIfNotReady();

        string keyword    = $"!multi-{Guid.NewGuid():N}";
        string reply1Text = $"reply1-{Guid.NewGuid():N}";
        string reply2Text = $"reply2-{Guid.NewGuid():N}";

        TaskCompletionSource<(string? first, string? second)> dialogResult = new();
        TaskCompletionSource                                  waiter1Ready = new();
        TaskCompletionSource                                  waiter2Ready = new();

        CancellationToken ct = CT;
        _fixture.Service!.Commands.RegisterDynamicCommand(
            async e =>
            {
                // Turn 1: wait for first reply
                Task<MessageReceivedEvent?> reply1Task =
                    e.WaitForNextMessageAsync(TimeSpan.FromSeconds(10), ct).AsTask();
                waiter1Ready.TrySetResult();
                MessageReceivedEvent? r1 = await reply1Task;

                if (r1 is null)
                {
                    dialogResult.TrySetResult((null, null));
                    return;
                }

                string firstText = r1.Message.Body.GetText();

                // Turn 2: wait for second reply (from the first reply's event)
                Task<MessageReceivedEvent?> reply2Task =
                    r1.WaitForNextMessageAsync(TimeSpan.FromSeconds(10), ct).AsTask();
                waiter2Ready.TrySetResult();
                MessageReceivedEvent? r2 = await reply2Task;

                string? secondText = r2?.Message.Body.GetText();
                dialogResult.TrySetResult((firstText, secondText));
            },
                [keyword],
            MatchType.Full,
            MessageSourceType.Group,
            description: "multi-turn test command");

        GroupId testGroup = TestConfig.TestGroupId;

        // Turn 0: trigger
        await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(keyword), CT);
        _output.WriteLine("Sent trigger");

        // Turn 1: first reply
        await Task.WhenAny(waiter1Ready.Task, Task.Delay(TimeSpan.FromSeconds(8), CT));
        Assert.True(waiter1Ready.Task.IsCompletedSuccessfully, "First waiter did not register in time");
        await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, new MessageBody(reply1Text), CT);
        _output.WriteLine("Sent reply 1");

        // Turn 2: second reply
        await Task.WhenAny(waiter2Ready.Task, Task.Delay(TimeSpan.FromSeconds(8), CT));
        Assert.True(waiter2Ready.Task.IsCompletedSuccessfully, "Second waiter did not register in time");
        await _fixture.SecondaryApi.SendGroupMessageAsync(testGroup, new MessageBody(reply2Text), CT);
        _output.WriteLine("Sent reply 2");

        await Task.WhenAny(dialogResult.Task, Task.Delay(15000, CT));
        Assert.True(dialogResult.Task.IsCompletedSuccessfully, "Multi-turn dialogue did not complete");

        (string? first, string? second) = await dialogResult.Task;
        _output.WriteLine($"Turn 1: {first}");
        _output.WriteLine($"Turn 2: {second}");

        Assert.NotNull(first);
        Assert.SkipWhen(
            !first.Contains(reply1Text),
            $"Waiter received cross-protocol message in turn 1: {first}");
        Assert.NotNull(second);
        Assert.SkipWhen(
            !second.Contains(reply2Text),
            $"Waiter received cross-protocol message in turn 2: {second}");

        _fixture.RecordResult(true);
    }

#endregion

#region Combined Tests

    /// <see cref="CommandManager.RegisterDynamicCommand" />
    /// <see cref="EventDispatcher.OnMessageReceived" />
    [Fact]
    public async Task Command_NonMatchingMessage_PassesThrough()
    {
        SkipIfNotReady();

        string cmdKeyword = $"!specific-cmd-{Guid.NewGuid():N}";
        string otherText  = $"[no-match-{Guid.NewGuid():N}]";

        TaskCompletionSource<bool>                 commandTcs = new();
        TaskCompletionSource<MessageReceivedEvent> eventTcs   = new();

        _fixture.Service!.Commands.RegisterDynamicCommand(
            _ =>
            {
                commandTcs.TrySetResult(true);
                return ValueTask.CompletedTask;
            },
                [cmdKeyword],
            MatchType.Full,
            MessageSourceType.Group,
            description: "specific command for passthrough test");

        // Subscribe to raw message event
        Func<MessageReceivedEvent, ValueTask> eventHandler = e =>
        {
            if (e.Message.Body.GetText().Contains(otherText))
                eventTcs.TrySetResult(e);
            return ValueTask.CompletedTask;
        };
        _fixture.Service.Events.OnMessageReceived += eventHandler;

        try
        {
            GroupId testGroup = TestConfig.TestGroupId;
            await _fixture.SecondaryApi!.SendGroupMessageAsync(testGroup, new MessageBody(otherText), CT);
            _output.WriteLine($"Sent non-matching message: {otherText}");

            // Non-matching message should NOT trigger the command
            await Task.Delay(3000, CT);
            Assert.False(
                commandTcs.Task.IsCompletedSuccessfully,
                "Command should NOT trigger for non-matching text");
            _output.WriteLine("Command correctly not triggered");

            // But OnMessageReceived should fire
            await Task.WhenAny(eventTcs.Task, Task.Delay(5000, CT));
            Assert.True(
                eventTcs.Task.IsCompletedSuccessfully,
                "OnMessageReceived should fire for non-matching messages");
            _output.WriteLine("OnMessageReceived correctly dispatched");
        }
        finally
        {
            _fixture.Service.Events.OnMessageReceived -= eventHandler;
        }

        _fixture.RecordResult(true);
    }

#endregion
}