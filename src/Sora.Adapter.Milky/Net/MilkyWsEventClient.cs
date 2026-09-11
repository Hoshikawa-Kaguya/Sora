using System.Buffers;
using System.Net.WebSockets;
using System.Text;

namespace Sora.Adapter.Milky.Net;

/// <summary>WebSocket client for Milky event streaming.</summary>
internal sealed class MilkyWsEventClient : IAsyncDisposable
{
#region Fields

    private readonly MilkyConfig              _config;
    private readonly ILogger                  _logger = SoraLogger.CreateLogger<MilkyWsEventClient>();
    private          CancellationTokenSource? _cts;
    private          Task?                    _connectionTask;

    /// <summary>Raised when the WebSocket connection is established.</summary>
    public event Action? OnConnected;

    /// <summary>Raised when the WebSocket connection is lost.</summary>
    public event Action<string>? OnDisconnected;

    /// <summary>Raised when a complete JSON message is received.</summary>
    public event Action<string>? OnMessage;

    /// <summary>Raised when the client begins a reconnection attempt.</summary>
    public event Action? OnReconnecting;

#endregion

#region Constructor

    /// <summary>Initializes a new instance of the <see cref="MilkyWsEventClient" /> class.</summary>
    /// <param name="config">The Milky adapter configuration.</param>
    public MilkyWsEventClient(MilkyConfig config)
    {
        _config = config;
    }

#endregion

#region Connection Lifecycle

    /// <summary>Starts the owned connection and receive loop without waiting for the connection lifetime.</summary>
    /// <param name="ct">Cancellation token for the connection lifetime.</param>
    /// <returns>A completed task after the connection loop has been scheduled.</returns>
    public ValueTask ConnectAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        CancellationToken lifetimeToken = _cts.Token;
        _connectionTask = Task.Run(() => ConnectLoopAsync(lifetimeToken));
        return ValueTask.CompletedTask;
    }

    /// <summary>Cancels the connection lifetime and waits for its resources to be released.</summary>
    public async ValueTask DisconnectAsync()
    {
        _logger.LogInformation("Milky WS client disconnecting");
        if (_cts is null) return;
        try
        {
            await _cts.CancelAsync();
            if (_connectionTask is not null) await _connectionTask;
        }
        finally
        {
            _cts.Dispose();
            _cts            = null;
            _connectionTask = null;
        }
    }

    /// <summary>Creates a <see cref="ClientWebSocket" /> configured with TLS settings from the config.</summary>
    private ClientWebSocket CreateWebSocket()
    {
        ClientWebSocket ws = new();
        if (_config is { UseTls: true, SkipCertificateValidation: true })
            ws.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        if (!string.IsNullOrEmpty(_config.ClientCertificatePath))
            ws.Options.ClientCertificates.Add(
                MilkyConfig.LoadCertificate(_config.ClientCertificatePath, _config.ClientCertificatePassword));
        return ws;
    }

    /// <summary>Continuously receives messages from the WebSocket.</summary>
    /// <param name="ws">The socket owned by the current connection attempt.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        byte[]                  buffer = ArrayPool<byte>.Shared.Rent(8192);
        ArrayBufferWriter<byte> writer = new();

        try
        {
            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                ValueWebSocketReceiveResult result =
                    await ws.ReceiveAsync(buffer.AsMemory(), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Milky WS: Server closed connection");
                    OnDisconnected?.Invoke("Server closed connection");
                    break;
                }

                writer.Write(buffer.AsSpan(0, result.Count));

                if (result.EndOfMessage)
                {
                    OnMessage?.Invoke(Encoding.UTF8.GetString(writer.WrittenSpan));
                    writer.Clear();
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Lifetime cancellation ends receiving without scheduling a reconnect.
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "Milky WS connection error");
            OnDisconnected?.Invoke(ex.Message);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>Owns each socket and retries disconnected or failed connections at the configured interval.</summary>
    /// <param name="ct">Cancellation token for the connection lifetime.</param>
    private async Task ConnectLoopAsync(CancellationToken ct)
    {
        bool reconnect      = false;
        bool firstReconnect = true;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (reconnect)
                {
                    if (_config.ReconnectInterval == TimeSpan.Zero) return;
                    if (firstReconnect)
                    {
                        OnReconnecting?.Invoke();
                        firstReconnect = false;
                    }

                    _logger.LogDebug("Milky WS reconnecting in {Interval}s", _config.ReconnectInterval.TotalSeconds);
                    await Task.Delay(_config.ReconnectInterval, ct);
                }

                Uri url = new(_config.GetEventUrl(true));
                _logger.LogDebug("Milky WS connecting to {Url}", url);
                using ClientWebSocket ws = CreateWebSocket();
                if (!string.IsNullOrEmpty(_config.AccessToken))
                    ws.Options.SetRequestHeader("Authorization", $"Bearer {_config.AccessToken}");

                await ws.ConnectAsync(url, ct);
                _logger.LogInformation("Milky WS connected to {Url}", url);
                firstReconnect = true;
                OnConnected?.Invoke();
                await ReceiveLoopAsync(ws, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Milky WS connection lost");
                string failMsg = reconnect
                    ? $"Reconnect failed: {ex.Message}"
                    : $"Connect failed: {ex.Message}";
                OnDisconnected?.Invoke(failMsg);
            }

            reconnect = true;
        }
    }

#endregion

#region IAsyncDisposable

    /// <summary>Disposes the client by disconnecting.</summary>
    public async ValueTask DisposeAsync() => await DisconnectAsync();

#endregion
}