namespace Sora.Entities.Filters;

/// <summary>
///     Internal helpers that evaluate whether an <see cref="IEventPreFilter" /> or
///     <see cref="IEventPostFilter" /> should run for a given <see cref="BotEvent" />, based on the
///     filter's declared scope (<c>EventTypes</c>, <c>SourceScope</c>, <c>Predicate</c>).
/// </summary>
internal static class EventFilterScope
{
    /// <summary>
    ///     Returns <c>true</c> if the given pre-filter should run for the given event according to its
    ///     declared scope. <see cref="IEventPreFilter.Predicate" /> exceptions are caught, logged at
    ///     warning level, and treated as a scope mismatch (filter skipped — fail-safe).
    /// </summary>
    public static bool MatchesScope(IEventPreFilter filter, BotEvent e, ILogger logger) =>
        MatchesCore(filter.GetType(), filter.EventTypes, filter.SourceScope, filter.Predicate, e, logger);

    /// <summary>
    ///     Returns <c>true</c> if the given post-filter should run for the given event according to its
    ///     declared scope. <see cref="IEventPostFilter.Predicate" /> exceptions are caught, logged at
    ///     warning level, and treated as a scope mismatch (filter skipped — fail-safe).
    /// </summary>
    public static bool MatchesScope(IEventPostFilter filter, BotEvent e, ILogger logger) =>
        MatchesCore(filter.GetType(), filter.EventTypes, filter.SourceScope, filter.Predicate, e, logger);

    private static bool MatchesCore(
        Type                  filterType,
        Type[]?               eventTypes,
        MessageSourceScope    sourceScope,
        Func<BotEvent, bool>? predicate,
        BotEvent              e,
        ILogger               logger)
    {
        if (eventTypes is { Length: > 0 } && !eventTypes.Any(t => t.IsInstanceOfType(e)))
            return false;

        // Any admits every event; a narrower scope admits only sourced events whose source it contains.
        if ((sourceScope & MessageSourceScope.Any) != MessageSourceScope.Any
            && (e is not ISourcedEvent sourced || !sourceScope.Contains(sourced.SourceType)))
            return false;

        if (predicate is null) return true;

        try
        {
            return predicate(e);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Event filter {FilterType} predicate threw for event {EventType}; treating as scope mismatch (filter skipped)",
                filterType.Name,
                e.GetType().Name);
            return false;
        }
    }
}