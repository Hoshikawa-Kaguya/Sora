using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;

namespace Sora.Entities.Info.InternalDataInfo;

internal sealed record DynamicCommandInfo : BaseCommandInfo
{
    internal readonly Guid                                     CommandId;
    internal readonly Func<GroupMessageEventArgs, ValueTask>   GroupCommand;
    internal readonly Func<PrivateMessageEventArgs, ValueTask> PrivateCommand;

    /// <summary>
    /// 指令信息构造(群聊动态指令构建)
    /// </summary>
    public DynamicCommandInfo(
        string desc,         string[]     regex,        MemberRoleType                         permissionType,
        int    priority,     RegexOptions regexOptions, Action<Exception>                      exceptionHandler,
        long[] sourceGroups, long[]       sourceUsers,  Func<GroupMessageEventArgs, ValueTask> groupCommand,
        Guid   commandId,    bool         suCommand)
        : base(desc, regex, permissionType, suCommand, priority, regexOptions, SourceFlag.Group, exceptionHandler,
            sourceGroups,
            sourceUsers)
    {
        CommandId    = commandId;
        GroupCommand = groupCommand;
    }

    /// <summary>
    /// 指令信息构造(私聊动态指令构建)
    /// </summary>
    public DynamicCommandInfo(
        string                                   desc,             string[]     regex,
        int                                      priority,         RegexOptions regexOptions,
        Action<Exception>                        exceptionHandler, long[]       sourceUsers,
        Func<PrivateMessageEventArgs, ValueTask> privateCommand,   Guid         commandId, bool suCommand)
        : base(desc, regex, MemberRoleType.Member, suCommand, priority, regexOptions, SourceFlag.Private,
            exceptionHandler,
            Array.Empty<long>(), sourceUsers)
    {
        CommandId      = commandId;
        PrivateCommand = privateCommand;
    }
}