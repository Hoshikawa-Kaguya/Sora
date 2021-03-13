using System.Threading.Tasks;
using Sora.Command.Attributes;
using Sora.EventArgs.SoraEvent;

namespace Sora_DllTest
{
    [CommandGroup]
    public class MyCommand
    {
        [GroupCommand(CommandExpressions = new[] {"坏唉"})]
        public async ValueTask TestCommand0(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("成功");
        }

        [GroupCommand(CommandExpressions = new[] {"好耶", "哇噢"})]
        public async ValueTask TestCommand1(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("坏耶");
        }
    }
}