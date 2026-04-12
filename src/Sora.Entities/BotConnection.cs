namespace Sora.Entities;

/// <summary>
///     Represents an active connection to a bot account.
/// </summary>
public sealed class BotConnection : IBotConnection
{
    /// <inheritdoc />
    public IBotApi Api { get; init; } = null!;

    /// <inheritdoc />
    public Guid ConnectionId { get; init; }

    /// <inheritdoc />
    public ConnectionState State { get; internal set; } = ConnectionState.Idle;
}