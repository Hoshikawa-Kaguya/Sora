namespace Sora.Entities.Events;

/// <summary>Raised when a connection to the protocol server is lost.</summary>
public sealed record DisconnectedEvent : BotEvent
{
    /// <summary>Reason for disconnection.</summary>
    public string Reason { get; init; } = "";
}