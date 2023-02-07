using System;
using Newtonsoft.Json;
using ProtoBuf;
using Sora.Util;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 自定义回复
/// </summary>
[ProtoContract]
public sealed record CustomReplySegment : BaseSegment
{
    internal CustomReplySegment()
    {
    }

    /// <summary>
    /// 自定义回复的信息
    /// </summary>
    [JsonProperty(PropertyName = "text")]
    [ProtoMember(1)]
    public string Text { get; internal set; }

    /// <summary>
    /// 自定义回复时的自定义QQ
    /// </summary>
    [JsonProperty(PropertyName = "qq")]
    [ProtoMember(2)]
    public long Uid { get; internal set; }

    [JsonProperty(PropertyName = "time")]
    [ProtoMember(3)]
    private long TimeStamp { get; set; }

    /// <summary>
    /// 自定义回复时的时间
    /// </summary>
    [JsonIgnore]
    public DateTime Time
    {
        get => TimeStamp.ToDateTime();
        init => TimeStamp = value.ToTimeStamp();
    }

    /// <summary>
    /// 起始消息序号, 可通过 <see langword="GetMessages"/> 获得
    /// </summary>
    [JsonProperty(PropertyName = "seq")]
    [ProtoMember(4)]
    public long MessageSequence { get; internal set; }
}