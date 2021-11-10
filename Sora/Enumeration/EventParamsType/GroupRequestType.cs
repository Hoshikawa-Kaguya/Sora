using System.ComponentModel;

namespace Sora.Enumeration.EventParamsType;

/// <summary>
/// 群申请类型
/// </summary>
[DefaultValue(Unknown)]
public enum GroupRequestType
{
    /// <summary>
    /// 未知，在转换错误时为此值
    /// </summary>
    [Description("")] 
    Unknown,

    /// <summary>
    /// 加群申请
    /// </summary>
    [Description("add")]
    Add,

    /// <summary>
    /// 加群邀请
    /// </summary>
    [Description("invite")] 
    Invite
}