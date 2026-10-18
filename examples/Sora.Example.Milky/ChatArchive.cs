using Microsoft.Extensions.Logging;
using Sora.Entities.Interfaces;
using Sora.Entities.Recording;

namespace Sora.Example.Milky;

public sealed class ChatArchive : IMessageRecorder
{
    public async ValueTask OnMessageAsync(MessageRecord record, CancellationToken ct)
    {
        ILogger logger = SoraLogger.CreateLogger<ChatArchive>();
        logger.LogInformation($"{record.Direction} msg: {record.MessageId} t: {record.Time}");
    }
}