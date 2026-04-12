namespace Sora.Example.Milky.Commands;

/// <summary>
///     普通指令
/// </summary>
[CommandGroup(Name = "basic", Prefix = "/")]
public static class BasicCommands
{
    [Command(Expressions = ["ping"], MatchType = MatchType.Full, Description = "ping", ReentryMessage = "你要干嘛")]
    public static async ValueTask Ping(MessageReceivedEvent e)
    {
        await Helpers.SendReplyAsync(e, new MessageBody("ybb"));
    }
}