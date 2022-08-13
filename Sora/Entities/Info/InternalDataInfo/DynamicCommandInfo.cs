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
    internal readonly Func<BaseMessageEventArgs, bool>         CommandMatchFunc; //自定义匹配方法

    /// <summary>
    /// 指令信息构造(群聊动态指令构建)
    /// </summary>
    public DynamicCommandInfo(
        string desc, string[] regex, Func<BaseMessageEventArgs, bool> matchFunc, MemberRoleType permissionType,
        int priority, RegexOptions regexOptions, long[] sourceGroups, long[] sourceUsers,
        Func<GroupMessageEventArgs, ValueTask> groupCommand, Guid commandId, bool suCommand, string seriesName)
        : base(desc, regex, permissionType, suCommand, priority, regexOptions, SourceFlag.Group, sourceGroups,
            sourceUsers, seriesName, commandId.ToString())
    {
        CommandId        = commandId;
        GroupCommand     = groupCommand;
        CommandMatchFunc = matchFunc;
    }

    /// <summary>
    /// 指令信息构造(私聊动态指令构建)
    /// </summary>
    public DynamicCommandInfo(
        string       desc,         string[] regex,       Func<BaseMessageEventArgs, bool> matchFunc, int priority,
        RegexOptions regexOptions, long[]   sourceUsers, Func<PrivateMessageEventArgs, ValueTask> privateCommand,
        Guid         commandId,    bool     suCommand,   string seriesName)
        : base(desc, regex, MemberRoleType.Member, suCommand, priority, regexOptions, SourceFlag.Private,
            Array.Empty<long>(), sourceUsers, seriesName, commandId.ToString())
    {
        CommandId        = commandId;
        PrivateCommand   = privateCommand;
        CommandMatchFunc = matchFunc;
    }
}