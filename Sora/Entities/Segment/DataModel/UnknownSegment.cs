using Newtonsoft.Json.Linq;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// <para>未知消息段</para>
/// <para>此消息段需要在pb序列化前去除</para>
/// </summary>
public sealed record UnknownSegment : BaseSegment
{
    internal UnknownSegment()
    {
    }

#region 属性

    /// <summary>
    /// 纯文本内容
    /// </summary>
    public JObject Content { get; internal set; }

#endregion
}