using Newtonsoft.Json;
using ProtoBuf;
using Sora.Converter;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 回复
/// </summary>
[ProtoContract]
public sealed record ReplySegment : BaseSegment
{
    internal ReplySegment()
    {
    }

#region 属性

    /// <summary>
    /// 消息ID
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "id")]
    [ProtoMember(1)]
    public int Target { get; internal set; }

#endregion
}