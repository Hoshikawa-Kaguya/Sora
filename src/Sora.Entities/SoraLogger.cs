using Destructurama;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Sora.Entities;

/// <summary>Provides the shared logger factory for all Sora components.</summary>
/// <remarks>
///     Configure logging before calling Sora methods that obtain a logger. The first logger request
///     initializes an Information-level Serilog console factory if none was configured.
///     Successful initialization permanently fixes the factory, including across service disposal.
///     Configuration and initialization are thread-safe.
/// </remarks>
public static class SoraLogger
{
    private static readonly Lock            InitializationLock = new();
    private static          ILoggerFactory? _factory;
    private static          bool            _initializing;

#region Configuration

    /// <summary>Configures the shared factory before any Sora logger is obtained.</summary>
    /// <param name="factory">
    ///     The caller-owned factory. Its lifetime must cover all Sora logging; Sora never disposes it
    ///     or changes its filtering rules. Pass <see cref="NullLoggerFactory.Instance" /> to silence logging.
    /// </param>
    /// <exception cref="ArgumentNullException">The factory is null.</exception>
    /// <exception cref="InvalidOperationException">Logging is already initialized or initialization is in progress.</exception>
    public static void Configure(ILoggerFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        lock (InitializationLock)
        {
            EnsureConfigurationAvailable();
            _factory = factory;
        }
    }

    /// <summary>Builds the shared Serilog factory from the supplied configuration.</summary>
    /// <param name="configuration">The completed configuration, including sinks and filtering rules.</param>
    /// <remarks>
    ///     The resulting factory is shared for the process lifetime and is not disposed by individual services.
    ///     To control flushing and disposal, create and own an ILoggerFactory and pass it to the factory overload.
    ///     A failed initialization leaves logging unconfigured and may be retried with a corrected configuration.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The configuration is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Logging is already initialized, initialization is re-entered, or building the factory fails.
    ///     Construction failures retain the original exception as InnerException.
    /// </exception>
    public static void Configure(LoggerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        lock (InitializationLock)
        {
            EnsureConfigurationAvailable();
            _factory = BuildFactory(configuration);
        }
    }

    /// <summary>Returns a fresh Serilog configuration with colored console output and JsonNet destructuring.</summary>
    /// <param name="logLevel">The minimum level, or <see cref="LogLevel.None" /> to disable output.</param>
    /// <returns>A configuration that can be customized before passing it to <see cref="Configure(LoggerConfiguration)" />.</returns>
    /// <remarks>This method does not initialize or lock the shared factory.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The level is not a defined logging level.</exception>
    public static LoggerConfiguration CreateDefaultLoggerConfiguration(LogLevel logLevel = LogLevel.Information) =>
        new LoggerConfiguration()
            .MinimumLevel.Is(ToSerilogLevel(logLevel))
            .Destructure.JsonNetTypes()
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

#endregion

#region Loggers

    /// <summary>Gets a logger whose category is the specified type.</summary>
    /// <typeparam name="T">The type used as the logger category.</typeparam>
    /// <returns>A logger from the shared factory.</returns>
    /// <exception cref="InvalidOperationException">Default initialization fails or initialization is re-entered.</exception>
    public static ILogger CreateLogger<T>() => GetFactory().CreateLogger<T>();

    /// <summary>Gets a logger for the specified category, initializing default logging if necessary.</summary>
    /// <param name="categoryName">The logger category.</param>
    /// <returns>A logger from the shared factory.</returns>
    /// <exception cref="ArgumentNullException">The category is null.</exception>
    /// <exception cref="InvalidOperationException">Default initialization fails or initialization is re-entered.</exception>
    public static ILogger CreateLogger(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(categoryName);
        return GetFactory().CreateLogger(categoryName);
    }

#endregion

    private static ILoggerFactory GetFactory()
    {
        lock (InitializationLock)
        {
            if (_factory is not null) return _factory;
            EnsureConfigurationAvailable();
            return _factory = BuildFactory();
        }
    }

    private static void EnsureConfigurationAvailable()
    {
        if (_factory is not null)
            throw new InvalidOperationException("Logging must be configured once, before any Sora logger is obtained.");
        if (_initializing)
            throw new InvalidOperationException(
                "Logging cannot be configured or used while its factory is being initialized.");
    }

    // Called under InitializationLock. Publish only after construction succeeds, including on re-entry failures.
    private static ILoggerFactory BuildFactory(LoggerConfiguration? configuration = null)
    {
        _initializing = true;
        Logger? logger = null;
        try
        {
            logger = (configuration ?? CreateDefaultLoggerConfiguration()).CreateLogger();
            return new SerilogLoggerFactory(logger, true);
        }
        catch (Exception exception)
        {
            logger?.Dispose();
            throw new InvalidOperationException("Failed to initialize Sora logging.", exception);
        }
        finally
        {
            _initializing = false;
        }
    }

    private static LogEventLevel ToSerilogLevel(LogLevel level) =>
        level switch
        {
            LogLevel.Trace       => LogEventLevel.Verbose,
            LogLevel.Debug       => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning     => LogEventLevel.Warning,
            LogLevel.Error       => LogEventLevel.Error,
            LogLevel.Critical    => LogEventLevel.Fatal,
            LogLevel.None        => (LogEventLevel)6,
            _                    => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
}