using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;

namespace Sora.Entities.Info.InternalDataInfo;

internal abstract record BaseCommandInfo
{
    internal readonly string                 Desc;             //注释
    internal readonly Regex[]                Regex;            //正则表达式
    internal readonly MemberRoleType         PermissionType;   //指令执行权限,私聊无效
    internal readonly bool                   SuperUserCommand; //机器人管理员指令
    internal readonly MessageSourceMatchFlag SourceType;       //指令匹配源类型
    internal readonly int                    Priority;         //优先级
    internal readonly HashSet<long>          SourceGroups;     //群组限制,私聊无效
    internal readonly HashSet<long>          SourceUsers;      //用户限制
    internal readonly HashSet<long>          SourceLogins;     //登录账户限制
    internal readonly string                 SeriesName;       //指令组名
    internal readonly string                 CommandName;      //指令名

    internal BaseCommandInfo(string                 desc,
                             string[]               regex,
                             MemberRoleType         permissionType,
                             bool                   suCommand,
                             int                    priority,
                             RegexOptions           regexOptions,
                             MessageSourceMatchFlag source,
                             long[]                 sourceGroups,
                             long[]                 sourceUsers,
                             long[]                 sourceLogins,
                             string                 seriesName,
                             string                 commandName)
    {
        Desc             = desc;
        PermissionType   = permissionType;
        SuperUserCommand = suCommand;
        Priority         = priority;
        SourceType       = source;
        SourceGroups     = sourceGroups.ToHashSet();
        SourceUsers      = sourceUsers.ToHashSet();
        SourceLogins     = sourceLogins.ToHashSet();
        SeriesName       = seriesName;
        CommandName      = string.IsNullOrEmpty(seriesName) ? commandName : $"({seriesName}){commandName}";

        Regex = new Regex[regex.Length];
        for (int i = 0; i < regex.Length; i++)
            Regex[i] = new Regex(regex[i], RegexOptions.Compiled | regexOptions);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)PermissionType, SuperUserCommand, (int)SourceType, Priority, SourceType, Regex);
    }
}