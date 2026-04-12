namespace Sora.Entities.Info;

/// <summary>Friend information.</summary>
public sealed record FriendInfo
{
    /// <summary>Friend's unique identifier.</summary>
    public UserId UserId { get; internal init; }

    /// <summary>Friend's display name.</summary>
    public string Nickname { get; internal init; } = "";

    /// <summary>Friend's QID.</summary>
    public string Qid { get; internal init; } = "";

    /// <summary>Friend remark/alias.</summary>
    public string Remark { get; internal init; } = "";

    /// <summary>Friend's gender.</summary>
    public Sex Sex { get; internal init; }

    /// <summary>Friend category.</summary>
    public FriendCategoryInfo? Category { get; internal init; }
}

/// <summary>Friend category information.</summary>
public sealed record FriendCategoryInfo
{
    /// <summary>Category identifier.</summary>
    public int CategoryId { get; internal init; }

    /// <summary>Category display name.</summary>
    public string CategoryName { get; internal init; } = "";
}