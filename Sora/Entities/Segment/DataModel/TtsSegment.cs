using Newtonsoft.Json;
using ProtoBuf;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 语音转文字（TTS）
/// </summary>
[ProtoContract]
public sealed record TtsSegment : BaseSegment
{
    internal TtsSegment()
    {
    }

#region 属性

    /// <summary>
    /// 纯文本内容
    /// </summary>
    [JsonProperty(PropertyName = "text")]
    [ProtoMember(1)]
    public string Content { get; internal set; }

#endregion
}