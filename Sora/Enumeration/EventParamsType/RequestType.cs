using System.ComponentModel;

namespace Sora.Enumeration.EventParamsType;

/// <summary>
/// 请求类型
/// </summary>
[DefaultValue(Unknown)]
public enum RequestType
{
    /// <summary>
    /// 未知，在转换错误时为此值
    /// </summary>
    [Description("")]
    Unknown,

    /// <summary>
    /// 群组请求
    /// </summary>
    [Description("group")] 
    Group,

    /// <summary>
    /// 好友请求
    /// </summary>
    [Description("friend")] 
    Friend
}