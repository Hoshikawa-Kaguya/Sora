using System.Collections.Generic;
using Sora.Attributes.Command;
using Sora.EventArgs.SoraEvent;
using System.Threading.Tasks;
using Sora.Entities;
using YukariToolBox.FormatLog;
using MatchType = Sora.Enumeration.MatchType;

namespace Sora_Test
{
    [CommandGroup]
    public class Commands
    {
        /// <summary>
        /// 请求表
        /// </summary>
        private List<User> requestList { get; } = new();

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

        [GroupCommand(CommandExpressions = new[] {"pixiv搜图"})]
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

        [GroupCommand(CommandExpressions = new[] {@"^\[CQ:image,file=[a-z0-9]+\.image\]$"},
                      MatchType          = MatchType.Regex)]
        public async ValueTask TestCommand4(GroupMessageEventArgs eventArgs)
        {
            if (!requestList.Exists(member => member == eventArgs.Sender)) return;
            Log.Debug("pic", $"get pic {eventArgs.Message.RawText} searching...");
            requestList.RemoveAll(user => user == eventArgs.Sender);

            await eventArgs.Reply(await SaucenaoSearch.SearchByUrl("92a805aff18cbc56c4723d7e2d5100c6892fe256", eventArgs.Message.GetAllImage()[0].Url, eventArgs));
        }
    }
}