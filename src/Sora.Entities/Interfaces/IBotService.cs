namespace Sora.Entities.Interfaces;

/// <summary>
///     Top-level bot service that users interact with.
/// </summary>
public interface IBotService : IAsyncDisposable
{
    /// <summary>The underlying protocol adapter.</summary>
    IBotAdapter Adapter { get; }

    /// <summary>Event dispatcher for subscribing to bot events.</summary>
    EventDispatcher Events { get; }

    /// <summary>Unique service identifier.</summary>
    Guid ServiceId { get; }

    /// <summary>Gets the API instance.</summary>
    /// <returns>The API instance, or null if the account is not connected.</returns>
    IBotApi? GetApi();

    /// <summary>Get active connection.</summary>
    IBotConnection? GetConnection();

    /// <summary>
    ///     Gets a typed adapter extension API from the default connection.
    ///     Shortcut for <see cref="IBotApi.GetExtension{T}" /> on the default API.
    /// </summary>
    /// <typeparam name="T">Extension interface type (e.g. IMilkyExtApi, IOneBot11ExtApi).</typeparam>
    /// <returns>The extension instance, or null if not supported or no connection is active.</returns>
    T? GetExtension<T>() where T : class, IAdapterExtension;

    /// <summary>Starts the bot service.</summary>
    /// <param name="ct">Cancellation token to bound the start operation.</param>
    ValueTask StartAsync(CancellationToken ct = default);

    /// <summary>Stops the bot service.</summary>
    /// <param name="ct">Cancellation token to bound the stop operation.</param>
    ValueTask StopAsync(CancellationToken ct = default);
}