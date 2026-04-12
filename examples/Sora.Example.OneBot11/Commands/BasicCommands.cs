namespace Sora.Example.OneBot11.Commands;

/// <summary>
///     普通指令
/// </summary>
[CommandGroup(Name = "basic", Prefix = "/")]
public static class BasicCommands
{
    [Command(Expressions = ["ping"], MatchType = MatchType.Full, Description = "ping")]
    public static async ValueTask Ping(MessageReceivedEvent e)
    {
        await Helpers.SendReplyAsync(e, new MessageBody("ybb"));
    }
}