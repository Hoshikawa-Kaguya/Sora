using System.Buffers;
using System.Net.WebSockets;
using System.Text;

namespace Sora.Adapter.Milky.Net;

/// <summary>WebSocket client for Milky event streaming.</summary>
internal sealed class MilkyWsEventClient : IAsyncDisposable
{
#region Fields

    private readonly MilkyConfig              _config;
    private readonly Lazy<ILogger>            _loggerLazy = new(SoraLogger.CreateLogger<MilkyWsEventClient>);
    private          ILogger                  _logger => _loggerLazy.Value;
    private          CancellationTokenSource? _cts;
    private          ClientWebSocket?         _ws;

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

    /// <summary>Connects to the Milky event WebSocket endpoint.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous connect operation.</returns>
    public async ValueTask ConnectAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ws  = CreateWebSocket();

        if (!string.IsNullOrEmpty(_config.AccessToken))
            _ws.Options.SetRequestHeader("Authorization", $"Bearer {_config.AccessToken}");

        Uri url = new(_config.GetEventUrl(true));
        _logger.LogDebug("Milky WS connecting to {Url}", url);
        await _ws.ConnectAsync(url, _cts.Token);
        _logger.LogInformation("Milky WS connected to {Url}", url);
        OnConnected?.Invoke();

        _ = ReceiveLoopAsync(_cts.Token);
    }

    /// <summary>Disconnects from the WebSocket.</summary>
    public async ValueTask DisconnectAsync()
    {
        _logger.LogInformation("Milky WS client disconnecting");
        if (_cts != null) await _cts.CancelAsync();
        if (_ws?.State == WebSocketState.Open)
            try
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopping", CancellationToken.None);
            }
            catch
            {
                /* ignore close errors */
            }

        _ws?.Dispose();
        _ws = null;
        _cts?.Dispose();
        _cts = null;
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
    /// <param name="ct">Cancellation token.</param>
    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        byte[]                  buffer = ArrayPool<byte>.Shared.Rent(8192);
        ArrayBufferWriter<byte> writer = new();

        try
        {
            while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
            {
                ValueWebSocketReceiveResult result =
                    await _ws.ReceiveAsync(buffer.AsMemory(), ct);

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
        catch (OperationCanceledException)
        {
            return;
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

        // After loop exits (disconnected), attempt reconnect
        if (!ct.IsCancellationRequested)
            await ReconnectLoopAsync(ct);
    }

    /// <summary>Attempts to reconnect to the WebSocket after disconnection.</summary>
    /// <param name="ct">Cancellation token.</param>
    private async Task ReconnectLoopAsync(CancellationToken ct)
    {
        OnReconnecting?.Invoke();

        while (!ct.IsCancellationRequested)
            try
            {
                _logger.LogDebug("Milky WS reconnecting in {Interval}...", _config.ReconnectInterval);
                await Task.Delay(_config.ReconnectInterval, ct);
                _ws?.Dispose();
                _ws = CreateWebSocket();
                if (!string.IsNullOrEmpty(_config.AccessToken))
                    _ws.Options.SetRequestHeader("Authorization", $"Bearer {_config.AccessToken}");

                Uri url = new(_config.GetEventUrl(true));
                await _ws.ConnectAsync(url, ct);
                _logger.LogInformation("Milky WS reconnected to {Url}", url);
                OnConnected?.Invoke();
                await ReceiveLoopAsync(ct); // Resume receiving
                return;                     // No error caused, ws connected
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                OnDisconnected?.Invoke($"Reconnect failed: {ex.Message}");
            }
    }

#endregion

#region IAsyncDisposable

    /// <summary>Disposes the client by disconnecting.</summary>
    public async ValueTask DisposeAsync() => await DisconnectAsync();

#endregion
}