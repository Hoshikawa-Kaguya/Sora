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

/// <summary>Response from the get_status action.</summary>
internal sealed class GetStatusResponse
{
    [JsonProperty("good")]
    public bool Good { get; set; }

    [JsonProperty("online")]
    public bool Online { get; set; }
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

/// <summary>Parameters for the set_restart action.</summary>
internal sealed class SetRestartParams
{
    [JsonProperty("delay")]
    public int Delay { get; set; }
}

/// <summary>Parameters for the get_group_honor_info action.</summary>
internal sealed class GetGroupHonorInfoParams
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; } = "all";
}

/// <summary>Parameters for the get_record action.</summary>
internal sealed class GetRecordParams
{
    [JsonProperty("file")]
    public string File { get; set; } = "";

    [JsonProperty("out_format")]
    public string OutFormat { get; set; } = "mp3";
}

/// <summary>Parameters for the get_image action.</summary>
internal sealed class GetImageParams
{
    [JsonProperty("file")]
    public string File { get; set; } = "";
}

/// <summary>Response containing a file path.</summary>
internal sealed class FileResponse
{
    [JsonProperty("file")]
    public string? File { get; set; }
}

/// <summary>Response containing a boolean value.</summary>
internal sealed class BoolResponse
{
    [JsonProperty("yes")]
    public bool Yes { get; set; }
}