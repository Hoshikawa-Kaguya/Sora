namespace Sora.Adapter.OneBot11.Models;

/// <summary>A friend category (group) from the OB11 categorized friends list. OB11-specific.</summary>
public sealed record FriendCategory
{
    /// <summary>Category identifier.</summary>
    public int CategoryId { get; init; }

    /// <summary>Category display name.</summary>
    public string CategoryName { get; init; } = "";

    /// <summary>Category sort order.</summary>
    public int SortId { get; init; }

    /// <summary>Number of friends in this category.</summary>
    public int FriendCount { get; init; }

    /// <summary>Number of online friends in this category.</summary>
    public int OnlineCount { get; init; }

    /// <summary>Friends in this category.</summary>
    public IReadOnlyList<FriendInfo> Friends { get; init; } = [];
}