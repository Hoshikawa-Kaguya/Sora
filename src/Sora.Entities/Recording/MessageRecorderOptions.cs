namespace Sora.Entities.Recording;

/// <summary>Queue options for one registered <see cref="IMessageRecorder" />.</summary>
public sealed record MessageRecorderOptions
{
    /// <summary>
    ///     Gets the maximum number of queued records. Must be positive. Defaults to 4096.
    ///     When the queue is full, new records are dropped with a rate-limited warning; recording never blocks.
    /// </summary>
    public int Capacity { get; init; } = 4096;
}
