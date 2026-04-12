using Newtonsoft.Json;
using Sora.Adapter.Milky.Converter;
using Sora.Adapter.Milky.Models;
using Sora.Adapter.Milky.Net;
using ConnectionState = Sora.Core.Enums.ConnectionState;

namespace Sora.Adapter.Milky;

/// <summary>
///     Milky protocol adapter implementation.
///     Supports SSE, WebSocket, and WebHook event transport.
/// </summary>
public sealed class MilkyAdapter : IBotAdapter, IAdapterEventSource
{
#region Fields

    private readonly MilkyConfig          _config;
    private readonly Lazy<ILogger>        _loggerLazy = new(SoraLogger.CreateLogger<MilkyAdapter>);
    private          ILogger              _logger => _loggerLazy.Value;
    private          MilkyHttpApiClient?  _apiClient;
    private          BotConnection?       _connection;
    private          MilkySseEventClient? _sseClient;
    private          MilkyWebHookServer?  _webHookServer;
    private          MilkyWsEventClient?  _wsClient;

    /// <inheritdoc />
    public string ProtocolName => "Milky";

    /// <inheritdoc />
    public AdapterState State { get; private set; } = AdapterState.Stopped;

    /// <inheritdoc />
    public UserId SelfId { get; private set; }

    private event Func<BotEvent, ValueTask>? _onEvent;

    /// <inheritdoc />
    event Func<BotEvent, ValueTask> IAdapterEventSource.OnEvent
    {
        add => _onEvent += value;
        remove => _onEvent -= value;
    }

#endregion

#region Constructor

    /// <summary>Creates a new Milky adapter with the given config.</summary>
    public MilkyAdapter(MilkyConfig config)
    {
        _config = config;
        MilkyMapsterConfig.Configure();
    }

#endregion

#region IBotAdapter Implementation

    /// <inheritdoc />
    public IBotApi? GetApi() => _connection?.Api;

    /// <inheritdoc />
    public IBotConnection? GetConnection() => _connection;

    /// <inheritdoc />
    public async ValueTask StartAsync(CancellationToken ct = default)
    {
        State = AdapterState.Starting;
        _logger.LogInformation(
            "Milky adapter starting (transport: {Transport}, host: {Host}:{Port})",
            _config.EventTransport,
            _config.Host,
            _config.Port);
        _apiClient = new MilkyHttpApiClient(_config);
        MilkyBotApi botApi = new(_apiClient);
        _connection = new BotConnection
            {
                ConnectionId = Guid.NewGuid(),
                Api          = botApi,
                State        = ConnectionState.Connecting
            };

        switch (_config.EventTransport)
        {
            case EventTransport.WebSocket:
                _wsClient                =  new MilkyWsEventClient(_config);
                _wsClient.OnMessage      += HandleEventMessage;
                _wsClient.OnConnected    += HandleConnected;
                _wsClient.OnDisconnected += HandleDisconnected;
                _wsClient.OnReconnecting += HandleReconnecting;
                await _wsClient.ConnectAsync(ct);
                break;

            case EventTransport.Sse:
                _sseClient                =  new MilkySseEventClient(_config);
                _sseClient.OnMessage      += HandleEventMessage;
                _sseClient.OnConnected    += HandleConnected;
                _sseClient.OnDisconnected += HandleDisconnected;
                _sseClient.OnReconnecting += HandleReconnecting;
                await _sseClient.ConnectAsync(ct);
                break;

            case EventTransport.WebHook:
                _webHookServer           =  new MilkyWebHookServer(_config);
                _webHookServer.OnMessage += HandleEventMessage;
                _webHookServer.OnStarted += HandleConnected;
                _webHookServer.OnStopped += HandleDisconnected;
                await _webHookServer.StartAsync(ct);
                break;
            default:
                State = AdapterState.Faulted;
                throw new ArgumentOutOfRangeException($"Unsupported event transport: {_config.EventTransport}");
        }

        State = AdapterState.Running;
        _logger.LogInformation("Milky adapter started");
    }

    /// <inheritdoc />
    public async ValueTask StopAsync(CancellationToken ct = default)
    {
        State = AdapterState.Stopping;
        _logger.LogInformation("Milky adapter stopping");

        if (_wsClient is not null) await _wsClient.DisconnectAsync();
        if (_sseClient is not null) await _sseClient.DisconnectAsync();
        if (_webHookServer is not null) await _webHookServer.StopAsync();
        _apiClient?.Dispose();

        State = AdapterState.Stopped;
        _logger.LogInformation("Milky adapter stopped");
    }

#endregion

#region Connection Management

    /// <summary>Handles a successful connection to the event transport.</summary>
    private void HandleConnected()
    {
        _connection?.State = ConnectionState.Connected;
        _logger.LogInformation("Milky adapter connected (transport: {Transport})", _config.EventTransport);
        HandleConnectedAsync().RunCatch(ex => _logger.LogError(ex, "Error during Milky connection initialization"));
    }

    /// <summary>Performs async initialization after a successful connection.</summary>
    private async Task HandleConnectedAsync()
    {
        _logger.LogInformation("Auto get account info on new connection");
        ApiResult<BotIdentity> selfInfo = await (_connection?.Api.GetSelfInfoAsync()
                                                 ?? throw new InvalidOperationException("Api client not initialized"));
        if (selfInfo is not { IsSuccess: true, Data: { } selfData })
            throw new InvalidOperationException($"Failed to get self info on connection: {selfInfo.Message}");
        if (SelfId == default)
            SelfId = selfData.UserId;
        else if (SelfId != selfData.UserId)
            throw new InvalidOperationException("Cannot connect multiple accounts with the same adapter instance");
        _logger.LogInformation("Get self account id: {AccountId}", selfData.UserId);

        _ = _onEvent?.Invoke(
                        new ConnectedEvent
                            {
                                ConnectionId = _connection?.ConnectionId ?? Guid.Empty,
                                SelfId       = SelfId,
                                Time         = DateTime.Now,
                                Api          = _connection?.Api!
                            })
                    .AsTask();
    }

    /// <summary>Handles disconnection from the event transport.</summary>
    /// <param name="reason">The disconnection reason.</param>
    private void HandleDisconnected(string reason)
    {
        _connection?.State = ConnectionState.Disconnected;
        _logger.LogInformation("Milky adapter disconnected: {Reason}", reason);

        _ = _onEvent?.Invoke(
                        new DisconnectedEvent
                            {
                                ConnectionId = _connection?.ConnectionId ?? Guid.Empty,
                                SelfId       = SelfId,
                                Time         = DateTime.Now,
                                Api          = _connection?.Api!,
                                Reason       = reason
                            })
                    .AsTask();

        // clean up id record
        SelfId = default;
    }

    /// <summary>Handles the beginning of a reconnection attempt.</summary>
    private void HandleReconnecting()
    {
        _connection?.State = ConnectionState.Reconnecting;
        _logger.LogInformation("Milky adapter reconnecting [{Transport}]{Host}", _config.EventTransport, _config.Host);
    }

#endregion

#region Message Handling

    /// <summary>Processes a raw JSON event message from the event transport.</summary>
    /// <param name="json">The raw JSON event string.</param>
    private void HandleEventMessage(string json)
    {
        try
        {
            _logger.LogTrace("Milky event received: {Json}", json);

            MilkyEvent? evt = JsonConvert.DeserializeObject<MilkyEvent>(json);
            if (evt is null)
            {
                _logger.LogWarning("Milky event payload could not be deserialized");
                return;
            }

            BotEvent? soraEvent =
                EventConverter.ToSoraEvent(evt, _connection?.ConnectionId ?? Guid.Empty, _connection?.Api!);
            //Drop message from self sent
            if (_config.DropSelfMessage && soraEvent is MessageReceivedEvent msg && msg.Sender.UserId == msg.SelfId) return;

            if (soraEvent is not null)
            {
                LogConvertedEvent(soraEvent);
                _ = _onEvent?.Invoke(soraEvent);
            }
            else
            {
                _logger.LogDebug("Milky event was ignored after conversion");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Milky event");
        }
    }

#endregion

#region Private Helpers

    private void LogConvertedEvent(BotEvent evt)
    {
        if (evt is MessageReceivedEvent messageEvent)
        {
            _logger.LogInformation(
                "[Message] From [{MessageSourceType}]{SourceId} {SenderId} seq={MessageId}",
                messageEvent.Message.SourceType,
                messageEvent.Message.SourceType == MessageSourceType.Group
                    ? messageEvent.Message.GroupId
                    : messageEvent.Message.SenderId,
                messageEvent.Message.SourceType == MessageSourceType.Group ? messageEvent.Message.SenderId : string.Empty,
                messageEvent.Message.MessageId);
            return;
        }

        _logger.LogInformation(
            "Milky event {EventType} converted for dispatch (connection: {ConnectionId}, self: {SelfId})",
            evt.GetType().Name,
            evt.ConnectionId,
            evt.SelfId);
    }

#endregion

#region IAsyncDisposable

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        if (_wsClient is not null) await _wsClient.DisposeAsync();
        if (_sseClient is not null) await _sseClient.DisposeAsync();
        if (_webHookServer is not null) await _webHookServer.DisposeAsync();
    }

#endregion
}