using System.ComponentModel;

namespace Sora.Enumeration.EventParamsType;

/// <summary>
/// 消息类型
/// </summary>
[DefaultValue(Unknown)]
public enum MessageType
{
    /// <summary>
    /// 未知，在转换错误时为此值
    /// </summary>
    [Description("")]
    Unknown,

    /// <summary>
    /// 私聊消息
    /// </summary>
    [Description("private")] 
    Private,

    /// <summary>
    /// 群消息
    /// </summary>
    [Description("group")]
    Group
}