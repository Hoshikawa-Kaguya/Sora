using Newtonsoft.Json;

namespace Sora.Entities.Info;

/// <summary>
/// 上传文件的信息
/// </summary>
public readonly struct UploadFileInfo
{
    /// <summary>
    /// 文件 ID
    /// </summary>
    [JsonProperty(PropertyName = "id")]
    public string FileId { get; internal init; }

    /// <summary>
    /// 文件名
    /// </summary>
    [JsonProperty(PropertyName = "name")]
    public string Name { get; internal init; }

    /// <summary>
    /// 文件大小(Byte)
    /// </summary>
    [JsonProperty(PropertyName = "size")]
    public long Size { get; internal init; }

    /// <summary>
    /// 未知字段
    /// </summary>
    [JsonProperty(PropertyName = "busid")]
    public long Busid { get; internal init; }
}