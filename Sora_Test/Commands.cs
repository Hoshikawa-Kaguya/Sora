using System.Threading.Tasks;
using Sora.Attributes.Command;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;

namespace Sora_Test;

[CommandGroup]
public static class Commands
{
    [GuildCommand(CommandExpressions = new[] {"1"}, Description = "死了啦都你害的啦")]
    public static async ValueTask TestCommand(GuildMessageEventArgs eventArgs)
    {
        var s = eventArgs.Messages.MessageBody[0].Data as TextSegment;
        Log.Info("触发指令", $"text:{s!.Content}");
        eventArgs.IsContinueEventChain = false;
        //throw new Exception("shit");
        await eventArgs.Reply(SoraSegment.At(eventArgs.Sender) + "怪欸");
    }
}