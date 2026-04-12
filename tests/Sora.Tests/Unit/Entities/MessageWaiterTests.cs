using Sora.Entities.MessageWaiting;
using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Tests for the MessageWaiter continuous command system.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class MessageWaiterTests
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    private static readonly Guid TestConnectionId = Guid.NewGuid();

#region Basic Wait Tests

    /// <see cref="MessageWaiter" />
    [Fact]
    public async Task WaitForNextMessage_AnyContent_ReceivesReply()
    {
        MessageWaiter        waiter = new();
        MessageReceivedEvent source = MakeEvent(waiter, 1L, "hello");

        ValueTask<MessageReceivedEvent?> waitTask = source.WaitForNextMessageAsync(TimeSpan.FromSeconds(5), CT);

        // Simulate incoming reply
        MessageReceivedEvent reply   = MakeEvent(waiter, 1L, "my reply");
        bool                 matched = waiter.TryMatch(reply);

        Assert.True(matched);

        MessageReceivedEvent? result   = await waitTask;
        MessageReceivedEvent  received = Assert.IsType<MessageReceivedEvent>(result);
        Assert.Equal("my reply", received.Message.Body.GetText());
    }

    /// <see cref="MessageWaiter" />
    [Fact]
    public async Task WaitForNextMessage_Timeout_ReturnsNull()
    {
        MessageWaiter        waiter = new();
        MessageReceivedEvent source = MakeEvent(waiter, 1L, "hello");

        MessageReceivedEvent? result = await source.WaitForNextMessageAsync(TimeSpan.FromMilliseconds(50), CT);

        Assert.Null(result);
    }

    /// <see cref="MessageWaiter" />
    [Fact]
    public async Task WaitForNextMessage_MultipleSenders_Independent()
    {
        MessageWaiter        waiter  = new();
        MessageReceivedEvent source1 = MakeEvent(waiter, 1L, "cmd1");
        MessageReceivedEvent source2 = MakeEvent(waiter, 2L, "cmd2");

        ValueTask<MessageReceivedEvent?> wait1 = source1.WaitForNextMessageAsync(TimeSpan.FromSeconds(5), CT);
        ValueTask<MessageReceivedEvent?> wait2 = source2.WaitForNextMessageAsync(TimeSpan.FromSeconds(5), CT);

        // Reply from user 2
        MessageReceivedEvent reply2 = MakeEvent(waiter, 2L, "answer2");
        Assert.True(waiter.TryMatch(reply2));

        MessageReceivedEvent? result2   = await wait2;
        MessageReceivedEvent  received2 = Assert.IsType<MessageReceivedEvent>(result2);
        Assert.Equal("answer2", received2.Message.Body.GetText());

        // Reply from user 1
        MessageReceivedEvent reply1 = MakeEvent(waiter, 1L, "answer1");
        Assert.True(waiter.TryMatch(reply1));

        MessageReceivedEvent? result1   = await wait1;
        MessageReceivedEvent  received1 = Assert.IsType<MessageReceivedEvent>(result1);
        Assert.Equal("answer1", received1.Message.Body.GetText());
    }

#endregion

#region Pattern Matching Tests

    /// <see cref="MessageWaiter" />
    [Fact]
    public async Task WaitForNextMessage_FullMatch()
    {
        MessageWaiter        waiter = new();
        MessageReceivedEvent source = MakeEvent(waiter, 1L, "cmd");

        ValueTask<MessageReceivedEvent?> waitTask = source.WaitForNextMessageAsync(
                ["confirm"],
            MatchType.Full,
            TimeSpan.FromSeconds(5),
            CT);

        MessageReceivedEvent partial = MakeEvent(waiter, 1L, "confirm please");
        Assert.False(waiter.TryMatch(partial));

        MessageReceivedEvent exact = MakeEvent(waiter, 1L, "confirm");
        Assert.True(waiter.TryMatch(exact));

        MessageReceivedEvent? result = await waitTask;
        Assert.NotNull(result);
    }

    /// <see cref="MessageWaiter" />
    [Fact]
    public async Task WaitForNextMessage_RegexMatch()
    {
        MessageWaiter        waiter = new();
        MessageReceivedEvent source = MakeEvent(waiter, 1L, "choose");

        ValueTask<MessageReceivedEvent?> waitTask = source.WaitForNextMessageAsync(
                ["^(yes|no)$"],
            MatchType.Regex,
            TimeSpan.FromSeconds(5),
            CT);

        // Non-matching message should not trigger
        MessageReceivedEvent unrelated = MakeEvent(waiter, 1L, "maybe");
        Assert.False(waiter.TryMatch(unrelated));

        // Matching message should trigger
        MessageReceivedEvent match = MakeEvent(waiter, 1L, "yes");
        Assert.True(waiter.TryMatch(match));

        MessageReceivedEvent? result   = await waitTask;
        MessageReceivedEvent  received = Assert.IsType<MessageReceivedEvent>(result);
        Assert.Equal("yes", received.Message.Body.GetText());
    }

    /// <see cref="MessageWaiter" />
    [Fact]
    public async Task WaitForNextMessage_KeywordMatch()
    {
        MessageWaiter        waiter = new();
        MessageReceivedEvent source = MakeEvent(waiter, 1L, "cmd");

        ValueTask<MessageReceivedEvent?> waitTask = source.WaitForNextMessageAsync(
                ["cancel"],
            MatchType.Keyword,
            TimeSpan.FromSeconds(5),
            CT);

        MessageReceivedEvent msg = MakeEvent(waiter, 1L, "I want to cancel now");
        Assert.True(waiter.TryMatch(msg));

        MessageReceivedEvent? result = await waitTask;
        Assert.NotNull(result);
    }

    /// <see cref="MessageWaiter" />
    [Fact]
    public async Task WaitForNextMessage_CustomPredicate()
    {
        MessageWaiter        waiter = new();
        MessageReceivedEvent source = MakeEvent(waiter, 1L, "upload");

        ValueTask<MessageReceivedEvent?> waitTask = source.WaitForNextMessageAsync(
            e => e.Message.Body.GetFirst<ImageSegment>() is not null,
            TimeSpan.FromSeconds(5),
            CT);

        // Text-only message should not match
        MessageReceivedEvent textOnly = MakeEvent(waiter, 1L, "no image here");
        Assert.False(waiter.TryMatch(textOnly));

        // Message with image should match
        MessageReceivedEvent withImage = new()
            {
                ConnectionId = TestConnectionId,
                SelfId       = 999L,
                Time         = DateTime.Now,
                Api          = null!,
                Message = new MessageContext
                    {
                        SenderId   = 1L,
                        SourceType = MessageSourceType.Friend,
                        Body = MessageBody.FromIncoming(
                            [
                                new TextSegment { Text = "here" },
                                new ImageSegment
                                        { Url = "http://img.png" }
                            ])
                    },
                Sender = new UserInfo { UserId = 1L, Nickname = "User" }
            };
        withImage.Waiter = waiter;
        Assert.True(waiter.TryMatch(withImage));

        MessageReceivedEvent? result = await waitTask;
        Assert.NotNull(result);
    }

#endregion

#region Source Matching Tests

    /// <see cref="MessageWaiter.TryMatch" />
    [Fact]
    public async Task WaitForNextMessage_DifferentSender_DoesNotMatch()
    {
        MessageWaiter        waiter = new();
        MessageReceivedEvent source = MakeEvent(waiter, 1L, "hello");

        ValueTask<MessageReceivedEvent?> waitTask = source.WaitForNextMessageAsync(TimeSpan.FromMilliseconds(100), CT);

        // Different sender
        MessageReceivedEvent other = MakeEvent(waiter, 2L, "reply");
        Assert.False(waiter.TryMatch(other));

        MessageReceivedEvent? result = await waitTask;
        Assert.Null(result); // Timed out
    }

    /// <see cref="MessageWaiter.TryMatch" />
    [Fact]
    public async Task WaitForNextMessage_DifferentGroup_DoesNotMatch()
    {
        MessageWaiter        waiter = new();
        MessageReceivedEvent source = MakeEvent(waiter, 1L, "hello", 100L, MessageSourceType.Group);

        ValueTask<MessageReceivedEvent?> waitTask = source.WaitForNextMessageAsync(TimeSpan.FromMilliseconds(100), CT);

        // Same sender but different group
        MessageReceivedEvent otherGroup = MakeEvent(waiter, 1L, "reply", 200L, MessageSourceType.Group);
        Assert.False(waiter.TryMatch(otherGroup));

        MessageReceivedEvent? result = await waitTask;
        Assert.Null(result);
    }

    /// <see cref="MessageWaiter.TryMatch" />
    [Fact]
    public async Task WaitForNextMessage_SameGroup_Matches()
    {
        MessageWaiter        waiter = new();
        MessageReceivedEvent source = MakeEvent(waiter, 1L, "hello", 100L, MessageSourceType.Group);

        ValueTask<MessageReceivedEvent?> waitTask = source.WaitForNextMessageAsync(TimeSpan.FromSeconds(5), CT);

        MessageReceivedEvent reply = MakeEvent(waiter, 1L, "reply", 100L, MessageSourceType.Group);
        Assert.True(waiter.TryMatch(reply));

        MessageReceivedEvent? result = await waitTask;
        Assert.NotNull(result);
    }

#endregion

#region Cancellation and Edge Cases Tests

    /// <see cref="MessageWaiter" />
    [Fact]
    public async Task WaitForNextMessage_Cancellation_ReturnsNull()
    {
        MessageWaiter           waiter = new();
        MessageReceivedEvent    source = MakeEvent(waiter, 1L, "hello");
        CancellationTokenSource cts    = new();

        ValueTask<MessageReceivedEvent?> waitTask = source.WaitForNextMessageAsync(null, cts.Token);

        await cts.CancelAsync();

        MessageReceivedEvent? result = await waitTask;
        Assert.Null(result);
    }

    /// <see cref="MessageWaiter" />
    [Fact]
    public async Task WaitForNextMessage_DuplicateSource_ReturnsNull()
    {
        MessageWaiter        waiter = new();
        MessageReceivedEvent source = MakeEvent(waiter, 1L, "hello");

        // First wait
        ValueTask<MessageReceivedEvent?> wait1 = source.WaitForNextMessageAsync(TimeSpan.FromSeconds(5), CT);

        // Second wait from same source should return null immediately
        MessageReceivedEvent? result2 = await source.WaitForNextMessageAsync(TimeSpan.FromSeconds(5), CT);
        Assert.Null(result2);

        // Complete the first wait
        MessageReceivedEvent reply = MakeEvent(waiter, 1L, "reply");
        waiter.TryMatch(reply);
        MessageReceivedEvent? result1 = await wait1;
        Assert.NotNull(result1);
    }

    /// <see cref="MessageWaiter" />
    [Fact]
    public async Task WaitForNextMessage_WithoutWaiter_ThrowsInvalidOperation()
    {
        MessageReceivedEvent source = new()
            {
                ConnectionId = TestConnectionId,
                SelfId       = 999L,
                Time         = DateTime.Now,
                Api          = null!,
                Message =
                    new MessageContext { SenderId = 1L, Body = new MessageBody("test") },
                Sender = new UserInfo { UserId = 1L, Nickname = "User" }
            };

        // No Waiter set — should throw
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await source.WaitForNextMessageAsync(TimeSpan.FromMilliseconds(10), CT));
    }

#endregion

#region Dispose Tests

    /// <see cref="MessageWaiter.DisposeAll" />
    [Fact]
    public async Task DisposeAll_CancelsAllWaiters()
    {
        MessageWaiter        waiter  = new();
        MessageReceivedEvent source1 = MakeEvent(waiter, 1L, "cmd1");
        MessageReceivedEvent source2 = MakeEvent(waiter, 2L, "cmd2");

        ValueTask<MessageReceivedEvent?> wait1 = source1.WaitForNextMessageAsync(TimeSpan.FromSeconds(30), CT);
        ValueTask<MessageReceivedEvent?> wait2 = source2.WaitForNextMessageAsync(TimeSpan.FromSeconds(30), CT);

        waiter.DisposeAll();

        MessageReceivedEvent? result1 = await wait1;
        MessageReceivedEvent? result2 = await wait2;
        Assert.Null(result1);
        Assert.Null(result2);
    }

    /// <see cref="MessageWaiter.DisposeConnection" />
    [Fact]
    public async Task DisposeConnection_OnlyCancelsMatchingConnection()
    {
        MessageWaiter waiter = new();
        Guid          conn1  = Guid.NewGuid();
        Guid          conn2  = Guid.NewGuid();

        MessageReceivedEvent source1 = MakeEvent(waiter, 1L, "cmd", connId: conn1);
        MessageReceivedEvent source2 = MakeEvent(waiter, 2L, "cmd", connId: conn2);

        ValueTask<MessageReceivedEvent?> wait1 = source1.WaitForNextMessageAsync(TimeSpan.FromSeconds(30), CT);
        ValueTask<MessageReceivedEvent?> wait2 = source2.WaitForNextMessageAsync(TimeSpan.FromSeconds(30), CT);

        // Dispose only conn1
        waiter.DisposeConnection(conn1);

        MessageReceivedEvent? result1 = await wait1;
        Assert.Null(result1);

        // conn2's waiter should still be active
        MessageReceivedEvent reply2 = MakeEvent(waiter, 2L, "answer", connId: conn2);
        Assert.True(waiter.TryMatch(reply2));

        MessageReceivedEvent? result2 = await wait2;
        Assert.NotNull(result2);
    }

#endregion

    /// <summary>Creates a synthetic MessageReceivedEvent with waiter injected for testing.</summary>
    private static MessageReceivedEvent MakeEvent(
        MessageWaiter     waiter,
        UserId            senderId,
        string            text,
        GroupId           groupId    = default,
        MessageSourceType sourceType = MessageSourceType.Friend,
        Guid?             connId     = null)
    {
        MessageReceivedEvent evt = new()
            {
                ConnectionId = connId ?? TestConnectionId,
                SelfId       = 999L,
                Time         = DateTime.Now,
                Api          = null!,
                Message = new MessageContext
                    {
                        SenderId   = senderId,
                        GroupId    = groupId,
                        SourceType = sourceType,
                        Body       = new MessageBody(text)
                    },
                Sender = new UserInfo { UserId   = senderId, Nickname = "TestUser" },
                Group  = new GroupInfo { GroupId = groupId, GroupName = "TestGroup" },
                Waiter = waiter
            };
        return evt;
    }
}