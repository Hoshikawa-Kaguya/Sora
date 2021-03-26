using Sora.Attributes.Command;
using Sora.EventArgs.SoraEvent;
using System.Threading.Tasks;
using Sora.Entities.CQCodes;
using Sora.Entities.CQCodes.CQCodeModel;
using Sora.Enumeration;

namespace Sora_Test
{
    [CommandGroup]
    public static class Commands
    {
        [GroupCommand(CommandExpressions = new[] {"好耶", "哇噢"}, Priority = 1)]
        public static async ValueTask TestCommand1(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("这是1");
            eventArgs.TriggerAfterThis = true;
        }

        [GroupCommand(CommandExpressions = new[] {"好耶", "哇噢"}, Priority = 0)]
        public static async ValueTask TestCommand3(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("这是0");
            eventArgs.TriggerAfterThis = false;
        }

        [GroupCommand(CommandExpressions = new[] {"来点色图"},
                      MatchType          = MatchType.KeyWord)]
        public static async ValueTask TestCommand2(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("不给");
        }

        [GroupCommand(CommandExpressions = new[] {@"^echo\s[\s\S]+$"},
                      MatchType          = MatchType.Regex)]
        public static async ValueTask Echo(GroupMessageEventArgs eventArgs)
        {
            //处理开头字符串
            if (eventArgs.Message.MessageList[0].Function == CQFunction.Text)
            {
                if (eventArgs.Message.MessageList[0].CQData is Text str && str.Content.StartsWith("echo "))
                {
                    if (str.Content.Equals("echo ")) eventArgs.Message.MessageList.RemoveAt(0);
                    else eventArgs.Message.MessageList[0] = CQCode.CQText(str.Content.Substring(5));
                }
            }

            //复读
            if (eventArgs.Message.MessageList.Count != 0) await eventArgs.Reply(eventArgs.Message.MessageList);
        }
    }
}