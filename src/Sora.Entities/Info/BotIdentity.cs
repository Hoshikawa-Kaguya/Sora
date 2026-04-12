namespace Sora.Entities.Info;

/// <summary>Bot account identity information.</summary>
public sealed record BotIdentity
{
    /// <summary>Bot's user ID.</summary>
    public UserId UserId { get; internal init; }

    /// <summary>Bot's display name.</summary>
    public string Nickname { get; internal init; } = "";
}