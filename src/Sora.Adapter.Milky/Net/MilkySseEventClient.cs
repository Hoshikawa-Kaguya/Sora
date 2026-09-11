using System.Net.Http.Headers;
using System.Text;

namespace Sora.Adapter.Milky.Net;

/// <summary>SSE client for Milky event streaming.</summary>
internal sealed class MilkySseEventClient : IAsyncDisposable
{
#region Fields

    private readonly MilkyConfig              _config;
    private readonly ILogger                  _logger = SoraLogger.CreateLogger<MilkySseEventClient>();
    private          CancellationTokenSource? _cts;
    private          Task?                    _connectionTask;

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

    /// <summary>Starts the owned connection and receive loop without waiting for the connection lifetime.</summary>
    /// <param name="ct">Cancellation token for the connection lifetime.</param>
    /// <returns>A completed task after the connection loop has been scheduled.</returns>
    public ValueTask ConnectAsync(CancellationToken ct = default)
    {
        HttpClient httpClient = new(_config.CreateHttpHandler(), true) { Timeout = Timeout.InfiniteTimeSpan };
        if (!string.IsNullOrEmpty(_config.AccessToken))
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.AccessToken);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        CancellationToken lifetimeToken = _cts.Token;
        _connectionTask = Task.Run(
            async () =>
            {
                using (httpClient)
                {
                    await ConnectLoopAsync(httpClient, lifetimeToken);
                }
            },
            lifetimeToken);
        return ValueTask.CompletedTask;
    }

    /// <summary>Cancels the connection lifetime and waits for its resources to be released.</summary>
    public async ValueTask DisconnectAsync()
    {
        _logger.LogInformation("Milky SSE client disconnecting");
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

    /// <summary>Owns the HTTP stream and retries disconnected or failed connections at the configured interval.</summary>
    /// <param name="httpClient">The HTTP client owned by the connection task.</param>
    /// <param name="ct">Cancellation token for the connection lifetime.</param>
    private async Task ConnectLoopAsync(HttpClient httpClient, CancellationToken ct)
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

                    _logger.LogDebug("Milky SSE reconnecting in {Interval}s", _config.ReconnectInterval.TotalSeconds);
                    await Task.Delay(_config.ReconnectInterval, ct);
                }

                string url = _config.GetEventUrl();
                _logger.LogDebug("Milky SSE connecting to {Url}", url);
                using HttpResponseMessage response =
                    await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();
                firstReconnect = true;
                OnConnected?.Invoke();
                _logger.LogInformation("Milky SSE connected to {Url}", url);

                await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
                using StreamReader reader = new(stream);
                await SseStreamLoopAsync(reader, msg => OnMessage?.Invoke(msg), ct);
                if (!ct.IsCancellationRequested) OnDisconnected?.Invoke("Server closed connection");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Milky SSE connection lost");
                string failMsg = reconnect
                    ? $"Reconnect failed: {ex.Message}"
                    : $"Connect failed: {ex.Message}";
                OnDisconnected?.Invoke(failMsg);
            }

            reconnect = true;
        }
    }

    /// <summary>
    ///     Parses an SSE stream line-by-line per the W3C Server-Sent Events specification.
    ///     Only events with type <c>milky_event</c> (or no explicit type) are dispatched.
    /// </summary>
    /// <param name="reader">The text reader providing SSE lines.</param>
    /// <param name="onMessage">Callback invoked with the accumulated data for each dispatched event.</param>
    /// <param name="ct">Cancellation token.</param>
    internal static async Task SseStreamLoopAsync(TextReader reader, Action<string> onMessage, CancellationToken ct)
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