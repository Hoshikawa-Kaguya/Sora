using Newtonsoft.Json;

namespace Sora.Adapter.OneBot11.Models;

/// <summary>OneBot v11 file info (from group_upload notice).</summary>
internal sealed class OneBotFile
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("busid")]
    public long BusId { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }
}