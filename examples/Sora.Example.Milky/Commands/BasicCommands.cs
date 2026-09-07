using Sora.Example.Milky.Filters;

namespace Sora.Example.Milky.Commands;

/// <summary>
///     普通指令组。类级 <see cref="Filters.LoggingAttribute" /> 演示组级 attribute filter
///     在组内所有命令上共享同一实例（计数器跨命令累计）。
/// </summary>
[CommandGroup(Name = "basic", Prefix = "/")]
[Logging]
public static class BasicCommands
{
    [Cooldown(5)]
    [Command(Expressions = ["ping"], MatchType = MatchType.Full, Description = "ping", ReentryMessage = "你要干嘛")]
    public static async ValueTask Ping(MessageReceivedEvent e)
    {
        await Helpers.SendReplyAsync(e, new MessageBody("ybb"));
    }
}