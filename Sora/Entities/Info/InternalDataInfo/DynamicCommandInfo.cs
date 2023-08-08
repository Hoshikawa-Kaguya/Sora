using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;

namespace Sora.Entities.Info.InternalDataInfo;

internal sealed record DynamicCommandInfo : BaseCommandInfo
{
    internal readonly Guid                                  CommandId;
    internal readonly Func<BaseMessageEventArgs, ValueTask> Command;
    internal readonly Func<BaseMessageEventArgs, bool>      CommandMatchFunc; //自定义匹配方法

    /// <summary>
    /// 指令信息构造(群聊动态指令构建)
    /// </summary>
    public DynamicCommandInfo(string                                desc,
                              string[]                              regex,
                              Func<BaseMessageEventArgs, bool>      matchFunc,
                              MemberRoleType                        permissionType,
                              int                                   priority,
                              RegexOptions                          regexOptions,
                              MessageSourceMatchFlag                sourceType,
                              long[]                                sourceGroups,
                              long[]                                sourceUsers,
                              long[]                                sourceLogins,
                              Func<BaseMessageEventArgs, ValueTask> command,
                              Guid                                  commandId,
                              bool                                  suCommand,
                              string                                seriesName)
        : base(desc,
               regex,
               permissionType,
               suCommand,
               priority,
               regexOptions,
               sourceType,
               sourceGroups,
               sourceUsers,
               sourceLogins,
               seriesName,
               commandId.ToString())
    {
        CommandId        = commandId;
        Command          = command;
        CommandMatchFunc = matchFunc;
    }
}