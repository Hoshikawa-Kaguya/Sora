using System.ComponentModel;

namespace Sora.Enumeration.EventParamsType;

/// <summary>
/// 成员权限等级
/// </summary>
[DefaultValue(Unknown)]
public enum MemberRoleType
{
    /// <summary>
    /// 未知
    /// </summary>
    [Description("")] 
    Unknown = 0,

    /// <summary>
    /// 成员
    /// </summary>
    [Description("member")] 
    Member = 1,

    /// <summary>
    /// 管理员
    /// </summary>
    [Description("admin")] 
    Admin = 2,

    /// <summary>
    /// 群主
    /// </summary>
    [Description("owner")] 
    Owner = 3,

    /// <summary>
    /// 该服务的管理员
    /// </summary>
    SuperUser = 4
}