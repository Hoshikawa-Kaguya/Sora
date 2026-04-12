using System.Net.WebSockets;
using System.Reactive.Linq;
using Websocket.Client;

namespace Sora.Adapter.OneBot11.Net;

/// <summary>Forward WebSocket client — connects to OneBot server.</summary>
internal sealed class ForwardWsClient : IAsyncDisposable
{
#region Fields

    private readonly OneBot11Config   _config;
    private readonly Lazy<ILogger>    _loggerLazy = new(SoraLogger.CreateLogger<ForwardWsClient>);
    private          ILogger          _logger => _loggerLazy.Value;
    private          WebsocketClient? _client;
    private          IDisposable?     _disconnectSubscription;
    private          IDisposable?     _messageSubscription;
    private          IDisposable?     _reconnectSubscription;

    /// <summary>Raised when the WebSocket connection is established or re-established.</summary>
    public event Action? OnConnected;

    /// <summary>Raised when the WebSocket connection is lost.</summary>
    public event Action<string>? OnDisconnected;

    /// <summary>Raised when a JSON message is received from the server.</summary>
    public event Action<string>? OnMessage;

    /// <summary>Raised when the client begins a reconnection attempt.</summary>
    public event Action? OnReconnecting;

#endregion

#region Constructor

    /// <summary>Initializes a new instance of the <see cref="ForwardWsClient" /> class.</summary>
    /// <param name="config">The OneBot v11 adapter configuration.</param>
    public ForwardWsClient(OneBot11Config config)
    {
        _config = config;
    }

#endregion

#region Connection Lifecycle

    /// <summary>Connects to the OneBot v11 forward WebSocket server.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async ValueTask ConnectAsync(CancellationToken ct = default)
    {
        // OB11 forward WS passes access_token as query parameter
        string scheme = _config.UseTls ? "wss" : "ws";
        string urlStr = $"{scheme}://{_config.Host}:{_config.Port}";
        if (!string.IsNullOrEmpty(_config.AccessToken))
            urlStr += $"?access_token={_config.AccessToken}";

        Uri url = new(urlStr);
        _logger.LogDebug("OB11 forward WS connecting to {Url}", url);
        _client = new WebsocketClient(
            url,
            () =>
            {
                ClientWebSocket ws = new();
                if (_config is { UseTls: true, SkipCertificateValidation: true })
                    ws.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                if (!string.IsNullOrEmpty(_config.CertificatePath))
                    ws.Options.ClientCertificates.Add(
                        OneBot11Config.LoadCertificate(
                            _config.CertificatePath,
                            _config.CertificatePassword));
                if (!string.IsNullOrEmpty(_config.AccessToken))
                    ws.Options.SetRequestHeader("Authorization", $"Bearer {_config.AccessToken}");
                return ws;
            })
            {
                // Use heartbeat interval × 3 as dead-connection timeout; fall back to 5 min if heartbeat is disabled
                ReconnectTimeout = _config.HeartbeatInterval > TimeSpan.Zero
                    ? _config.HeartbeatInterval * 3
                    : TimeSpan.FromMinutes(5),
                IsReconnectionEnabled = _config.ReconnectInterval > TimeSpan.Zero
            };

        _messageSubscription = _client.MessageReceived
                                      .Where(msg => msg.Text is not null)
                                      .Subscribe(msg => OnMessage?.Invoke(msg.Text!));

        _reconnectSubscription = _client.ReconnectionHappened
                                        .Subscribe(info =>
                                        {
                                            _logger.LogInformation("OB11 forward WS connected (type: {Type})", info.Type);
                                            OnConnected?.Invoke();
                                        });

        _disconnectSubscription = _client.DisconnectionHappened
                                         .Subscribe(info =>
                                         {
                                             _logger.LogWarning("OB11 forward WS disconnected: {Type}", info.Type);
                                             OnDisconnected?.Invoke(info.Type.ToString());
                                             if (_client.IsReconnectionEnabled)
                                                 OnReconnecting?.Invoke();
                                         });

        await _client.Start();
    }

    /// <summary>Disconnects from the WebSocket server.</summary>
    public async ValueTask DisconnectAsync()
    {
        _logger.LogInformation("OB11 forward WS client disconnecting");
        _messageSubscription?.Dispose();
        _reconnectSubscription?.Dispose();
        _disconnectSubscription?.Dispose();
        if (_client is not null)
        {
            await _client.Stop(WebSocketCloseStatus.NormalClosure, "Stopping");
            _client.Dispose();
            _client = null;
        }
    }

    /// <summary>Sends a JSON message through the WebSocket.</summary>
    /// <param name="message">The JSON message to send.</param>
    /// <returns>A task that completes when the send finishes.</returns>
    public ValueTask SendAsync(string message)
    {
        if (_client is not null && _client.IsRunning)
            _client.Send(message);
        else
            _logger.LogWarning("OB11 forward WS: message dropped — client not connected");
        return ValueTask.CompletedTask;
    }

    /// <summary>Forces a reconnection of the WebSocket client (e.g., on heartbeat timeout).</summary>
    internal void ForceReconnect()
    {
        _logger.LogWarning("OB11 forward WS: Force reconnect triggered (heartbeat timeout)");
        _client?.Reconnect();
    }

#endregion

#region IAsyncDisposable

    /// <summary>Disposes the client by disconnecting.</summary>
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }

#endregion
}