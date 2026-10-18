namespace Sora.Entities.Interfaces;

/// <summary>
///     Read-only receiver of chat message records. Register through <c>SoraService.UseMessageRecorder</c>.
/// </summary>
/// <remarks>
///     Each registered recorder has its own queue and is called sequentially by a single consumer, so a recorder
///     registered on one service is never called concurrently. A recorder instance registered on several services
///     is called concurrently and must be thread-safe.
/// </remarks>
public interface IMessageRecorder
{
    /// <summary>Handles one message record.</summary>
    /// <param name="record">The read-only message record.</param>
    /// <param name="ct">Canceled when the service abandons draining the queue.</param>
    ValueTask OnMessageAsync(MessageRecord record, CancellationToken ct);
}
