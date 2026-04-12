namespace Sora.Entities.Info;

/// <summary>Group information.</summary>
public sealed record GroupInfo
{
    /// <summary>Group's unique identifier.</summary>
    public GroupId GroupId { get; internal init; }

    /// <summary>Group owner's user ID.</summary>
    public UserId OwnerId { get; internal init; }

    /// <summary>Group's display name.</summary>
    public string GroupName { get; internal init; } = "";

    /// <summary>Group remark (alias set by the user). LLBot extension.</summary>
    public string Remark { get; internal init; } = "";

    /// <summary>Group description / introduction. LLBot extension.</summary>
    public string Description { get; internal init; } = "";

    /// <summary>Group announcement memo. LLBot extension.</summary>
    public string Announcement { get; internal init; } = "";

    /// <summary>Entry question for joining the group. LLBot extension.</summary>
    public string Question { get; internal init; } = "";

    /// <summary>Current number of members.</summary>
    public int MemberCount { get; internal init; }

    /// <summary>Maximum capacity.</summary>
    public int MaxMemberCount { get; internal init; }

    /// <summary>Group creation time as Unix timestamp. LLBot extension.</summary>
    public long CreatedTime { get; internal init; }
}