namespace Sora.Entities.Info;

/// <summary>Basic user information.</summary>
public sealed record UserInfo
{
    /// <summary>User's unique identifier.</summary>
    public UserId UserId { get; internal init; }

    /// <summary>User's display name.</summary>
    public string Nickname { get; internal init; } = "";

    /// <summary>User's gender.</summary>
    public Sex Sex { get; internal init; }
}