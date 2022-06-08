using System;
using System.Text.RegularExpressions;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;

namespace Sora.Entities.Info.InternalDataInfo;

internal abstract record BaseCommandInfo
{
    internal readonly string            Desc;             //注释
    internal readonly Regex[]           Regex;            //正则表达式
    internal readonly MemberRoleType    PermissionType;   //指令执行权限,私聊无效
    internal readonly bool              SuperUserCommand; //机器人管理员指令
    internal readonly SourceFlag        SourceType;       //指令匹配源类型
    internal readonly int               Priority;         //优先级
    internal readonly Action<Exception> ExceptionHandler; //异常回调
    internal readonly long[]            SourceGroups;     //群组限制,私聊无效
    internal readonly long[]            SourceUsers;      //用户限制

    internal BaseCommandInfo(
        string            desc,             string[] regex,        MemberRoleType permissionType,
        bool              suCommand,        int      priority,     RegexOptions   regexOptions, SourceFlag source,
        Action<Exception> exceptionHandler, long[]   sourceGroups, long[]         sourceUsers)
    {
        Desc             = desc;
        PermissionType   = permissionType;
        SuperUserCommand = suCommand;
        Priority         = priority;
        SourceType       = source;
        SourceGroups     = sourceGroups;
        SourceUsers      = sourceUsers;
        ExceptionHandler = exceptionHandler;

        Regex = new Regex[regex.Length];
        for (int i = 0; i < regex.Length; i++) Regex[i] = new Regex(regex[i], RegexOptions.Compiled | regexOptions);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int) PermissionType, SuperUserCommand, (int) SourceType, Priority, SourceType, Regex);
    }
}