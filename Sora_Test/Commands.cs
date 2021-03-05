using System.Threading.Tasks;
using Sora.Command.Attributes;
using Sora.EventArgs.SoraEvent;

namespace Sora_Test
{
    [CommandGroup("test group")]
    public class Commands
    {
        [GroupCommand(CommandExpression = "好耶")]
        public async ValueTask TestCommand1(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("坏耶");
        }

        [GroupCommand(CommandExpression = "贴贴")]
        public async ValueTask TestCommand2(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("爪巴");
        }

        [PrivateCommand(CommandExpression = "在")]
        public async ValueTask TestCommand3(PrivateMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("不在");
        }
    }
}
