using System.Threading.Tasks;
using Sora;
using Sora.Attributes.Command;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using Sora.Interfaces;
using YukariToolBox.LightLog;

namespace Sora_Test;

[CommandGroup(SeriesName = "test")]
public static class Commands
{
    [SoraCommand(
        CommandExpressions = new[] {"1"},
        Description = "死了啦都你害的啦",
        SourceType = SourceFlag.Group,
        Priority = 0)]
    public static async ValueTask TestCommand1(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        TextSegment s = eventArgs.Message.MessageBody[0].Data as TextSegment;
        Log.Info("触发指令", $"text:{s!.Content}");
        await eventArgs.Reply($"哇哦1");

        SoraServiceFactory.TryGetService(eventArgs.ServiceId, out ISoraService service);

        service.Event.CommandManager.TryDisableGroupCommand("test", eventArgs.SourceGroup);
    }
}