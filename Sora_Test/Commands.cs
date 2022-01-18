using Sora.Attributes.Command;
using Sora.Entities.Segment.DataModel;
using Sora.EventArgs.SoraEvent;
using System.Threading.Tasks;
using Sora.Enumeration;
using YukariToolBox.LightLog;

namespace Sora_Test;

[CommandGroup]
public static class Commands
{
    [GroupCommand(CommandExpressions = new[] {"1"}, Description = "死了啦都你害的啦")]
    public static async ValueTask TestCommand(GroupMessageEventArgs eventArgs)
    {
        var s = eventArgs.Message.MessageBody[0].Data as TextSegment;
        Log.Info("触发指令", $"text:{s!.Content}");
        eventArgs.IsContinueEventChain = false;
        await eventArgs.Reply($"{eventArgs.SourceType}[{(int)eventArgs.SourceType}]");
        var c = 1;
        await eventArgs.Reply($"{c}");
        for (int i = 0; i < 5; i++)
        {
            c++;
            var e = await eventArgs.WaitForNextMessageAsync("1", MatchType.Full);
            await e.Reply($"{c}");
        }
    }
}