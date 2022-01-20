using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sora.Attributes.Command;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;

namespace Sora_Test;

[CommandGroup]
public static class Commands
{
    [RegexCommand(
        CommandExpressions = new[] {"1"},
        Description = "死了啦都你害的啦",
        SourceType = SourceFlag.Group,
        Priority = 10)]
    public static async ValueTask TestCommand1(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;

        var s = eventArgs.Message.MessageBody[0].Data as TextSegment;
        Log.Info("触发指令", $"text:{s!.Content}");
        await eventArgs.Reply($"debug = {eventArgs.IsSuperUser} {eventArgs.Sender.IsSuperUser}");
        var t1 = await eventArgs.SoraApi.GetFriendList();
        Debug.Assert(t1.apiStatus.RetCode == ApiStatusType.Ok);
        Debug.Assert(t1.friendList.Any(u => u.UserId == eventArgs.Sender && u.IsSuperUser));
        Debug.Assert(t1.friendList.Any(u => u.UserId != eventArgs.Sender && !u.IsSuperUser));
        var t2 = await eventArgs.SourceGroup.GetGroupMemberList();
        Debug.Assert(t2.apiStatus.RetCode == ApiStatusType.Ok);
        Debug.Assert(t2.groupMemberList.Any(u => u.UserId == eventArgs.Sender && u.IsSuperUser));
        Debug.Assert(t2.groupMemberList.Any(u => u.UserId != eventArgs.Sender && !u.IsSuperUser));
        var t3 = await eventArgs.SourceGroup.GetGroupMemberInfo(eventArgs.Sender);
        Debug.Assert(t3.apiStatus.RetCode == ApiStatusType.Ok);
        Debug.Assert(t3.memberInfo.IsSuperUser);
        var t4 = await eventArgs.Sender.GetUserInfo();
        Debug.Assert(t4.apiStatus.RetCode == ApiStatusType.Ok);
        Debug.Assert(t4.userInfo.IsSuperUser);
        await eventArgs.Reply($"ok");
        Log.Info("触发指令", $"OK");
    }

    [RegexCommand(
        CommandExpressions = new[] {"2"},
        Description = "死了啦都你害的啦",
        SourceType = SourceFlag.Group)]
    public static async ValueTask TestCommand2(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;

        var s = eventArgs.Message.MessageBody[0].Data as TextSegment;
        Log.Info("触发指令", $"text:{s!.Content}");
        await eventArgs.Reply($"{eventArgs.SourceType}[{(int) eventArgs.SourceType}]");
        var c = 1;
        await eventArgs.Reply($"{c}");
        for (int i = 0; i < 5; i++)
        {
            c++;
            var e = await eventArgs.WaitForNextMessageAsync("1", MatchType.Full);
            await e.Reply($"{c}");
        }
    }
}