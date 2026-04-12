namespace Sora.Entities.Info;

/// <summary>Detailed user profile information (richer than UserInfo).</summary>
public sealed record UserProfile
{
    /// <summary>User's unique identifier.</summary>
    public UserId UserId { get; internal init; }

    /// <summary>User's display name.</summary>
    public string Nickname { get; internal init; } = "";

    /// <summary>User's QID.</summary>
    public string Qid { get; internal init; } = "";

    /// <summary>Friend remark (if is friend).</summary>
    public string Remark { get; internal init; } = "";

    /// <summary>User's gender.</summary>
    public Sex Sex { get; internal init; }

    /// <summary>User's bio/signature.</summary>
    public string Bio { get; internal init; } = "";

    /// <summary>User's country/region.</summary>
    public string Country { get; internal init; } = "";

    /// <summary>User's city.</summary>
    public string City { get; internal init; } = "";

    /// <summary>User's school.</summary>
    public string School { get; internal init; } = "";

    /// <summary>User's age.</summary>
    public int Age { get; internal init; }

    /// <summary>User's QQ level.</summary>
    public int Level { get; internal init; }
}