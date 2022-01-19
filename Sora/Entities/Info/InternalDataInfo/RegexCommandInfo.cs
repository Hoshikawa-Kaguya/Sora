using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Sora.Attributes;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.Util;

namespace Sora.Entities.Info.InternalDataInfo;

/// <summary>
/// 指令信息
/// </summary>
internal readonly struct RegexCommandInfo
{
    #region 属性

    internal readonly string            Desc;             //注释
    internal readonly string[]          Regex;            //正则表达式
    internal readonly string            GroupName;        //指令组名
    internal readonly MethodInfo        MethodInfo;       //指令执行的方法
    internal readonly MemberRoleType    PermissionType;   //指令执行权限
    internal readonly SourceFlag        SourceFlag;       //指令匹配源类型
    internal readonly Type              InstanceType;     //指令所在实例类型
    internal readonly int               Priority;         //优先级
    internal readonly RegexOptions      RegexOptions;     //正则设置
    internal readonly Action<Exception> ExceptionHandler; //异常回调
    internal readonly long[]            SourceGroups;     //群组限制
    internal readonly long[]            SourceUsers;      //用户限制

    #endregion

    #region 构造方法

    /// <summary>
    /// 指令信息构造(常规指令构建)
    /// </summary>
    internal RegexCommandInfo(
        string            desc,             string[] regex,        string       groupName,    MethodInfo method,
        MemberRoleType    permissionType,   int      priority,     RegexOptions regexOptions, SourceFlag source,
        Action<Exception> exceptionHandler, long[]   sourceGroups, long[]       sourceUsers,  Type instanceType = null)
    {
        Desc             = desc;
        Regex            = regex;
        GroupName        = groupName;
        MethodInfo       = method;
        InstanceType     = instanceType;
        PermissionType   = permissionType;
        Priority         = priority;
        RegexOptions     = regexOptions;
        SourceFlag       = source;
        SourceGroups     = sourceGroups;
        SourceUsers      = sourceUsers;
        ExceptionHandler = exceptionHandler;
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