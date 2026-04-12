using Fleck;

namespace Sora.Adapter.OneBot11.Net;

/// <summary>Reverse WebSocket server — OneBot connects to Sora.</summary>
internal sealed class ReverseWsServer : IAsyncDisposable
{
#region Fields

    private readonly OneBot11Config        _config;
    private readonly Lock                  _lock       = new();
    private readonly Lazy<ILogger>         _loggerLazy = new(SoraLogger.CreateLogger<ReverseWsServer>);
    private          ILogger               _logger => _loggerLazy.Value;
    private          IWebSocketConnection? _connection;
    private          WebSocketServer?      _server;

    /// <summary>Raised when a WebSocket connection is established.</summary>
    public event Action? OnConnected;

    /// <summary>Raised when the WebSocket connection is lost.</summary>
    public event Action<string>? OnDisconnected;

    /// <summary>Raised when a JSON message is received from a client.</summary>
    public event Action<string>? OnMessage;

#endregion

#region Constructor

    /// <summary>Initializes a new instance of the <see cref="ReverseWsServer" /> class.</summary>
    /// <param name="config">The OneBot v11 adapter configuration.</param>
    public ReverseWsServer(OneBot11Config config)
    {
        _config = config;
    }

#endregion

#region Server Lifecycle

    /// <summary>Starts the reverse WebSocket server.</summary>
    /// <param name="ct">Cancellation token.</param>
    public ValueTask StartAsync(CancellationToken ct = default)
    {
        string scheme   = _config.UseTls ? "wss" : "ws";
        string location = $"{scheme}://{_config.Host}:{_config.Port}";
        _logger.LogDebug("OB11 reverse WS server starting at {Location}", location);
        _server = new WebSocketServer(location);

        if (_config.UseTls && !string.IsNullOrEmpty(_config.CertificatePath))
            _server.Certificate = OneBot11Config.LoadCertificate(_config.CertificatePath, _config.CertificatePassword);

        _server.Start(socket =>
        {
            // Validate access token if configured
            if (!string.IsNullOrEmpty(_config.AccessToken))
            {
                string? authHeader = null;
                if (socket.ConnectionInfo.Headers.TryGetValue("Authorization", out string? authVal))
                    authHeader = authVal;

                string? queryToken = null;
                string  path       = socket.ConnectionInfo.Path;
                int     tokenIdx   = path.IndexOf("access_token=", StringComparison.Ordinal);
                if (tokenIdx >= 0)
                {
                    string tokenPart = path[(tokenIdx + "access_token=".Length)..];
                    int    ampIdx    = tokenPart.IndexOf('&');
                    queryToken = ampIdx >= 0 ? tokenPart[..ampIdx] : tokenPart;
                }

                string? token = authHeader is not null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? authHeader["Bearer ".Length..]
                    : queryToken;
                if (token != _config.AccessToken)
                {
                    _logger.LogWarning("OB11 reverse WS: Auth failed from {Client}", socket.ConnectionInfo.ClientIpAddress);
                    socket.Close();
                    return;
                }
            }

            socket.OnOpen = () =>
            {
                lock (_lock)
                {
                    _connection = socket;
                }

                OnConnected?.Invoke();
                _logger.LogInformation("OB11 reverse WS: Client connected from {Client}", socket.ConnectionInfo.ClientIpAddress);
            };

            socket.OnClose = () =>
            {
                lock (_lock)
                {
                    _connection = null;
                }

                OnDisconnected?.Invoke("Connection closed");
                _logger.LogInformation("OB11 reverse WS: Client disconnected");
            };

            socket.OnMessage = message => { OnMessage?.Invoke(message); };
        });

        return ValueTask.CompletedTask;
    }

    /// <summary>Stops the reverse WebSocket server.</summary>
    public ValueTask StopAsync()
    {
        _logger.LogInformation("OB11 reverse WS server stopping");
        lock (_lock)
        {
            _connection?.Close();
            _connection = null;
        }

        _server?.Dispose();
        _server = null;
        return ValueTask.CompletedTask;
    }

    /// <summary>Sends a JSON message through the active WebSocket connection.</summary>
    /// <param name="message">The JSON message to send.</param>
    /// <returns>A task that completes when the send finishes.</returns>
    public ValueTask SendAsync(string message)
    {
        IWebSocketConnection? conn;
        lock (_lock)
        {
            conn = _connection;
        }

        conn?.Send(message);
        return ValueTask.CompletedTask;
    }

    /// <summary>Closes the currently connected client (if any) due to heartbeat timeout.</summary>
    internal void CloseClientAsync()
    {
        _logger.LogWarning("OB11 reverse WS: Force-closing client due to heartbeat timeout");
        lock (_lock)
        {
            _connection?.Close();
        }
    }

#endregion

#region IAsyncDisposable

    /// <summary>Disposes the server by stopping it.</summary>
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

#endregion
}