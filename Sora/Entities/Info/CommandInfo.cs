using System;
using System.Reflection;
using Sora.Enumeration.EventParamsType;

namespace Sora.Entities.Info
{
    /// <summary>
    /// 指令信息
    /// </summary>
    internal readonly struct CommandInfo
    {
        #region 属性

        /// <summary>
        /// 指令描述
        /// </summary>
        internal string Desc { get; }

        /// <summary>
        /// 匹配指令的正则
        /// </summary>
        internal string[] Regex { get; }

        /// <summary>
        /// 指令组名
        /// </summary>
        internal string GroupName { get; }

        /// <summary>
        /// 指令回调方法
        /// </summary>
        internal MethodInfo MethodInfo { get; }

        /// <summary>
        /// 权限限制
        /// </summary>
        internal MemberRoleType? PermissonType { get; }

        /// <summary>
        /// 执行实例
        /// </summary>
        internal Type InstanceType { get; }

        /// <summary>
        /// 是否在指令后触发事件
        /// </summary>
        public bool TriggerEventAfterCommand { get; }

        #endregion

        #region 构造方法

        /// <summary>
        /// 指令信息构造
        /// </summary>
        internal CommandInfo(string desc, string[] regex, string groupName, MethodInfo method,
                             MemberRoleType? permissonType, bool triggerEventAfterCommand, Type instanceType = null)
        {
            Desc                     = desc;
            Regex                    = regex;
            GroupName                = groupName;
            MethodInfo               = method;
            InstanceType             = instanceType;
            TriggerEventAfterCommand = triggerEventAfterCommand;
            PermissonType            = permissonType;
        }

        #endregion
    }
}