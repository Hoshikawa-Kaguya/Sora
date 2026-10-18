namespace Sora.Entities.Recording;

/// <summary>Adapter-side reporting helpers that keep the recording contract in one place.</summary>
internal static class MessageRecordSinkExtensions
{
    /// <summary>
    ///     Reports a converted received message before it is dispatched. Messages sent by the bot account are skipped
    ///     whatever the adapter's self-message setting is, because they are recorded only by the send path.
    /// </summary>
    /// <param name="sink">The attached sink.</param>
    /// <param name="protocolName">The protocol name of the reporting adapter.</param>
    /// <param name="e">The converted message event.</param>
    internal static void WriteInbound(this IMessageRecordSink sink, string protocolName, MessageReceivedEvent e)
    {
        if (!sink.IsEnabled || e.Message.SenderId == e.SelfId) return;
        sink.Write(MessageRecord.Inbound(protocolName, e));
    }
}