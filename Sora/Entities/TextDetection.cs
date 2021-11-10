using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sora.Entities;

/// <summary>
/// OCR识别结果
/// </summary>
public struct TextDetection
{
    /// <summary>
    /// 文本
    /// </summary>
    [JsonProperty(PropertyName = "text")]
    public string Text { get; private init; }

    /// <summary>
    /// 置信度
    /// </summary>
    [JsonProperty(PropertyName = "confidence")]
    public int Confidence { get; private init; }

    /// <summary>
    /// 坐标
    /// </summary>
    [JsonProperty(PropertyName = "coordinates")]
    public List<Vector2> Coordinates { get; private init; }
}