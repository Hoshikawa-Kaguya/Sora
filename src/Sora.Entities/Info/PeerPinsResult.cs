namespace Sora.Entities.Info;

/// <summary>Result from the get_peer_pins API containing pinned friends and groups.</summary>
public sealed record PeerPinsResult
{
    /// <summary>Pinned friends.</summary>
    public IReadOnlyList<FriendInfo> Friends { get; internal init; } = [];

    /// <summary>Pinned groups.</summary>
    public IReadOnlyList<GroupInfo> Groups { get; internal init; } = [];
}