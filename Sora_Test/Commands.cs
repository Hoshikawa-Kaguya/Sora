using Sora.Attributes.Command;
using Sora.EventArgs.SoraEvent;
using System.Threading.Tasks;
using Sora.Entities.MessageElement.CQModel;
using Sora.Enumeration;

namespace Sora_Test
{
    [CommandGroup]
    public static class Commands
    {
        [GroupCommand(CommandExpressions = new[] {"1"}, Priority = 0)]
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
        public static async ValueTask TestCommand3(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("cmd3");
            eventArgs.IsContinueEventChain = false;
        }

        [GroupCommand(CommandExpressions = new[] {"怪"},
                      MatchType          = MatchType.KeyWord)]
        public static async ValueTask TestCommand2(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.Reply("cmd2");
        }

        [GroupCommand(CommandExpressions = new[] {@"^echo\s[\s\S]+$"},
                      MatchType          = MatchType.Regex)]
        public static async ValueTask Echo(GroupMessageEventArgs eventArgs)
        {
            //处理开头字符串
            if (eventArgs.Message.MessageBody[0].MessageType == CQType.Text)
            {
                if (eventArgs.Message.MessageBody[0].DataObject is Text str && str.Content.StartsWith("echo "))
                {
                    if (str.Content.Equals("echo ")) eventArgs.Message.MessageBody.RemoveAt(0);
                    else eventArgs.Message.MessageBody[0] = str.Content[5..];
                }
            }

            //复读
            if (eventArgs.Message.MessageBody.Count != 0) await eventArgs.Reply(eventArgs.Message.MessageBody);
        }
    }
}