using System.ComponentModel;

namespace Sora.Enumeration.EventParamsType;

/// <summary>
/// 荣誉类型
/// </summary>
[DefaultValue(Unknown)]
public enum HonorType
{
    /// <summary>
    /// 未知，在转换错误时为此值
    /// </summary>
    [Description("")]
    Unknown,

    /// <summary>
    /// 龙王
    /// </summary>
    [Description("talkative")] 
    Talkative,

    /// <summary>
    /// 群聊之火
    /// </summary>
    [Description("performer")]
    Performer,

    /// <summary>
    /// 快乐源泉
    /// </summary>
    [Description("emotion")] 
    Emotion
}