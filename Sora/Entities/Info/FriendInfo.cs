using Sora.Enumeration.EventParamsType;

namespace Sora.Entities.Info;

/// <summary>
/// 好友信息
/// </summary>
public readonly struct FriendInfo
{
    #region 属性

    /// <summary>
    /// 好友备注
    /// </summary>
    public string Remark { get; internal init; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Nick { get; internal init; }

    /// <summary>
    /// 权限等级
    /// </summary>
    public MemberRoleType Role { get; internal init; }

    /// <summary>
    /// 好友ID
    /// </summary>
    public long UserId { get; internal init; }

    #endregion
}