namespace Sora.Entities.Events;

/// <summary>Raised when a group's name is changed.</summary>
public sealed record GroupNameChangedEvent : BotEvent
{
    /// <summary>The group.</summary>
    public GroupId GroupId { get; init; }

    /// <summary>The new group name.</summary>
    public string NewName { get; init; } = "";

    /// <summary>The operator who changed the name.</summary>
    public UserId OperatorId { get; init; }
}