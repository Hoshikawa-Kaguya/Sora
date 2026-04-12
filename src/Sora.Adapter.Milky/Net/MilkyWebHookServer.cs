using System.Net;

namespace Sora.Adapter.Milky.Net;

/// <summary>HTTP listener for Milky WebHook event push.</summary>
internal sealed class MilkyWebHookServer : IAsyncDisposable
{
#region Fields

    private readonly MilkyConfig              _config;
    private readonly Lazy<ILogger>            _loggerLazy = new(SoraLogger.CreateLogger<MilkyWebHookServer>);
    private          ILogger                  _logger => _loggerLazy.Value;
    private          CancellationTokenSource? _cts;
    private          HttpListener?            _listener;

    /// <summary>Raised when a complete JSON message is received.</summary>
    public event Action<string>? OnMessage;

    /// <summary>Raised when the WebHook server has started.</summary>
    public event Action? OnStarted;

    /// <summary>Raised when the WebHook server has stopped.</summary>
    public event Action<string>? OnStopped;

#endregion

#region Constructor

    /// <summary>Initializes a new instance of the <see cref="MilkyWebHookServer" /> class.</summary>
    /// <param name="config">The Milky adapter configuration.</param>
    public MilkyWebHookServer(MilkyConfig config)
    {
        _config = config;
    }

#endregion

#region Server Lifecycle

    /// <summary>Starts the WebHook HTTP listener.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous start operation.</returns>
    public ValueTask StartAsync(CancellationToken ct = default)
    {
        _cts      = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _listener = new HttpListener();
        string scheme = _config.UseTls ? "https" : "http";
        _listener.Prefixes.Add($"{scheme}://+:{_config.WebHookListenPort}{_config.WebHookPath}/");

        _logger.LogDebug(
            "Milky WebHook server starting on port {Port}, path {Path}",
            _config.WebHookListenPort,
            _config.WebHookPath);

        _listener.Start();
        _logger.LogInformation(
            "Milky WebHook server listening on {Scheme}://+:{Port}{Path}",
            scheme,
            _config.WebHookListenPort,
            _config.WebHookPath);
        OnStarted?.Invoke();
        _ = ListenLoopAsync(_cts.Token);
        return ValueTask.CompletedTask;
    }

    /// <summary>Stops the WebHook HTTP listener.</summary>
    public async ValueTask StopAsync()
    {
        _logger.LogInformation("Milky WebHook server stopping");
        if (_cts != null) await _cts.CancelAsync();
        _listener?.Stop();
        _listener?.Close();
        _listener = null;
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>Handles an incoming HTTP request.</summary>
    /// <param name="context">The HTTP listener context.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        try
        {
            if (context.Request.HttpMethod != "POST")
            {
                _logger.LogWarning("WebHook rejected non-POST request: {Method}", context.Request.HttpMethod);
                context.Response.StatusCode      = 405;
                context.Response.ContentLength64 = 0;
                context.Response.Close();
                return;
            }

            if (!string.IsNullOrEmpty(_config.WebHookToken))
            {
                string? auth = context.Request.Headers["Authorization"];
                if (auth is null
                    || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    || !auth.AsSpan(7).SequenceEqual(_config.WebHookToken))
                {
                    _logger.LogWarning("WebHook auth failed from {RemoteEndpoint}", context.Request.RemoteEndPoint);
                    context.Response.StatusCode      = 401;
                    context.Response.ContentLength64 = 0;
                    context.Response.Close();
                    return;
                }
            }

            using StreamReader reader = new(context.Request.InputStream);
            string             body   = await reader.ReadToEndAsync(ct);
            OnMessage?.Invoke(body);

            context.Response.StatusCode      = 200;
            context.Response.ContentLength64 = 0;
            context.Response.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebHook request");
            try
            {
                context.Response.StatusCode      = 500;
                context.Response.ContentLength64 = 0;
                context.Response.Close();
            }
            catch (Exception closeEx)
            {
                _logger.LogWarning(closeEx, "Failed to send error response");
            }
        }
    }

    /// <summary>Continuously listens for incoming HTTP requests.</summary>
    /// <param name="ct">Cancellation token.</param>
    private async Task ListenLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _listener?.IsListening == true)
            {
                HttpListenerContext context = await _listener.GetContextAsync().WaitAsync(ct);
                _ = HandleRequestAsync(context, ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            OnStopped?.Invoke(ex.Message);
        }
    }

#endregion

#region IAsyncDisposable

    /// <summary>Disposes the server by stopping it.</summary>
    public async ValueTask DisposeAsync() => await StopAsync();

#endregion
}