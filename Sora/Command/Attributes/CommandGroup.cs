using System;

namespace Sora.Command.Attributes
{
    /// <summary>
    /// 指令组
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandGroup : Attribute
    {
        /// <summary>
        /// 指令组名
        /// </summary>
        public string GroupName { get; }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="groupName">分组名</param>
        public CommandGroup(string groupName)
        {
            GroupName = groupName ?? throw new NullReferenceException(nameof(groupName));
        }
    }
}