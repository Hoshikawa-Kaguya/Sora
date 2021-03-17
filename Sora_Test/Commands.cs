using System.Collections.Generic;
using Sora.Attributes.Command;
using Sora.EventArgs.SoraEvent;
using System.Threading.Tasks;
using Sora;
using Sora.Entities.CQCodes;

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
    }
}