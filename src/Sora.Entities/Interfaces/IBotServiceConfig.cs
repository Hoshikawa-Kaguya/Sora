namespace Sora.Entities.Interfaces;

/// <summary>
///     Base configuration for a bot service.
/// </summary>
public interface IBotServiceConfig
{
    /// <summary>List of super user IDs with elevated permissions.</summary>
    UserId[] SuperUsers { get; }

    /// <summary>List of blocked user IDs.</summary>
    UserId[] BlockUsers { get; }

    /// <summary>Whether to enable the command manager.</summary>
    bool EnableCommandManager { get; }

    /// <summary>Whether to automatically mark messages as read after receiving them.</summary>
    bool AutoMarkMessageRead => true;

    /// <summary>Drop messages that self sent.</summary>
    bool DropSelfMessage => true;

    /// <summary>
    ///     Custom logger factory for the service. When provided, the framework uses this factory
    ///     for all logging output. When <c>null</c>, the framework creates a default Serilog-based
    ///     console logger on first service creation.
    /// </summary>
    /// <remarks>
    ///     To completely silence logging, set this to
    ///     <see cref="Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance" />.
    /// </remarks>
    ILoggerFactory? LoggerFactory => null;

    /// <summary>
    ///     Minimum log level for the default logger. Only effective when <see cref="LoggerFactory" /> is <c>null</c>
    ///     (i.e., the framework creates the default Serilog console logger).
    ///     When providing a custom <see cref="LoggerFactory" />, configure levels through your own factory instead.
    /// </summary>
    LogLevel MinimumLogLevel => LogLevel.Information;
}