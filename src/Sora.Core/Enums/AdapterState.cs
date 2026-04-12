namespace Sora.Core.Enums;

/// <summary>
///     Represents the lifecycle state of a bot adapter.
/// </summary>
public enum AdapterState
{
    /// <summary>Adapter is stopped.</summary>
    Stopped,

    /// <summary>Adapter is starting up.</summary>
    Starting,

    /// <summary>Adapter is running and connected.</summary>
    Running,

    /// <summary>Adapter is shutting down.</summary>
    Stopping,

    /// <summary>Adapter encountered a fatal error.</summary>
    Faulted
}