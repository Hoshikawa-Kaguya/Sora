using System.ComponentModel;

namespace Sora.Enumeration.EventParamsType;

/// <summary>
/// 音乐分享类型
/// </summary>
[DefaultValue(Unknown)]
public enum MusicShareType
{
    /// <summary>
    /// 未知
    /// </summary>
    [Description("")] 
    Unknown,

    /// <summary>
    /// 网易云音乐
    /// </summary>
    [Description("163")] 
    Netease,

    /// <summary>
    /// QQ 音乐
    /// </summary>
    [Description("qq")] 
    QQMusic
}