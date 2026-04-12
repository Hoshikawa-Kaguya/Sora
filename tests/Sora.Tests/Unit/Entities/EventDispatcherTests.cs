using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Tests for <see cref="EventDispatcher" />.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class EventDispatcherTests
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

#region Basic Dispatch Tests

    /// <see cref="EventDispatcher.DispatchAsync" />
    [Fact]
    public async Task DispatchAsync_MessageReceived_InvokesHandler()
    {
        EventDispatcher dispatcher = new();
        bool            invoked    = false;

        dispatcher.OnMessageReceived += async _ =>
        {
            invoked = true;
            await ValueTask.CompletedTask;
        };

        MessageReceivedEvent evt = new()
            {
                Api          = null!,
                ConnectionId = Guid.NewGuid(),
                SelfId       = 100L,
                Time         = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        GroupId    = 200L,
                        SenderId   = 300L,
                        Body       = new MessageBody("test")
                    }
            };

        await dispatcher.DispatchAsync(evt, CT);
        Assert.True(invoked);
    }

    /// <see cref="EventDispatcher.DispatchAsync" />
    [Fact]
    public async Task DispatchAsync_MultipleHandlers_AllInvoked()
    {
        EventDispatcher dispatcher = new();
        int             count      = 0;

        dispatcher.OnMessageReceived += async _ =>
        {
            count++;
            await ValueTask.CompletedTask;
        };
        dispatcher.OnMessageReceived += async _ =>
        {
            count++;
            await ValueTask.CompletedTask;
        };

        MessageReceivedEvent evt = new()
            {
                Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 100L, Time = DateTime.Now,
                Message = new MessageContext
                    {
                        MessageId  = 1,
                        SourceType = MessageSourceType.Group,
                        Body       = new MessageBody("t")
                    }
            };

        await dispatcher.DispatchAsync(evt, CT);
        Assert.Equal(2, count);
    }

#endregion

#region Event Routing Tests

    /// <see cref="EventDispatcher" />
    [Fact]
    public async Task DispatchAsync_AllEventTypes_CorrectHandlerCalled()
    {
        EventDispatcher dispatcher = new();
        string          lastType   = "";

        dispatcher.OnMemberJoined += async _ =>
        {
            lastType = "MemberJoined";
            await ValueTask.CompletedTask;
        };
        dispatcher.OnMemberLeft += async _ =>
        {
            lastType = "MemberLeft";
            await ValueTask.CompletedTask;
        };
        dispatcher.OnGroupAdminChanged += async _ =>
        {
            lastType = "AdminChanged";
            await ValueTask.CompletedTask;
        };
        dispatcher.OnGroupMute += async _ =>
        {
            lastType = "Mute";
            await ValueTask.CompletedTask;
        };
        dispatcher.OnFileUpload += async _ =>
        {
            lastType = "FileUpload";
            await ValueTask.CompletedTask;
        };
        dispatcher.OnNudge += async _ =>
        {
            lastType = "Nudge";
            await ValueTask.CompletedTask;
        };
        dispatcher.OnFriendRequest += async _ =>
        {
            lastType = "FriendReq";
            await ValueTask.CompletedTask;
        };
        dispatcher.OnGroupJoinRequest += async _ =>
        {
            lastType = "GroupJoinReq";
            await ValueTask.CompletedTask;
        };
        dispatcher.OnDisconnected += async _ =>
        {
            lastType = "Disconnected";
            await ValueTask.CompletedTask;
        };

        await dispatcher.DispatchAsync(
            new MemberJoinedEvent
                {
                    Api     = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                    GroupId = 1L,
                    UserId  = 2L
                },
            CT);
        Assert.Equal("MemberJoined", lastType);

        await dispatcher.DispatchAsync(
            new MemberLeftEvent
                {
                    Api     = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                    GroupId = 1L,
                    UserId  = 2L
                },
            CT);
        Assert.Equal("MemberLeft", lastType);

        await dispatcher.DispatchAsync(
            new GroupAdminChangedEvent
                {
                    Api     = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                    GroupId = 1L, UserId          = 2L,
                    IsSet   = true
                },
            CT);
        Assert.Equal("AdminChanged", lastType);

        await dispatcher.DispatchAsync(
            new GroupMuteEvent
                {
                    Api             = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                    GroupId         = 1L, UserId          = 2L,
                    DurationSeconds = 60
                },
            CT);
        Assert.Equal("Mute", lastType);

        await dispatcher.DispatchAsync(
            new FileUploadEvent
                {
                    Api        = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                    SourceType = MessageSourceType.Group,
                    FileId     = "f1", FileName = "test.txt"
                },
            CT);
        Assert.Equal("FileUpload", lastType);

        await dispatcher.DispatchAsync(
            new NudgeEvent
                {
                    Api        = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                    SenderId   = 1L,
                    ReceiverId = 2L
                },
            CT);
        Assert.Equal("Nudge", lastType);

        await dispatcher.DispatchAsync(
            new FriendRequestEvent
                {
                    Api        = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                    FromUserId = 2L
                },
            CT);
        Assert.Equal("FriendReq", lastType);

        await dispatcher.DispatchAsync(
            new GroupJoinRequestEvent
                {
                    Api        = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                    GroupId    = 1L,
                    FromUserId = 2L
                },
            CT);
        Assert.Equal("GroupJoinReq", lastType);

        await dispatcher.DispatchAsync(
            new DisconnectedEvent
                {
                    Api    = null!, ConnectionId = Guid.NewGuid(), SelfId = 1L, Time = DateTime.Now,
                    Reason = "test"
                },
            CT);
        Assert.Equal("Disconnected", lastType);
    }

    /// <see cref="EventDispatcher.OnEvent" />
    [Fact]
    public async Task DispatchAsync_CatchAll_InvokedForAnyEvent()
    {
        EventDispatcher dispatcher = new();
        bool            invoked    = false;

        dispatcher.OnEvent += async _ =>
        {
            invoked = true;
            await ValueTask.CompletedTask;
        };

        ConnectedEvent evt = new()
            {
                Api          = null!,
                ConnectionId = Guid.NewGuid(),
                SelfId       = 100L,
                Time         = DateTime.Now
            };

        await dispatcher.DispatchAsync(evt, CT);
        Assert.True(invoked);
    }

#endregion

#region Error and Propagation Tests

    /// <see cref="EventDispatcher.DispatchAsync" />
    [Fact]
    public async Task DispatchAsync_HandlerException_DoesNotCrash()
    {
        EventDispatcher dispatcher   = new();
        bool            secondCalled = false;

        dispatcher.OnConnected += async _ => { throw new InvalidOperationException("test error"); };
        dispatcher.OnConnected += async _ =>
        {
            secondCalled = true;
            await ValueTask.CompletedTask;
        };

        ConnectedEvent evt = new() { Api = null!, ConnectionId = Guid.NewGuid(), SelfId = 100L, Time = DateTime.Now };
        await dispatcher.DispatchAsync(evt, CT);
        Assert.True(secondCalled);
    }

    /// <see cref="BotEvent.IsContinueEventChain" />
    [Fact]
    public async Task DispatchAsync_StopPropagation()
    {
        EventDispatcher dispatcher    = new();
        bool            handlerCalled = false;

        dispatcher.OnEvent += async e =>
        {
            e.IsContinueEventChain = false;
            await ValueTask.CompletedTask;
        };

        dispatcher.OnConnected += async _ =>
        {
            handlerCalled = true;
            await ValueTask.CompletedTask;
        };

        ConnectedEvent evt = new()
            {
                Api          = null!,
                ConnectionId = Guid.NewGuid(),
                SelfId       = 100L,
                Time         = DateTime.Now
            };

        await dispatcher.DispatchAsync(evt, CT);
        Assert.False(handlerCalled);
    }

#endregion
}