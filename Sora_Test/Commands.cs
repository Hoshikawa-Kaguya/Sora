using System.Threading.Tasks;
using Sora.Attributes.Command;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

namespace Sora_Test;

[CommandSeries(SeriesName = "test")]
public static class Commands
{
    [SoraCommand(CommandExpressions = new[] { "1" },
                 Description = "死了啦都你害的啦",
                 SourceType = MessageSourceMatchFlag.All,
                 Priority = 0)]
    public static async ValueTask TestCommand1(BaseMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        // TextSegment s = eventArgs.Message.MessageBody[0].Data as TextSegment;
        await eventArgs.Reply("wow");
    }
}