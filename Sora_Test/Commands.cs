using Sora.Attributes.Command;
using Sora.EventArgs.SoraEvent;
using System.Threading.Tasks;

namespace Sora_Test
{
    [CommandGroup]
    public static class Commands
    {
        [GroupCommand(CommandExpressions = new[] { "1" })]
        public static async ValueTask TestCommand(GroupMessageEventArgs eventArgs)
        {
            eventArgs.IsContinueEventChain = false;
            await eventArgs.Reply("怪欸");
        }
    }
}