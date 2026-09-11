namespace Sora.Entities.Events;

/// <summary>Raised when a group is disbanded.</summary>
public sealed record GroupDisbandedEvent : BotEvent
{
    /// <summary>The disbanded group.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>The operator who disbanded the group.</summary>
    public UserId OperatorId { get; init; }
}