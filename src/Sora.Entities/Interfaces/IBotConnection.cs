namespace Sora.Entities.Interfaces;

/// <summary>
///     Represents a single connection to a bot account.
/// </summary>
public interface IBotConnection
{
    /// <summary>The API instance for this connection.</summary>
    IBotApi Api { get; }

    /// <summary>Framework-assigned connection identifier.</summary>
    Guid ConnectionId { get; }

    /// <summary>Current connection state.</summary>
    ConnectionState State { get; }
}