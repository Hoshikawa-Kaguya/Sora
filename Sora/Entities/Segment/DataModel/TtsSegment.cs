using Newtonsoft.Json;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 语音转文字（TTS）
/// </summary>
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
    public string Content { get; internal set; }

    #endregion
}