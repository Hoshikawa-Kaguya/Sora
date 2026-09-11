using System.Text.RegularExpressions;
using MatchType = Sora.Core.Enums.MatchType;

namespace Sora.Entities.MessageWaiting;

/// <summary>
///     Internal state for a single message-waiting session.
/// </summary>
internal sealed class WaitingSession
{
    /// <summary>Task completion source that the caller awaits.</summary>
    public TaskCompletionSource<MessageReceivedEvent?> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Connection this session belongs to.</summary>
    public Guid ConnectionId { get; init; }

    /// <summary>The group context (default for private messages).</summary>
    public GroupId GroupId { get; init; }

    /// <summary>Regex/full/keyword patterns (null if using predicate).</summary>
    public string[]? Patterns { get; init; }

    /// <summary>Custom predicate (null if using patterns).</summary>
    public Func<MessageReceivedEvent, bool>? Predicate { get; init; }

    /// <summary>The sender whose next message we're waiting for.</summary>
    public UserId SenderId { get; init; }

    /// <summary>Unique session identifier.</summary>
    public Guid SessionId { get; } = Guid.NewGuid();

    /// <summary>Match type for pattern-based matching.</summary>
    public MatchType? SessionMatchType { get; init; }

    /// <summary>Message source type filter.</summary>
    public MessageSourceType SourceType { get; init; }

    /// <summary>Atomic registration identity; non-group messages have no group scope.</summary>
    public SessionKey Source => new(ConnectionId, SenderId, GroupId, SourceType);

    /// <summary>
    ///     Tests the criteria of an incoming event already selected by source identity.
    /// </summary>
    /// <param name="incoming">The incoming message event to test.</param>
    /// <returns>True if the event matches; false otherwise.</returns>
    /// <exception cref="NotSupportedException">The session uses an unknown match type.</exception>
    public bool IsMatch(MessageReceivedEvent incoming)
    {
        // If we have a custom predicate, use it
        if (Predicate is not null)
            return Predicate(incoming);

        // No predicate and no patterns = match any message from same source
        if (Patterns is null || !SessionMatchType.HasValue) return true;

        // If we have patterns, match against them
        string text = incoming.Message.Body.GetText();
        return Patterns.Any(pattern =>
                                SessionMatchType.Value switch
                                {
                                    MatchType.Full => string.Equals(text, pattern, StringComparison.Ordinal),
                                    MatchType.Regex => Regex.IsMatch(text, pattern),
                                    MatchType.Keyword => text.Contains(pattern, StringComparison.Ordinal),
                                    _ => throw new NotSupportedException($"Unknown match type:{pattern}")
                                });
    }
}