using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sora.Attributes;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.Util;

namespace Sora.Entities.Info.InternalDataInfo;

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
    internal MemberRoleType PermissionType { get; }

    /// <summary>
    /// 动态指令委托来源
    /// </summary>
    internal SourceFlag? SourceFlag { get; }

    /// <summary>
    /// 动态指令委托
    /// </summary>
    internal Func<GroupMessageEventArgs, ValueTask> GroupActionBlock { get; }

    /// <summary>
    /// 动态指令委托
    /// </summary>
    internal Func<PrivateMessageEventArgs, ValueTask> PrivateActionBlock { get; }

    /// <summary>
    /// 执行实例
    /// </summary>
    internal Type InstanceType { get; }

    /// <summary>
    /// 优先级
    /// </summary>
    internal int Priority { get; }

    /// <summary>
    /// 正则匹配选项
    /// </summary>
    internal RegexOptions RegexOptions { get; }

    /// <summary>
    /// 执行类型
    /// </summary>
    internal InvokeType InvokeType { get; }

    /// <summary>
    /// 指令执行异常处理
    /// </summary>
    internal Action<Exception> ExceptionHandler { get; }

    #endregion

    #region 构造方法

    /// <summary>
    /// 指令信息构造(常规指令构建)
    /// </summary>
    internal CommandInfo(string desc, string[] regex, string groupName, MethodInfo method,
                         MemberRoleType permissionType, int priority, RegexOptions regexOptions,
                         Action<Exception> exceptionHandler, Type instanceType = null)
    {
        Desc               = desc;
        Regex              = regex;
        GroupName          = groupName;
        MethodInfo         = method;
        InstanceType       = instanceType;
        PermissionType     = permissionType;
        GroupActionBlock   = null;
        PrivateActionBlock = null;
        Priority           = priority;
        RegexOptions       = regexOptions;
        InvokeType         = InvokeType.Method;
        SourceFlag         = null;
        ExceptionHandler   = exceptionHandler;
    }

    /// <summary>
    /// 指令信息构造(动态指令构建)
    /// </summary>
    internal CommandInfo(string desc, string[] regex, string groupName,
                         Func<GroupMessageEventArgs, ValueTask> actionBlock, Action<Exception> exceptionHandler,
                         MemberRoleType permissionType, int priority, RegexOptions regexOptions)
    {
        Desc               = desc;
        Regex              = regex;
        GroupName          = groupName;
        MethodInfo         = null;
        InstanceType       = null;
        GroupActionBlock   = actionBlock;
        PrivateActionBlock = null;
        PermissionType     = permissionType;
        Priority           = priority;
        RegexOptions       = regexOptions;
        InvokeType         = InvokeType.Action;
        ExceptionHandler   = exceptionHandler;
        SourceFlag         = Enumeration.SourceFlag.Group;
    }

    /// <summary>
    /// 指令信息构造(动态指令构建)
    /// </summary>
    internal CommandInfo(string desc, string[] regex, string groupName,
                         Func<PrivateMessageEventArgs, ValueTask> actionBlock, Action<Exception> exceptionHandler,
                         MemberRoleType permissionType, int priority, RegexOptions regexOptions)
    {
        Desc               = desc;
        Regex              = regex;
        GroupName          = groupName;
        MethodInfo         = null;
        InstanceType       = null;
        GroupActionBlock   = null;
        PrivateActionBlock = actionBlock;
        PermissionType     = permissionType;
        Priority           = priority;
        RegexOptions       = regexOptions;
        InvokeType         = InvokeType.Action;
        ExceptionHandler   = exceptionHandler;
        SourceFlag         = Enumeration.SourceFlag.Private;
    }

    #endregion

    [NeedReview("ALL")]
    internal bool Equals(CommandInfo another)
    {
        if (InvokeType != another.InvokeType) return false;

        return InvokeType switch
        {
            InvokeType.Method => MethodInfo.Name == another.MethodInfo.Name &&
                                 MethodInfo.GetGenericArguments()
                                           .ArrayEquals(another.MethodInfo.GetGenericArguments()) &&
                                 Regex.ArrayEquals(another.Regex) && PermissionType == another.PermissionType &&
                                 Priority == another.Priority,
            InvokeType.Action => GroupActionBlock.Equals(another.GroupActionBlock) &&
                                 PrivateActionBlock.Equals(another.PrivateActionBlock) &&
                                 Regex.ArrayEquals(another.Regex) && PermissionType == another.PermissionType &&
                                 Priority == another.Priority,
            _ => throw new NotSupportedException("unknown InvokeType found")
        };
    }
}