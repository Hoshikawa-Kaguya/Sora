using System.Security.Cryptography.X509Certificates;

namespace Sora.Adapter.Milky;

/// <summary>
///     Event transport mode for the Milky adapter.
/// </summary>
public enum EventTransport
{
    /// <summary>Server-Sent Events (SSE) — HTTP GET streaming.</summary>
    Sse,

    /// <summary>WebSocket connection to /event endpoint.</summary>
    WebSocket,

    /// <summary>WebHook — protocol server POSTs events to a local HTTP endpoint.</summary>
    WebHook
}

/// <summary>
///     Configuration for the Milky adapter.
/// </summary>
public sealed class MilkyConfig : IBotServiceConfig
{
    /// <summary>Access token for API authentication (Bearer token).</summary>
    public string AccessToken { get; init; } = "";

    /// <summary>API call timeout.</summary>
    public TimeSpan ApiTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public bool AutoMarkMessageRead { get; init; } = true;

    /// <inheritdoc />
    public bool DropSelfMessage { get; init; } = true;

    /// <inheritdoc />
    public UserId[] BlockUsers { get; init; } = [];

    /// <summary>Password for the client certificate file.</summary>
    public string? ClientCertificatePassword { get; init; }

    /// <summary>Path to a client certificate file (PFX/PKCS12 format). Null = no client certificate.</summary>
    public string? ClientCertificatePath { get; init; }

    /// <inheritdoc />
    public bool EnableCommandManager { get; init; } = true;

    /// <summary>Event transport mode.</summary>
    public EventTransport EventTransport { get; init; } = EventTransport.WebSocket;

    /// <summary>Protocol server host address.</summary>
    public string Host { get; init; } = "127.0.0.1";

    /// <inheritdoc />
    public ILoggerFactory? LoggerFactory { get; init; }

    /// <inheritdoc />
    public LogLevel MinimumLogLevel { get; init; } = LogLevel.Information;

    /// <summary>Protocol server port.</summary>
    public int Port { get; init; } = 3000;

    /// <summary>URL path prefix (e.g. "milky" → /milky/api/:action).</summary>
    public string Prefix { get; init; } = "";

    /// <summary>Auto-reconnect interval for SSE/WebSocket (zero = no reconnect).</summary>
    public TimeSpan ReconnectInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Whether to skip server certificate validation (for self-signed certificates).</summary>
    public bool SkipCertificateValidation { get; init; }

    /// <inheritdoc />
    public UserId[] SuperUsers { get; init; } = [];

    /// <summary>Whether to use TLS (HTTPS/WSS) for connections.</summary>
    public bool UseTls { get; init; }

    /// <summary>Local port to listen on for WebHook events (only used when EventTransport = WebHook).</summary>
    public int WebHookListenPort { get; init; } = 23000;

    /// <summary>WebHook path to listen on (e.g. "/webhook").</summary>
    public string WebHookPath { get; init; } = "/webhook";

    /// <summary>WebHook access token for verifying incoming events.</summary>
    public string WebHookToken { get; init; } = "";

    /// <summary>Creates an <see cref="HttpClientHandler" /> configured with TLS settings.</summary>
    internal HttpClientHandler CreateHttpHandler()
    {
        HttpClientHandler handler = new();
        if (SkipCertificateValidation)
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        if (!string.IsNullOrEmpty(ClientCertificatePath))
            handler.ClientCertificates.Add(LoadCertificate(ClientCertificatePath, ClientCertificatePassword));
        return handler;
    }

    /// <summary>
    ///     Gets the base URL for API calls (e.g. "http://127.0.0.1:3000/milky").
    /// </summary>
    internal string GetApiBaseUrl()
    {
        string prefix = string.IsNullOrEmpty(Prefix) ? "" : $"/{Prefix.TrimStart('/')}";
        string scheme = UseTls ? "https" : "http";
        return $"{scheme}://{Host}:{Port}{prefix}/api";
    }

    /// <summary>
    ///     Gets the event endpoint URL.
    /// </summary>
    internal string GetEventUrl(bool useWs = false)
    {
        string prefix = string.IsNullOrEmpty(Prefix) ? "" : $"/{Prefix.TrimStart('/')}";
        string scheme = (useWs, UseTls) switch
                            {
                                (true, true)   => "wss",
                                (true, false)  => "ws",
                                (false, true)  => "https",
                                (false, false) => "http"
                            };
        return $"{scheme}://{Host}:{Port}{prefix}/event";
    }

    /// <summary>Loads a PFX/PKCS12 certificate from the given file path.</summary>
    internal static X509Certificate2 LoadCertificate(string path, string? password = null) =>
        string.IsNullOrEmpty(password)
            ? X509CertificateLoader.LoadPkcs12FromFile(path, null)
            : X509CertificateLoader.LoadPkcs12FromFile(path, password);
}