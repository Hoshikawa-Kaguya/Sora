using Newtonsoft.Json;
using ProtoBuf;
using Sora.Converter;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// <para>群成员戳一戳</para>
/// <para>仅发送</para>
/// <para>仅支持GoCQ</para>
/// </summary>
[ProtoContract]
public sealed record PokeSegment : BaseSegment
{
    internal PokeSegment()
    {
    }

#region 属性

    /// <summary>
    /// 需要戳的成员
    /// </summary>
    [JsonConverter(typeof(StringConverter))]
    [JsonProperty(PropertyName = "qq")]
    [ProtoMember(1)]
    public long Uid { get; internal set; }

#endregion
}