using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// <para>未知消息段</para>
/// <para>此消息段需要在pb序列化前去除</para>
/// </summary>
[ProtoContract]
public sealed record UnknownSegment : BaseSegment
{
    internal UnknownSegment()
    {
    }

#region 属性

    /// <summary>
    /// 内容
    /// </summary>
    public JToken Content { get; internal set; }

    [JsonProperty(PropertyName = "content")]
    [ProtoMember(1)]
    private string CtxStr
    {
        get => Content.ToString(Formatting.None);
        set => Content = JToken.Parse(value);
    }

#endregion
}