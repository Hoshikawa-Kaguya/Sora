namespace Sora.Entities;

/// <summary>
///     Dispatches bot events to registered handlers.
/// </summary>
public sealed class EventDispatcher
{
    private readonly Lazy<ILogger> _loggerLazy = new(SoraLogger.CreateLogger<EventDispatcher>);
    private          ILogger       _logger => _loggerLazy.Value;

#region Connection Events

    /// <summary>Raised when connected to the protocol server.</summary>
    public event Func<ConnectedEvent, ValueTask>? OnConnected;

    /// <summary>Raised when disconnected from the protocol server.</summary>
    public event Func<DisconnectedEvent, ValueTask>? OnDisconnected;

#endregion

#region Message Events

    /// <summary>Raised when a message is received.</summary>
    public event Func<MessageReceivedEvent, ValueTask>? OnMessageReceived;

    /// <summary>Raised when a message is recalled/deleted.</summary>
    public event Func<MessageDeletedEvent, ValueTask>? OnMessageDeleted;

#endregion

#region Friend Events

    /// <summary>Raised when a new friend is added.</summary>
    public event Func<FriendAddedEvent, ValueTask>? OnFriendAdded;

    /// <summary>Raised when a friend request is received.</summary>
    public event Func<FriendRequestEvent, ValueTask>? OnFriendRequest;

    /// <summary>Raised when a nudge (poke) is received.</summary>
    public event Func<NudgeEvent, ValueTask>? OnNudge;

#endregion

#region Group Notice Events

    /// <summary>Raised when a group admin is added or removed.</summary>
    public event Func<GroupAdminChangedEvent, ValueTask>? OnGroupAdminChanged;

    /// <summary>Raised when a group essence message changes.</summary>
    public event Func<GroupEssenceChangedEvent, ValueTask>? OnGroupEssenceChanged;

    /// <summary>Raised when a member is muted/unmuted or group-wide mute toggles.</summary>
    public event Func<GroupMuteEvent, ValueTask>? OnGroupMute;

    /// <summary>Raised when a group name is changed.</summary>
    public event Func<GroupNameChangedEvent, ValueTask>? OnGroupNameChanged;

    /// <summary>Raised when a group message reaction changes. Milky-specific.</summary>
    public event Func<GroupReactionEvent, ValueTask>? OnGroupReaction;

    /// <summary>Raised when a conversation pin (top) state changes. Milky-specific.</summary>
    public event Func<PeerPinChangedEvent, ValueTask>? OnPeerPinChanged;

#endregion

#region Group Member Events

    /// <summary>Raised when a member joins a group.</summary>
    public event Func<MemberJoinedEvent, ValueTask>? OnMemberJoined;

    /// <summary>Raised when a member leaves or is kicked from a group.</summary>
    public event Func<MemberLeftEvent, ValueTask>? OnMemberLeft;

    /// <summary>Raised when a file is uploaded.</summary>
    public event Func<FileUploadEvent, ValueTask>? OnFileUpload;

#endregion

#region Group Request Events

    /// <summary>Raised when a group invitation is received.</summary>
    public event Func<GroupInvitationEvent, ValueTask>? OnGroupInvitation;

    /// <summary>Raised when a group join request is received.</summary>
    public event Func<GroupJoinRequestEvent, ValueTask>? OnGroupJoinRequest;

#endregion

#region Catch-All

    /// <summary>Raised for every event, regardless of type.</summary>
    public event Func<BotEvent, ValueTask>? OnEvent;

#endregion

    /// <summary>
    ///     Dispatches an event to all matching registered handlers.
    /// </summary>
    /// <param name="e">The event to dispatch.</param>
    /// <param name="ct">Cancellation token to interrupt handler invocation.</param>
    internal async ValueTask DispatchAsync(BotEvent e, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Dispatching {EventType} (connection: {ConnectionId}, self: {SelfId})",
            e.GetType().Name,
            e.ConnectionId,
            e.SelfId);

        // Dispatch to catch-all first
        await InvokeHandlersAsync(OnEvent, e, ct);
        if (!e.IsContinueEventChain) return;

        // Dispatch to typed handlers
        switch (e)
        {
            case ConnectedEvent ev:
                await InvokeHandlersAsync(OnConnected, ev, ct);
                break;
            case DisconnectedEvent ev:
                await InvokeHandlersAsync(OnDisconnected, ev, ct);
                break;
            case MessageReceivedEvent ev:
                await InvokeHandlersAsync(OnMessageReceived, ev, ct);
                break;
            case MessageDeletedEvent ev:
                await InvokeHandlersAsync(OnMessageDeleted, ev, ct);
                break;
            case FriendAddedEvent ev:
                await InvokeHandlersAsync(OnFriendAdded, ev, ct);
                break;
            case FriendRequestEvent ev:
                await InvokeHandlersAsync(OnFriendRequest, ev, ct);
                break;
            case NudgeEvent ev:
                await InvokeHandlersAsync(OnNudge, ev, ct);
                break;
            case GroupAdminChangedEvent ev:
                await InvokeHandlersAsync(OnGroupAdminChanged, ev, ct);
                break;
            case GroupEssenceChangedEvent ev:
                await InvokeHandlersAsync(OnGroupEssenceChanged, ev, ct);
                break;
            case GroupMuteEvent ev:
                await InvokeHandlersAsync(OnGroupMute, ev, ct);
                break;
            case GroupNameChangedEvent ev:
                await InvokeHandlersAsync(OnGroupNameChanged, ev, ct);
                break;
            case GroupReactionEvent ev:
                await InvokeHandlersAsync(OnGroupReaction, ev, ct);
                break;
            case PeerPinChangedEvent ev:
                await InvokeHandlersAsync(OnPeerPinChanged, ev, ct);
                break;
            case MemberJoinedEvent ev:
                await InvokeHandlersAsync(OnMemberJoined, ev, ct);
                break;
            case MemberLeftEvent ev:
                await InvokeHandlersAsync(OnMemberLeft, ev, ct);
                break;
            case FileUploadEvent ev:
                await InvokeHandlersAsync(OnFileUpload, ev, ct);
                break;
            case GroupInvitationEvent ev:
                await InvokeHandlersAsync(OnGroupInvitation, ev, ct);
                break;
            case GroupJoinRequestEvent ev:
                await InvokeHandlersAsync(OnGroupJoinRequest, ev, ct);
                break;
        }
    }

    private async ValueTask InvokeHandlersAsync<T>(Func<T, ValueTask>? handler, T e, CancellationToken ct) where T : BotEvent
    {
        if (handler is null) return;

        foreach (Delegate d in handler.GetInvocationList())
        {
            ct.ThrowIfCancellationRequested();
            if (!e.IsContinueEventChain) break;

            try
            {
                _logger.LogTrace("Invoking handler {HandlerName} for {EventType}", d.Method.Name, typeof(T).Name);
                await ((Func<T, ValueTask>)d)(e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event handler threw an unhandled exception for {EventType}", typeof(T).Name);
            }
        }
    }
}