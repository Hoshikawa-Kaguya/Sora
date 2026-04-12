namespace Sora.Entities.Interfaces;

/// <summary>
///     Adapter contract — each protocol implements this.
///     Merges network layer + protocol implementation.
/// </summary>
public interface IBotAdapter : IAsyncDisposable
{
    /// <summary>Gets the protocol name (e.g., "OneBot11", "Milky").</summary>
    string ProtocolName { get; }

    /// <summary>Gets the current adapter state.</summary>
    AdapterState State { get; }

    /// <summary> Bot account user id. </summary>
    UserId SelfId { get; }

    /// <summary>Gets the API instance.</summary>
    /// <returns>The API instance, or null if the account is not connected.</returns>
    IBotApi? GetApi();

    /// <summary>Get active connection managed by this adapter.</summary>
    IBotConnection? GetConnection();

    /// <summary>Starts the adapter and begins connecting.</summary>
    /// <param name="ct">Cancellation token to bound the start operation.</param>
    ValueTask StartAsync(CancellationToken ct = default);

    /// <summary>Stops the adapter and disconnects.</summary>
    /// <param name="ct">Cancellation token to bound the stop operation.</param>
    ValueTask StopAsync(CancellationToken ct = default);
}