using System.ComponentModel;
using Newtonsoft.Json.Linq;
using Sora.Adapter.OneBot11.Converter;
using Sora.Adapter.OneBot11.Models;
using Sora.Adapter.OneBot11.Net;

namespace Sora.Adapter.OneBot11;

/// <summary>
///     OneBot v11 protocol adapter implementation.
///     Supports both forward and reverse WebSocket connections.
/// </summary>
public sealed class OneBot11Adapter : IBotAdapter, IAdapterEventSource
{
#region Fields

    private readonly OneBot11Config      _config;
    private readonly Lazy<ILogger>       _loggerLazy = new(SoraLogger.CreateLogger<OneBot11Adapter>);
    private          ILogger             _logger => _loggerLazy.Value;
    private          ReactiveApiManager? _apiManager;
    private          OneBot11BotApi?     _botApi;
    private          BotConnection?      _connection;
    private          ForwardWsClient?    _forwardClient;
    private          Timer?              _heartbeatTimer;
    private          long                _lastHeartbeatTicks;
    private          ReverseWsServer?    _reverseServer;

    /// <inheritdoc />
    public string ProtocolName => "OneBot11";

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

    /// <summary>Creates a new OneBot11 adapter with the given config.</summary>
    public OneBot11Adapter(OneBot11Config config)
    {
        _config = config;
        OneBot11MapsterConfig.Configure();
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
            "OneBot11 adapter starting (mode: {Mode}, host: {Host}:{Port})",
            _config.Mode,
            _config.Host,
            _config.Port);
        _apiManager = new ReactiveApiManager(_config.ApiTimeout);
        OneBot11BotApi botApi = new(_apiManager, SendRawAsync);
        _botApi = botApi;
        _connection = new BotConnection
            {
                ConnectionId = Guid.NewGuid(),
                Api          = botApi,
                State        = ConnectionState.Connecting
            };

        switch (_config.Mode)
        {
            case ConnectionMode.ForwardWebSocket:
                _forwardClient                =  new ForwardWsClient(_config);
                _forwardClient.OnMessage      += HandleMessage;
                _forwardClient.OnConnected    += HandleConnected;
                _forwardClient.OnDisconnected += HandleDisconnected;
                _forwardClient.OnReconnecting += HandleReconnecting;
                await _forwardClient.ConnectAsync(ct);
                break;
            case ConnectionMode.ReverseWebSocket:
                _reverseServer                =  new ReverseWsServer(_config);
                _reverseServer.OnMessage      += HandleMessage;
                _reverseServer.OnConnected    += HandleConnected;
                _reverseServer.OnDisconnected += HandleDisconnected;
                await _reverseServer.StartAsync(ct);
                break;
            default:
                throw new InvalidEnumArgumentException($"OneBot11 mode out of range: {_config.Mode}");
        }

        State = AdapterState.Running;
        _logger.LogInformation("OneBot11 adapter started");

        // Start heartbeat monitoring for both WS modes
        if (_config.HeartbeatInterval.Ticks > 0)
        {
            Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);
            TimeSpan checkInterval = _config.HeartbeatInterval;
            TimeSpan timeout       = _config.HeartbeatInterval * 3;
            _heartbeatTimer = new Timer(
                _ =>
                {
                    long     lastTicks = Interlocked.Read(ref _lastHeartbeatTicks);
                    TimeSpan elapsed   = DateTime.UtcNow - new DateTime(lastTicks, DateTimeKind.Utc);
                    if (elapsed <= timeout) return;

                    _logger.LogWarning(
                        "Heartbeat timeout ({Elapsed:F1}s > {Timeout:F1}s), triggering reconnection",
                        elapsed.TotalSeconds,
                        timeout.TotalSeconds);

                    switch (_config.Mode)
                    {
                        case ConnectionMode.ForwardWebSocket:
                            _forwardClient?.ForceReconnect();
                            break;
                        case ConnectionMode.ReverseWebSocket:
                            _reverseServer?.CloseClientAsync();
                            break;
                    }
                },
                null,
                checkInterval,
                checkInterval);
        }
    }

    /// <inheritdoc />
    public async ValueTask StopAsync(CancellationToken ct = default)
    {
        State = AdapterState.Stopping;
        _logger.LogInformation("OneBot11 adapter stopping");

        if (_heartbeatTimer is not null)
            await _heartbeatTimer.DisposeAsync();

        if (_forwardClient is not null)
            await _forwardClient.DisconnectAsync();
        if (_reverseServer is not null)
            await _reverseServer.StopAsync();

        _apiManager?.Dispose();
        State = AdapterState.Stopped;
        _logger.LogInformation("OneBot11 adapter stopped");
    }

#endregion

#region Connection Management

    /// <summary>Handles a successful WebSocket connection.</summary>
    private void HandleConnected()
    {
        _connection?.State = ConnectionState.Connected;
        Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);
        _logger.LogInformation("OneBot11 adapter connected (mode: {Mode})", _config.Mode);
        HandleConnectedAsync().RunCatch(ex => _logger.LogError(ex, "Error during OneBot11 connection initialization"));
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

        ConnectedEvent evt = new()
            {
                ConnectionId = _connection?.ConnectionId ?? Guid.Empty,
                SelfId       = SelfId,
                Time         = DateTime.Now,
                Api          = _connection?.Api!
            };
        _ = _onEvent?.Invoke(evt);
    }

    /// <summary>Handles WebSocket disconnection.</summary>
    /// <param name="reason">The disconnection reason.</param>
    private void HandleDisconnected(string reason)
    {
        _connection?.State = ConnectionState.Disconnected;
        _logger.LogInformation("OneBot11 adapter disconnected: {Reason}", reason);

        DisconnectedEvent evt = new()
            {
                ConnectionId = _connection?.ConnectionId ?? Guid.Empty,
                SelfId       = SelfId,
                Time         = DateTime.Now,
                Api          = _connection?.Api!,
                Reason       = reason
            };
        _ = _onEvent?.Invoke(evt);

        // clean up id record
        SelfId = default;
    }

    /// <summary>Handles the beginning of a reconnection attempt.</summary>
    private void HandleReconnecting()
    {
        _connection?.State = ConnectionState.Reconnecting;
        _logger.LogInformation("OneBot11 adapter reconnecting (mode: {Mode})", _config.Mode);
    }

#endregion

#region Private Helpers

    /// <summary>Processes a raw JSON message (event or API response) from the WebSocket.</summary>
    /// <param name="json">The raw JSON message string.</param>
    private void HandleMessage(string json)
    {
        try
        {
            _logger.LogTrace("OneBot11 message received: {Json}", json);

            JObject jObj = JObject.Parse(json);

            // Check if this is an API response (has non-null echo field)
            if (jObj.TryGetValue("echo", out JToken? echoToken) && echoToken.Type != JTokenType.Null)
            {
                OneBotApiResponse? response = jObj.ToObject<OneBotApiResponse>();
                if (response is not null)
                    _apiManager?.HandleResponse(response);
                return;
            }

            // Otherwise it's an event
            OneBotEvent? eventModel = jObj.ToObject<OneBotEvent>();
            switch (eventModel)
            {
                case null:
                    _logger.LogWarning("OneBot11 payload could not be parsed into an event model");
                    return;
                // Track heartbeat events — update liveness timestamp, skip further dispatch
                case { PostType: "meta_event", MetaEventType: "heartbeat" }:
                    Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);
                    _logger.LogTrace("Heartbeat received from self_id={SelfId}", eventModel.SelfId);
                    return;
                // Store normal friend request flags before event conversion (OB11 has no query API for these)
                case { PostType: "request", RequestType: "friend" }
                    when !string.IsNullOrEmpty(eventModel.Flag)
                         && _botApi is not null:
                    _botApi.StoreFriendRequestFlag(eventModel.UserId, eventModel.Flag);
                    break;
                // Store group request flags (join requests and invitation to bot)
                case { PostType: "request", RequestType: "group" }
                    when !string.IsNullOrEmpty(eventModel.Flag) && _botApi is not null:
                    switch (eventModel.SubType)
                    {
                        case "add":
                            _botApi.StoreGroupRequestFlag(eventModel.GroupId, eventModel.UserId, eventModel.Flag);
                            break;
                        case "invite":
                            _botApi.StoreGroupInvitationFlag(eventModel.GroupId, eventModel.UserId, eventModel.Flag);
                            break;
                    }

                    break;
            }

            BotEvent? soraEvent =
                EventConverter.ToSoraEvent(eventModel, _connection?.ConnectionId ?? Guid.Empty, _connection?.Api!);
            //Drop message from self sent
            if (_config.DropSelfMessage && soraEvent is MessageReceivedEvent msg && msg.Sender.UserId == msg.SelfId) return;

            if (soraEvent is not null)
            {
                LogConvertedEvent(soraEvent);
                _ = _onEvent?.Invoke(soraEvent);
            }
            else
            {
                _logger.LogDebug(
                    "OneBot11 event was ignored after conversion (postType: {PostType}, subType: {SubType})",
                    eventModel.PostType,
                    eventModel.SubType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling OneBot11 message");
        }
    }

    /// <summary>Sends a raw JSON string through the active WebSocket connection.</summary>
    /// <param name="json">The JSON string to send.</param>
    /// <returns>A task that completes when the send finishes.</returns>
    private ValueTask SendRawAsync(string json)
    {
        if (_forwardClient is null && _reverseServer is null)
        {
            _logger.LogError("Cannot send OneBot11 payload: no active WebSocket connection");
            throw new InvalidOperationException("No active WebSocket connection.");
        }

        _logger.LogTrace("OneBot11 raw send: {Json}", json);
        return _reverseServer?.SendAsync(json) ?? _forwardClient!.SendAsync(json);
    }

    private void LogConvertedEvent(BotEvent evt)
    {
        if (evt is MessageReceivedEvent messageEvent)
        {
            _logger.LogInformation(
                "OneBot11 event {EventType}: source={SourceType}, sender={SenderId}, group={GroupId}, message={MessageId}",
                evt.GetType().Name,
                messageEvent.Message.SourceType,
                messageEvent.Message.SenderId,
                messageEvent.Message.GroupId,
                messageEvent.Message.MessageId);
            return;
        }

        _logger.LogInformation(
            "OneBot11 event {EventType} converted for dispatch (connection: {ConnectionId}, self: {SelfId})",
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
        if (_forwardClient is not null)
            await _forwardClient.DisposeAsync();
        if (_reverseServer is not null)
            await _reverseServer.DisposeAsync();
    }

#endregion
}