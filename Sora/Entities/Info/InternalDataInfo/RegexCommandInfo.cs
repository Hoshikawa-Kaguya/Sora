using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;

namespace Sora.Entities.Info.InternalDataInfo;

/// <summary>
/// 指令信息
/// </summary>
internal sealed record RegexCommandInfo : BaseCommandInfo
{
    internal readonly string     GroupName;    //指令组名
    internal readonly MethodInfo MethodInfo;   //指令执行的方法
    internal readonly Type       InstanceType; //指令所在实例类型

    /// <summary>
    /// 指令信息构造(常规指令构建)
    /// </summary>
    internal RegexCommandInfo(
        string            desc,             string[] regex,        string       groupName,    MethodInfo method,
        MemberRoleType    permissionType,   int      priority,     RegexOptions regexOptions, SourceFlag source,
        Action<Exception> exceptionHandler, long[]   sourceGroups, long[]       sourceUsers,  Type instanceType = null)
        : base(desc, regex, permissionType, priority, regexOptions, source, exceptionHandler, sourceGroups, sourceUsers)
    {
        GroupName    = groupName;
        MethodInfo   = method;
        InstanceType = instanceType;
    }
}