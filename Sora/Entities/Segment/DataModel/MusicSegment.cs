using Newtonsoft.Json;
using ProtoBuf;
using Sora.Converter;
using Sora.Enumeration.EventParamsType;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 音乐分享
/// 仅发送
/// </summary>
[ProtoContract]
public sealed record MusicSegment : BaseSegment
{
    internal MusicSegment()
    {
    }

    /// <summary>
    /// 音乐分享类型
    /// </summary>
    [JsonConverter(typeof(EnumDescriptionConverter))]
    [JsonProperty(PropertyName = "type")]
    [ProtoMember(1)]
    public MusicShareType MusicType { get; internal set; }

    /// <summary>
    /// 歌曲 ID
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "id")]
    [ProtoMember(2)]
    public long MusicId { get; internal set; }
}