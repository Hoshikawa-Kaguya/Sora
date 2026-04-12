namespace Sora.Example.OneBot11;

internal static class Helpers
{
    internal static async ValueTask SendReplyAsync(MessageReceivedEvent e, MessageBody body)
    {
        if (e.Message.SourceType == MessageSourceType.Group)
            await e.Api.SendGroupMessageAsync(e.Message.GroupId, body);
        else
            await e.Api.SendFriendMessageAsync(e.Message.SenderId, body);
    }
}