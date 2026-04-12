using Sora.Entities.MessageWaiting;

namespace Sora.Entities.Events;

/// <summary>
///     Base class for all bot events.
/// </summary>
public abstract record BotEvent
{
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
}