using System.Threading.Tasks;
using Sora.Command.Attributes;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;

namespace Sora_Test
{
    [CommandGroup("test group")]
    public class Commands
    {
        [GroupCommand("好耶", MemberRoleType.Member, MatchType.Full, "测试用指令")]
        public async ValueTask TestCommand1(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("坏耶");
        }

        [GroupCommand("贴贴", MemberRoleType.Member, MatchType.Full, "测试用指令")]
        public async ValueTask TestCommand2(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("爪巴");
        }
    }
}
