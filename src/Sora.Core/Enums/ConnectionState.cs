namespace Sora.Core.Enums;

/// <summary>
///     Represents the state of a connection to a protocol server.
/// </summary>
public enum ConnectionState
{
    /// <summary>Connection has not been started yet.</summary>
    Idle,

    /// <summary>Connection is being established.</summary>
    Connecting,

    /// <summary>Connection is active and ready.</summary>
    Connected,

    /// <summary>Connection has been closed.</summary>
    Disconnected,

    /// <summary>Connection is attempting to reconnect after an unexpected disconnection.</summary>
    Reconnecting
}