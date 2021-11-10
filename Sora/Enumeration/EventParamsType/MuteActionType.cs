using System.ComponentModel;

namespace Sora.Enumeration.EventParamsType;

/// <summary>
/// 禁言操作类型
/// </summary>
[DefaultValue(Unknown)]
public enum MuteActionType
{
    /// <summary>
    /// 未知，在转换错误时为此值
    /// </summary>
    [Description("")]
    Unknown,

    /// <summary>
    /// 开启
    /// </summary>
    [Description("ban")] 
    Enable,

    /// <summary>
    /// 解除
    /// </summary>
    [Description("lift_ban")]
    Disable
}