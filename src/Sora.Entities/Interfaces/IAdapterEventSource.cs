namespace Sora.Entities.Interfaces;

/// <summary>
///     Internal event source for adapter-to-service communication.
///     Adapters implement this alongside <see cref="IBotAdapter" /> to push events into the framework pipeline.
/// </summary>
internal interface IAdapterEventSource
{
    /// <summary>Raised when a bot event is received from the protocol.</summary>
    event Func<BotEvent, ValueTask> OnEvent;
}