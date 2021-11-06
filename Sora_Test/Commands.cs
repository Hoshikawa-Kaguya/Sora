using Sora.Attributes.Command;
using Sora.EventArgs.SoraEvent;
using System.Threading.Tasks;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using YukariToolBox.FormatLog;

namespace Sora_Test
{
    [CommandGroup]
    public static class Commands
    {
        [GroupCommand(CommandExpressions = new[] { "1" })]
        public static async ValueTask TestCommand(GroupMessageEventArgs eventArgs)
        {
            var s = eventArgs.Message.MessageBody[0].Data as TextSegment;
            Log.Info("触发指令", $"txet:{s!.Content}");
            eventArgs.IsContinueEventChain = false;
            await eventArgs.Reply(SoraSegment.At(eventArgs.Sender) + "怪欸");
        }
    }
}