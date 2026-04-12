namespace Sora.Entities.Info;

/// <summary>Result of a group essence messages query.</summary>
public sealed record GroupEssenceMessagesPage
{
    /// <summary>Essence messages.</summary>
    public IReadOnlyList<GroupEssenceMessageInfo> Messages { get; internal init; } = [];

    /// <summary>Whether this is the last page.</summary>
    public bool IsEnd { get; internal init; }
}