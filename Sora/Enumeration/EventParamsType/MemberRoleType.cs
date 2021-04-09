using System.ComponentModel;

namespace Sora.Enumeration.EventParamsType
{
    /// <summary>
    /// 成员权限等级
    /// </summary>
    [DefaultValue(Member)]
    public enum MemberRoleType
    {
        /// <summary>
        /// 成员
        /// </summary>
        [Description("member")] Member = 0,

        /// <summary>
        /// 管理员
        /// </summary>
        [Description("admin")] Admin = 1,

        /// <summary>
        /// 群主
        /// </summary>
        [Description("owner")] Owner = 2,

        /// <summary>
        /// 该服务的管理员
        /// </summary>
        SuperUser = 3
    }
}