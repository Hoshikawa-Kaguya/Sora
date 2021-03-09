using System.Threading.Tasks;
using Sora.Command.Attributes;
using Sora.EventArgs.SoraEvent;

namespace Sora_Test
{
    [CommandGroup]
    public class Commands
    {
        [GroupCommand(CommandExpressions = new[] {"好耶", "哇噢"})]
        public async ValueTask TestCommand1(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("坏耶");
        }

        [GroupCommand(CommandExpressions = new[] {"贴贴"}, Description = "不能贴爪巴")]
        public async ValueTask TestCommand2(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("爪巴");
        }

        [PrivateCommand(CommandExpressions = new[] {"在"})]
        public async ValueTask TestCommand3(PrivateMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("不在");
        }

        public async ValueTask TestCommand4(PrivateMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("不在");
        }
    }
}