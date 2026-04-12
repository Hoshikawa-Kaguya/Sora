using System.Security.Cryptography.X509Certificates;

namespace Sora.Adapter.OneBot11;

/// <summary>
///     Connection mode for OneBot v11 adapter.
/// </summary>
public enum ConnectionMode
{
    /// <summary>Forward WebSocket — Sora connects to the OneBot server.</summary>
    ForwardWebSocket,

    /// <summary>Reverse WebSocket — OneBot server connects to Sora.</summary>
    ReverseWebSocket
}

/// <summary>
///     Configuration for the OneBot v11 adapter.
/// </summary>
public sealed class OneBot11Config : IBotServiceConfig
{
    /// <summary>Access token for authentication (empty = no auth).</summary>
    public string AccessToken { get; init; } = "";

    /// <summary>API call timeout.</summary>
    public TimeSpan ApiTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public bool AutoMarkMessageRead { get; init; } = true;

    /// <inheritdoc />
    public bool DropSelfMessage { get; init; } = true;

    /// <inheritdoc />
    public UserId[] BlockUsers { get; init; } = [];

    /// <summary>Password for the certificate file.</summary>
    public string? CertificatePassword { get; init; }

    /// <summary>
    ///     Path to a certificate file (PFX/PKCS12 format). Used as client cert for forward WS, server cert for reverse
    ///     WS.
    /// </summary>
    public string? CertificatePath { get; init; }

    /// <inheritdoc />
    public bool EnableCommandManager { get; init; } = true;

    /// <summary>Heartbeat check interval.</summary>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Host address. For forward WS: remote host. For reverse WS: listen address.</summary>
    public string Host { get; init; } = "127.0.0.1";

    /// <inheritdoc />
    public ILoggerFactory? LoggerFactory { get; init; }

    /// <inheritdoc />
    public LogLevel MinimumLogLevel { get; init; } = LogLevel.Information;

    /// <summary>Connection mode (forward or reverse WebSocket).</summary>
    public ConnectionMode Mode { get; init; } = ConnectionMode.ForwardWebSocket;

    /// <summary>Port number.</summary>
    public int Port { get; init; } = 6700;

    /// <summary>Auto-reconnect interval for forward WS (zero = no reconnect).</summary>
    public TimeSpan ReconnectInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Whether to skip server certificate validation (for self-signed certificates).</summary>
    public bool SkipCertificateValidation { get; init; }

    /// <inheritdoc />
    public UserId[] SuperUsers { get; init; } = [];

    /// <summary>Whether to use TLS (WSS) for connections.</summary>
    public bool UseTls { get; init; }

    /// <summary>Loads a PFX/PKCS12 certificate from the given file path.</summary>
    internal static X509Certificate2 LoadCertificate(
        string  path,
        string? password = null) =>
        string.IsNullOrEmpty(password)
            ? X509CertificateLoader.LoadPkcs12FromFile(path, null)
            : X509CertificateLoader.LoadPkcs12FromFile(path, password);
}