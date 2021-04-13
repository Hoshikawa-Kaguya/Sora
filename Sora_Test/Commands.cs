using Sora.Attributes.Command;
using Sora.EventArgs.SoraEvent;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Sora_Test
{
    [CommandGroup]
    public static class Commands
    {
        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {"1"})]
        public static async ValueTask TestCommand(GroupMessageEventArgs eventArgs)
        {
            eventArgs.IsContinueEventChain = false;
            await eventArgs.Reply("怪欸");
        }
    }
}