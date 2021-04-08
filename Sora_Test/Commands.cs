using System.Linq;
using Sora.Attributes.Command;
using Sora.EventArgs.SoraEvent;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Sora.Entities.MessageElement.CQModel;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;

namespace Sora_Test
{
    [CommandGroup]
    public static class Commands
    {
        [GroupCommand(CommandExpressions = new[] {"1"}, Priority = 0)]
        [UsedImplicitly]
        public static async ValueTask TestCommand1(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("cmd1_1");
            var ea1 = await eventArgs.WaitForNextMessageAsync(new[] {"2"}, MatchType.Full);
            if (ea1 != null)
            {
                await ea1.Reply("cmd1_2");
                var ea2 = await ea1.WaitForNextMessageAsync(new[] {"3"}, MatchType.Full);
                await ea2.Reply("cmd1_3");
            }
        }

        [GroupCommand(CommandExpressions = new[] {"好耶", "哇噢"}, Priority = 0)]
        [UsedImplicitly]
        public static async ValueTask TestCommand2(GroupMessageEventArgs eventArgs)
        {
            var (apiStatus, list) = await eventArgs.SourceGroup.GetGroupMemberList();
            if (apiStatus.RetCode != ApiStatusType.OK) return;
            var nodes = list.Select(member => new CustomNode(member.Nick, member.UserId, "好耶"))
                            .ToList();
            await eventArgs.SourceGroup.SendGroupForwardMsg(nodes);
        }
    }
}