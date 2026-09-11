using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Service actor policies and their effect on command permissions.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class UserPolicyTests
{
    /// <summary>Blocked actors never reach automatic reads, waiters, filters or commands, even if also super users.</summary>
    [Fact]
    public async Task BlockedActor_StopsBeforeAllProcessing()
    {
        EventPipelineFilterTests.MockAdapter adapter = new();
        await using SoraService service = new(
            adapter,
            new PolicyConfig
            {
                BlockUsers = [200L], SuperUsers = [200L], AutoMarkMessageRead = true
            });
        MessageReceivedEvent message = Message(200L);
        service.UseEventPreFilter(new CallbackFilter(_ => Assert.Fail("Blocked event reached a filter")));
        service.Events.OnMessageReceived += _ => throw new InvalidOperationException("Blocked event was dispatched");
        service.Commands.RegisterDynamicCommand(
            _ => throw new InvalidOperationException("Blocked command ran"),
            ["policy"]);

        // A null API would fail if the automatic read path were reached.
        await adapter.RaiseEventAsync(message);

        Assert.False(message.IsContinueEventChain);
        Assert.Null(message.Waiter);
        Assert.Null(message.PipelineContext);
        Assert.False(message.IsSuperUser);
    }

    /// <summary>Moderation policies use the operator, not the affected member.</summary>
    [Fact]
    public async Task Moderation_UsesActorInsteadOfTarget()
    {
        EventPipelineFilterTests.MockAdapter adapter = new();
        await using SoraService service = new(adapter, new PolicyConfig { BlockUsers = [200L], SuperUsers = [300L] });
        List<BotEvent> delivered = [];
        service.UseEventPreFilter(new CallbackFilter(delivered.Add));
        GroupMuteEvent blocked = new() { Api = null!, OperatorId = 200L, UserId = 300L };
        GroupMuteEvent allowed = new() { Api = null!, OperatorId = 300L, UserId = 200L };
        ConnectedEvent system  = new() { Api = null!, SelfId     = 200L };

        await adapter.RaiseEventAsync(blocked);
        await adapter.RaiseEventAsync(allowed);
        await adapter.RaiseEventAsync(system);

        Assert.Equal([allowed, system], delivered);
        Assert.True(allowed.IsSuperUser);
        Assert.False(system.IsSuperUser);
    }

    /// <summary>Group disband policies use the operator before filters and typed handlers.</summary>
    [Fact]
    public async Task GroupDisband_UsesOperatorBeforeDispatch()
    {
        EventPipelineFilterTests.MockAdapter adapter = new();
        await using SoraService service = new(adapter, new PolicyConfig { BlockUsers = [200L], SuperUsers = [300L] });
        List<BotEvent> filtered = [];
        List<GroupDisbandedEvent> dispatched = [];
        bool markedBeforeFilter = false;
        service.UseEventPreFilter(
            new CallbackFilter(e =>
            {
                filtered.Add(e);
                markedBeforeFilter = e.IsSuperUser;
            }));
        service.Events.OnGroupDisbanded += e =>
        {
            dispatched.Add(e);
            return ValueTask.CompletedTask;
        };
        GroupDisbandedEvent blocked = new() { Api = null!, GroupId = 300L, OperatorId = 200L };
        GroupDisbandedEvent allowed = new() { Api = null!, GroupId = 200L, OperatorId = 300L };

        await adapter.RaiseEventAsync(blocked);
        await adapter.RaiseEventAsync(allowed);

        Assert.Equal([allowed], filtered);
        Assert.Equal([allowed], dispatched);
        Assert.True(markedBeforeFilter);
        Assert.False(blocked.IsContinueEventChain);
        Assert.Null(blocked.PipelineContext);
    }

    /// <summary>Super-user restrictions apply to scanned and dynamic commands without overriding member roles.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuperUserOnly_RequiresBothPermissions(bool dynamicCommand)
    {
        EventPipelineFilterTests.MockAdapter adapter  = new();
        await using SoraService              service  = new(adapter, new PolicyConfig { SuperUsers = [300L] });
        RestrictedCommands                   commands = new();
        if (dynamicCommand)
        {
            service.Commands.RegisterDynamicCommand(
                commands.Run,
                ["policy"],
                permissionLevel: MemberRole.Admin,
                superUserOnly: true);
        }
        else
        {
            service.Commands.RegisterCommandInstance(commands);
            service.Commands.ScanType(typeof(RestrictedCommands));
        }

        await adapter.RaiseEventAsync(Message(200L, MemberRole.Owner));
        await adapter.RaiseEventAsync(Message(300L));
        Assert.Equal(0, commands.Calls);
        MessageReceivedEvent permitted = Message(300L, MemberRole.Admin);
        await adapter.RaiseEventAsync(permitted);
        Assert.Equal(1, commands.Calls);
        Assert.True(permitted.IsSuperUser);
    }

    /// <summary>Request senders are marked before filters read the event.</summary>
    [Fact]
    public async Task FriendRequest_MarksSenderBeforeFilters()
    {
        EventPipelineFilterTests.MockAdapter adapter = new();
        await using SoraService              service = new(adapter, new PolicyConfig { SuperUsers = [300L] });
        bool                                 markedBeforeFilter = false;
        service.UseEventPreFilter(new CallbackFilter(e => markedBeforeFilter = e.IsSuperUser));
        await adapter.RaiseEventAsync(new FriendRequestEvent { Api = null!, FromUserId = 300L });
        Assert.True(markedBeforeFilter);
    }

    private static MessageReceivedEvent Message(long senderId, MemberRole role = MemberRole.Member) => new()
    {
        Api = null!, ConnectionId = Guid.NewGuid(),
        Message = new MessageContext
        {
            SourceType = MessageSourceType.Group, GroupId = 100L, SenderId = senderId, Body = new MessageBody("policy")
        },
        Member = new GroupMemberInfo { UserId = senderId, GroupId = 100L, Role = role }
    };

    private sealed class PolicyConfig : IBotServiceConfig
    {
        public UserId[] BlockUsers           { get; init; } = [];
        public UserId[] SuperUsers           { get; init; } = [];
        public bool     EnableCommandManager => true;
        public bool     AutoMarkMessageRead  { get; init; }
    }

    private sealed class CallbackFilter : IEventPreFilter
    {
        private readonly Action<BotEvent> _callback;

        public CallbackFilter(Action<BotEvent> callback)
        {
            _callback = callback;
        }

        public ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct)
        {
            _callback(e);
            return new ValueTask<bool>(true);
        }
    }

    private sealed class RestrictedCommands
    {
        public int Calls { get; private set; }

        [Command(Expressions = ["policy"], SuperUserOnly = true, PermissionLevel = MemberRole.Admin)]
        public ValueTask Run(MessageReceivedEvent message)
        {
            Calls++;
            return ValueTask.CompletedTask;
        }
    }
}