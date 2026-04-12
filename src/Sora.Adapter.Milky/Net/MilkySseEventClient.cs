using System.Net.Http.Headers;
using System.Text;

namespace Sora.Adapter.Milky.Net;

/// <summary>SSE client for Milky event streaming.</summary>
internal sealed class MilkySseEventClient : IAsyncDisposable
{
#region Fields

    private readonly MilkyConfig              _config;
    private readonly Lazy<ILogger>            _loggerLazy = new(SoraLogger.CreateLogger<MilkySseEventClient>);
    private          ILogger                  _logger => _loggerLazy.Value;
    private          CancellationTokenSource? _cts;
    private          HttpClient?              _httpClient;

    /// <summary>Raised when the SSE connection is established.</summary>
    public event Action? OnConnected;

    /// <summary>Raised when the SSE connection is lost.</summary>
    public event Action<string>? OnDisconnected;

    /// <summary>Raised when a complete JSON message is received.</summary>
    public event Action<string>? OnMessage;

    /// <summary>Raised when the client begins a reconnection attempt.</summary>
    public event Action? OnReconnecting;

#endregion

#region Constructor

    /// <summary>Initializes a new instance of the <see cref="MilkySseEventClient" /> class.</summary>
    /// <param name="config">The Milky adapter configuration.</param>
    public MilkySseEventClient(MilkyConfig config)
    {
        _config = config;
    }

#endregion

#region Connection Lifecycle

    /// <summary>Connects to the Milky event SSE endpoint.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous connect operation.</returns>
    public ValueTask ConnectAsync(CancellationToken ct = default)
    {
        _cts        = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _httpClient = new HttpClient(_config.CreateHttpHandler(), true) { Timeout = Timeout.InfiniteTimeSpan };

        if (!string.IsNullOrEmpty(_config.AccessToken))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.AccessToken);

        string url = _config.GetEventUrl();
        _logger.LogDebug("Milky SSE connecting to {Url}", url);
        _ = ReadSseStreamAsync(url, _cts.Token);

        return ValueTask.CompletedTask;
    }

    /// <summary>Disconnects from the SSE stream.</summary>
    public async ValueTask DisconnectAsync()
    {
        _logger.LogInformation("Milky SSE client disconnecting");
        if (_cts != null) await _cts.CancelAsync();
        _httpClient?.Dispose();
        _httpClient = null;
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>Continuously reads events from the SSE stream.</summary>
    /// <param name="url">The SSE endpoint URL.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task ReadSseStreamAsync(string url, CancellationToken ct)
    {
        try
        {
            using HttpResponseMessage response =
                await _httpClient!.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            OnConnected?.Invoke();
            _logger.LogInformation("Milky SSE connected to {Url}", url);

            await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
            using StreamReader reader = new(stream);

            await ParseSseStreamAsync(reader, msg => OnMessage?.Invoke(msg), ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Milky SSE connection lost");
            OnDisconnected?.Invoke(ex.Message);
        }

        // After stream ends (disconnected), attempt reconnect
        if (!ct.IsCancellationRequested)
            await ReconnectSseAsync(url, ct);
    }

    /// <summary>Attempts to reconnect to the SSE stream after disconnection.</summary>
    /// <param name="url">The SSE endpoint URL.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task ReconnectSseAsync(string url, CancellationToken ct)
    {
        OnReconnecting?.Invoke();

        while (!ct.IsCancellationRequested)
            try
            {
                _logger.LogDebug("Milky SSE reconnecting in {Interval}...", _config.ReconnectInterval);
                await Task.Delay(_config.ReconnectInterval, ct);
                await ReadSseStreamAsync(url, ct);
                return; // If stream reading returns normally, we're done
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

    /// <summary>
    ///     Parses an SSE stream line-by-line per the W3C Server-Sent Events specification.
    ///     Only events with type <c>milky_event</c> (or no explicit type) are dispatched.
    /// </summary>
    /// <param name="reader">The text reader providing SSE lines.</param>
    /// <param name="onMessage">Callback invoked with the accumulated data for each dispatched event.</param>
    /// <param name="ct">Cancellation token.</param>
    internal static async Task ParseSseStreamAsync(TextReader reader, Action<string> onMessage, CancellationToken ct)
    {
        StringBuilder dataBuffer     = new();
        bool          shouldDispatch = true;

        while (!ct.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(ct);
            if (line is null) break;

            // Empty line → dispatch accumulated event (per SSE spec)
            if (line.Length == 0)
            {
                if (dataBuffer.Length > 0)
                {
                    // Remove trailing LF added by multi-line accumulation
                    dataBuffer.Length--;

                    // Only process milky_event type (or no event type for compatibility)
                    if (dataBuffer.Length > 0 && shouldDispatch)
                        onMessage(dataBuffer.ToString());
                }

                dataBuffer.Clear();
                shouldDispatch = true;
                continue;
            }

            // Comment line (SSE spec: lines starting with ':' are ignored)
            if (line[0] == ':')
                continue;

            // Parse "field:value" — find first colon
            ReadOnlySpan<char> lineSpan   = line.AsSpan();
            int                colonIndex = lineSpan.IndexOf(':');
            if (colonIndex < 0) continue;

            ReadOnlySpan<char> fieldName  = lineSpan[..colonIndex];
            ReadOnlySpan<char> fieldValue = lineSpan[(colonIndex + 1)..];

            // Strip single leading space after colon (per SSE spec)
            if (fieldValue is [' ', ..])
                fieldValue = fieldValue[1..];

            switch (fieldName)
            {
                case "data":
                    dataBuffer.Append(fieldValue);
                    dataBuffer.Append('\n');
                    break;
                case "event":
                    shouldDispatch = fieldValue is "milky_event" or "";
                    break;
            }
            // id, retry — not used by Milky protocol, skip silently
        }
    }

#endregion

#region IAsyncDisposable

    /// <summary>Disposes the client by disconnecting.</summary>
    public async ValueTask DisposeAsync() => await DisconnectAsync();

#endregion
}