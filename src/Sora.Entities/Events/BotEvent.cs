using Sora.Entities.MessageWaiting;

namespace Sora.Entities.Events;

/// <summary>
///     Base class for all bot events.
/// </summary>
public abstract record BotEvent
{
    /// <summary>
    ///     Whether the actor is configured in SuperUsers. Set by the service before waiters and filters.
    ///     This flag does not override group member permissions.
    /// </summary>
    public bool IsSuperUser { get; internal set; }

    /// <summary>
    ///     Internal reference to the service's message waiter, set by the event pipeline.
    ///     Used by extension methods to provide transparent WaitForNextMessage support.
    /// </summary>
    internal MessageWaiter? Waiter { get; set; }

    /// <summary>API instance for this connection (convenience).</summary>
    public required IBotApi Api { get; init; }

    /// <summary>The bot account that received this event.</summary>
    public UserId SelfId { get; init; }

    /// <summary>Framework-assigned connection identifier.</summary>
    public Guid ConnectionId { get; init; }

    /// <summary>When the event occurred.</summary>
    public DateTime Time { get; init; }

    /// <summary>
    ///     Set to false in a handler to stop propagation to subsequent handlers.
    /// </summary>
    public bool IsContinueEventChain { get; set; } = true;

    /// <summary>
    ///     Pipeline context shared across filters, commands, and event handlers within this event's processing cycle.
    ///     Initialized by the framework before pre-filters execute. <c>null</c> for waiter-consumed events.
    /// </summary>
    public PipelineContext? PipelineContext { get; internal set; }
}