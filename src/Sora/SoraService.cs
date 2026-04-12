using System.Reflection;
using Destructurama;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Sora.Entities.MessageWaiting;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Sora;

/// <summary>
///     Default implementation of <see cref="IBotService" />.
/// </summary>
public sealed class SoraService : IBotService
{
#region Fields

    private readonly IBotServiceConfig        _config;
    private readonly Lazy<CommandManager>     _commandLazy = new(() => new CommandManager());
    private readonly ILogger                  _logger;
    private readonly MessageWaiter            _waiter = new();
    private          CancellationTokenSource? _serviceCts;

#endregion

#region Properties

    /// <inheritdoc />
    public IBotAdapter Adapter { get; }

    /// <summary>Command manager for attribute-based command routing.</summary>
    public CommandManager Commands => _commandLazy.Value;

    /// <inheritdoc />
    public EventDispatcher Events { get; } = new();

    /// <inheritdoc />
    public Guid ServiceId { get; } = Guid.NewGuid();

#endregion

#region Constructor

    /// <summary>Creates a new SoraService wrapping the given adapter.</summary>
    /// <param name="adapter">The bot adapter to wrap.</param>
    /// <param name="config">Service configuration options.</param>
    public SoraService(IBotAdapter adapter, IBotServiceConfig config)
    {
        Adapter = adapter;
        _config = config;

        if (adapter is not IAdapterEventSource eventSource)
            throw new ArgumentException($"Adapter {adapter.GetType().Name} must implement IAdapterEventSource.", nameof(adapter));

        // Default logger's loglevel can be overridden by SORA_LOG_LEVEL_OVERRIDE env var
        LogLevel currentLogLevel = SysUtils.GetEnvLogLevelOverride() ?? config.MinimumLogLevel;
        // Initialize logging and seal — SetFactory() will throw after this point
        SoraLogger.InternalInitFactory(config.LoggerFactory, () => CreateDefaultLoggerFactory(currentLogLevel));
        _logger = SoraLogger.CreateLogger<SoraService>();

        eventSource.OnEvent += async e =>
        {
            CancellationToken ct = _serviceCts?.Token ?? CancellationToken.None;

            // Inject waiter reference so extension methods work transparently
            e.Waiter = _waiter;

            // Auto-mark messages as read (fire-and-forget)
            AutoMarkMessageRead(e, ct);

            // Match special event
            switch (e)
            {
                case DisconnectedEvent disconnectedEvent:
                    _logger.LogDebug(
                        "Clearing message waiters for disconnected connection {ConnectionId}",
                        disconnectedEvent.ConnectionId);
                    _waiter.DisposeConnection(disconnectedEvent.ConnectionId);
                    break;
                // Check message waiters (continuous commands)
                case MessageReceivedEvent msgForWaiter when _waiter.TryMatch(msgForWaiter):
                    _logger.LogDebug(
                        "Message [{MessageId}] was consumed by a waiter on connection {ConnectionId}",
                        msgForWaiter.Message.MessageId,
                        msgForWaiter.ConnectionId);
                    e.IsContinueEventChain = false;
                    return;
            }

            // Run command manager on message events
            if (_config.EnableCommandManager && e is MessageReceivedEvent msgEvent && e.IsContinueEventChain)
            {
                _logger.LogDebug("Routing message [{MessageId}] to the command manager", msgEvent.Message.MessageId);
                await Commands.HandleMessageEventAsync(msgEvent, ct);
            }

            if (!e.IsContinueEventChain) return;

            // Dispatch to EventDispatcher
            await Events.DispatchAsync(e, ct);
        };
    }

#endregion

#region Service Lifecycle

    /// <inheritdoc />
    public async ValueTask StartAsync(CancellationToken ct = default)
    {
        _serviceCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Startup banner
        _logger.LogInformation("Ciallo～★");
        _logger.LogInformation(
            "Sora {SoraVersion} | Adapter: {AdapterName} {AdapterVersion}",
            GetAssemblyVersion(typeof(SoraService)),
            Adapter.GetType().Assembly.GetName().Name,
            GetAssemblyVersion(Adapter.GetType()));
        _logger.LogDebug(
            "Components — Entities: {EntitiesVersion}, Command: {CommandVersion}, Core: {CoreVersion}",
            GetAssemblyVersion(typeof(SoraLogger)),
            GetAssemblyVersion(typeof(CommandManager)),
            GetAssemblyVersion(typeof(MessageSourceType)));
        _logger.LogDebug(
            "Runtime: .NET {RuntimeVersion} | OS: {OsVersion} | LogLevel: {LogLevel}",
            Environment.Version,
            Environment.OSVersion,
            _config.MinimumLogLevel);

        _logger.LogInformation("SoraService {ServiceId} starting (adapter: {Protocol})", ServiceId, Adapter.GetType().FullName);
        await Adapter.StartAsync(ct);
        _logger.LogInformation("SoraService {ServiceId} started", ServiceId);
    }

    /// <inheritdoc />
    public async ValueTask StopAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("SoraService {ServiceId} stopping", ServiceId);
        _waiter.DisposeAll();
        if (_serviceCts is not null)
            await _serviceCts.CancelAsync();
        _serviceCts?.Dispose();
        _serviceCts = null;
        await Adapter.StopAsync(ct);
        _logger.LogInformation("SoraService {ServiceId} stopped", ServiceId);
    }

#endregion

#region IBotService Implementation

    /// <inheritdoc />
    public IBotApi? GetApi() => Adapter.GetApi();

    /// <inheritdoc />
    public IBotConnection? GetConnection() => Adapter.GetConnection();

    /// <inheritdoc />
    public T? GetExtension<T>() where T : class, IAdapterExtension => GetApi()?.GetExtension<T>();

#endregion

#region Event Pipeline

    private void AutoMarkMessageRead(BotEvent e, CancellationToken ct)
    {
        if (!_config.AutoMarkMessageRead
            || e is not MessageReceivedEvent markMsg
            || !Enum.IsDefined(markMsg.Message.SourceType)) return;

        long peerId = markMsg.Message.SourceType switch
                          {
                              MessageSourceType.Group  => markMsg.Message.GroupId,
                              MessageSourceType.Friend => markMsg.Message.SenderId,
                              MessageSourceType.Temp   => markMsg.Message.SenderId,
                              _                        => -1
                          };
        if (peerId == -1)
        {
            _logger.LogError(
                "Cannot auto mark message [{MessageId}] as read: unsupported source type {SourceType}",
                markMsg.Message.MessageId,
                markMsg.Message.SourceType);
            return;
        }

        _logger.LogDebug(
            "Auto-marking message [{MessageId}] as read for [{SourceType}] {PeerId}",
            markMsg.Message.MessageId,
            markMsg.Message.SourceType,
            peerId);
        markMsg.Api.MarkMessageAsReadAsync(
                   markMsg.Message.SourceType,
                   peerId,
                   markMsg.Message.MessageId,
                   ct)
               .RunCatch(ex => _logger.LogError(
                   ex,
                   "Failed to auto-mark message [{MessageId}] as read",
                   markMsg.Message.MessageId));
    }

#endregion

#region Logging

    /// <summary>
    ///     Gets the default Serilog <see cref="LoggerConfiguration" /> with colored console output.
    /// </summary>
    /// <param name="logLevel">The minimum log level to apply.</param>
    /// <returns>A configured <see cref="LoggerConfiguration" /> instance.</returns>
    public static LoggerConfiguration CreateDefaultLoggerConfiguration(LogLevel logLevel) =>
        new LoggerConfiguration()
            .MinimumLevel.Is(ToSerilogLevel(logLevel))
            .Destructure.JsonNetTypes()
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

    /// <summary>
    ///     Creates the default Serilog-based logger factory with colored console output.
    /// </summary>
    /// <param name="minimumLevel">The minimum log level to use.</param>
    /// <returns>A configured <see cref="ILoggerFactory" />.</returns>
    private static ILoggerFactory CreateDefaultLoggerFactory(LogLevel minimumLevel)
    {
        Logger serilogLogger = CreateDefaultLoggerConfiguration(minimumLevel).CreateLogger();
        return new SerilogLoggerFactory(serilogLogger, true);
    }

    /// <summary>Converts a MEL <see cref="LogLevel" /> to a Serilog <see cref="LogEventLevel" />.</summary>
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

    /// <summary>Gets the informational version string of the assembly containing the specified type.</summary>
    private static string GetAssemblyVersion(Type type) =>
        type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? type.Assembly.GetName().Version?.ToString()
        ?? "unknown";

#endregion

#region IDisposable / IAsyncDisposable

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        await Adapter.DisposeAsync();
    }

#endregion
}