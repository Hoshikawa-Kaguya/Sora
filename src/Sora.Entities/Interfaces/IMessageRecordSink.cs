namespace Sora.Entities.Interfaces;

/// <summary>
///     Service-owned entry point through which adapters report message records.
///     Attached to the adapter through <see cref="IBotAdapter" /> when the service is constructed.
/// </summary>
internal interface IMessageRecordSink
{
    /// <summary>Gets whether any recorder is currently receiving records; adapters skip building records when false.</summary>
    bool IsEnabled { get; }

    /// <summary>
    ///     Queues a record for every recorder without blocking. A record is dropped for a recorder whose queue is full.
    /// </summary>
    /// <param name="record">The record to queue.</param>
    void Write(MessageRecord record);
}
