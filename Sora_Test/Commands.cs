using System.Collections.Generic;
using Sora.Command.Attributes;
using Sora.EventArgs.SoraEvent;
using System.Threading.Tasks;
using Sora.Entities;

namespace Sora_Test
{
    [CommandGroup]
    public class Commands
    {
        /// <summary>
        /// 请求表
        /// </summary>
        private List<User> requestList { get; set; } = new();

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

        [GroupCommand(CommandExpressions = new[] {"搜图"})]
        public async ValueTask TestCommand3(GroupMessageEventArgs eventArgs)
        {
            if (requestList.Exists(member => member == eventArgs.Sender))
            {
                await eventArgs.Reply("dnmd图呢");
                return;
            }
            await eventArgs.Reply("图呢");
            requestList.Add(eventArgs.Sender);
        }

        public async ValueTask TestCommand4(GroupMemberChangeEventArgs eventArgs)
        {

        }
    }
}