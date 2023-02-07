using Newtonsoft.Json;
using ProtoBuf;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 自定义音乐分享
/// </summary>
[ProtoContract]
public sealed record CustomMusicSegment : BaseSegment
{
    internal CustomMusicSegment()
    {
    }

    [JsonProperty(PropertyName = "type")]
    [ProtoMember(1)]
    internal string ShareType;

    /// <summary>
    /// 跳转URL
    /// </summary>
    [JsonProperty(PropertyName = "url")]
    [ProtoMember(2)]
    public string Url { get; internal set; }

    /// <summary>
    /// 音乐URL
    /// </summary>
    [JsonProperty(PropertyName = "audio")]
    [ProtoMember(3)]
    public string MusicUrl { get; internal set; }

    /// <summary>
    /// 标题
    /// </summary>
    [JsonProperty(PropertyName = "title")]
    [ProtoMember(4)]
    public string Title { get; internal set; }

    /// <summary>
    /// 内容描述[可选]
    /// </summary>
    [JsonProperty(PropertyName = "content", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(5)]
    public string Content { get; internal set; }

    /// <summary>
    /// 分享内容图片[可选]
    /// </summary>
    [JsonProperty(PropertyName = "image", NullValueHandling = NullValueHandling.Ignore)]
    [ProtoMember(6)]
    public string CoverImageUrl { get; internal set; }
}