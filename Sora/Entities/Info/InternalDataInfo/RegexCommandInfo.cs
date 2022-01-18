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
internal readonly struct RegexCommandInfo
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
    internal SourceFlag SourceFlag { get; }

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
    /// 指令执行异常处理
    /// </summary>
    internal Action<Exception> ExceptionHandler { get; }

    #endregion

    #region 构造方法

    /// <summary>
    /// 指令信息构造(常规指令构建)
    /// </summary>
    internal RegexCommandInfo(string    desc,           string[] regex,    string       groupName,    MethodInfo method,
                         MemberRoleType permissionType, int      priority, RegexOptions regexOptions, SourceFlag source,
                         Action<Exception> exceptionHandler, Type     instanceType = null)
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
        SourceFlag         = source;
        ExceptionHandler   = exceptionHandler;
    }

    #endregion

    [NeedReview("ALL")]
    internal bool Equals(RegexCommandInfo another)
    {
        return MethodInfo.Name == another.MethodInfo.Name &&
            MethodInfo.GetGenericArguments()
                      .ArrayEquals(another.MethodInfo.GetGenericArguments()) &&
            Regex.ArrayEquals(another.Regex) && PermissionType == another.PermissionType &&
            Priority == another.Priority;
    }
}