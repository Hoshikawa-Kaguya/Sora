namespace Sora.Entities.Interfaces;

/// <summary>
///     An inbound event that belongs to a concrete message source (friend, group, or temporary session).
///     Event filters use it to evaluate their <see cref="MessageSourceScope" />; events that do not implement it
///     have no message source.
/// </summary>
public interface ISourcedEvent
{
    /// <summary>The concrete message source of this event.</summary>
    MessageSourceType SourceType { get; }
}