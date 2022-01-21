using System.Threading.Tasks;
using Sora.Attributes.Command;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;

namespace Sora_Test;

[CommandGroup]
public static class Commands
{
    [SoraCommand(
        CommandExpressions = new[] {"1"},
        Description = "死了啦都你害的啦",
        SourceType = SourceFlag.Group,
        PermissionLevel = MemberRoleType.Admin,
        Priority = 10)]
    public static async ValueTask TestCommand1(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;

        var s = eventArgs.Message.MessageBody[0].Data as TextSegment;
        Log.Info("触发指令", $"text:{s!.Content}");
        await eventArgs.Reply($"哇哦");
    }
}