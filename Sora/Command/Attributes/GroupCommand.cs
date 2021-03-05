using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;

namespace Sora.Command.Attributes
{
    /// <summary>
    /// 群组指令
    /// </summary>
    public sealed class GroupCommand : Command
    {
        /// <summary>
        /// 成员权限限制
        /// </summary>
        internal MemberRoleType PermissionLevel { get; }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="command">指令表达式</param>
        /// <param name="permissionLevel">权限限制</param>
        /// <param name="matchType">匹配类型</param>
        /// <param name="desc">说明(在执行失败时可能会用到)</param>
        public GroupCommand(string command, MemberRoleType permissionLevel, MatchType matchType, string desc = "") :
            base(command, matchType, desc)
        {
            PermissionLevel = permissionLevel;
        }
    }
}
