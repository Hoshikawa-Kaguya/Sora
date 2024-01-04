namespace Sora.Core;

public class SoraConfig
{
    public string Host { get; init; } = "127.0.0.1";
    public ushort Port { get; init; } = 8199;
    public string AccessToken { get; init; } = string.Empty;
    public string Path { get; init; } = "/";
    public TimeSpan HeartBeatTimeOut { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan ApiTimeOut { get; init; } = TimeSpan.FromSeconds(5);
    public string[] SuperUsers { get; init; } = Array.Empty<string>();
    public string[] BlockUsers { get; init; } = Array.Empty<string>();
    public bool EnableSocketMessage { get; init; } = false;
}