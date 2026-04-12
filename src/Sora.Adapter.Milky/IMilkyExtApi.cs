namespace Sora.Adapter.Milky;

/// <summary>
///     Milky-specific extension API (peer pin management). Access via <see cref="IBotApi.GetExtension{T}" /> where T is
///     <see cref="IMilkyExtApi" />.
/// </summary>
public interface IMilkyExtApi : IAdapterExtension
{
#region PeerPinsApi

    /// <summary>Gets all pinned (topped) conversations.</summary>
    ValueTask<ApiResult<PeerPinsResult>> GetPeerPinsAsync(CancellationToken ct = default);

    /// <summary>Pins or unpins a conversation.</summary>
    /// <param name="messageScene">The message source type.</param>
    /// <param name="peerId">The peer ID (user ID for friends, group ID for groups).</param>
    /// <param name="isPinned">True to pin, false to unpin.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask<ApiResult> SetPeerPinAsync(
        MessageSourceType messageScene,
        long              peerId,
        bool              isPinned,
        CancellationToken ct = default);

#endregion
}