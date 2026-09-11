using Newtonsoft.Json;

namespace Sora.Adapter.OneBot11.Models.Api;

/// <summary>Response from the get_login_info action.</summary>
internal sealed class GetLoginInfoResponse
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("nickname")]
    public string? Nickname { get; set; }
}

/// <summary>Response from the get_version_info action.</summary>
internal sealed class GetVersionInfoResponse
{
    [JsonProperty("app_name")]
    public string? AppName { get; set; }

    [JsonProperty("app_version")]
    public string? AppVersion { get; set; }

    [JsonProperty("protocol_version")]
    public string? ProtocolVersion { get; set; }
}

/// <summary>Parameters for the get_cookies action.</summary>
internal sealed class GetCookiesParams
{
    [JsonProperty("domain")]
    public string Domain { get; set; } = "";
}

/// <summary>Response from the get_cookies action.</summary>
internal sealed class GetCookiesResponse
{
    [JsonProperty("cookies")]
    public string? Cookies { get; set; }
}

/// <summary>Response from the get_csrf_token action.</summary>
internal sealed class GetCsrfTokenResponse
{
    [JsonProperty("token")]
    public int Token { get; set; }
}