namespace Sora.Entities.Filters;

/// <summary>
///     Event pre-filter. Executes on <see cref="Events.BotEvent" /> instances before the Command Manager and EventDispatcher.
///     Return <c>false</c> to short-circuit the entire event chain.
/// </summary>
/// <remarks>
///     <para>
///         Scope is opt-in via <see cref="EventTypes" />, <see cref="SourceTypes" />, and <see cref="Predicate" />.
///         When all scope properties are <c>null</c> or empty, the filter applies to every event (registration-wide global).
///         Setting any scope property narrows the filter to only events that satisfy it.
///     </para>
///     <para>
///         Register via <c>SoraService.UseEventPreFilter(...)</c>.
///     </para>
/// </remarks>
public interface IEventPreFilter
{
    /// <summary>
    ///     Filter priority. Lower values execute first.
    /// </summary>
    int Order => 0;

    /// <summary>
    ///     Event-type scope. When <c>null</c> or empty, the filter applies to all event types.
    ///     Otherwise the filter applies only when the event is an instance of one of these types
    ///     (base classes match derived events).
    /// </summary>
    Type[]? EventTypes => null;

    /// <summary>
    ///     Message-source scope. When <c>null</c> or empty, no source-type constraint is applied.
    ///     When non-empty, the filter applies only to <see cref="MessageReceivedEvent" /> whose
    ///     <c>Message.SourceType</c> is in this set; **all non-message events are treated as a scope mismatch**
    ///     (the filter is skipped).
    /// </summary>
    MessageSourceType[]? SourceTypes => null;

    /// <summary>
    ///     Predicate scope. When <c>null</c>, no predicate constraint is applied.
    ///     When non-null, the filter applies only when the predicate returns <c>true</c>.
    ///     Predicate exceptions are caught, logged at warning level, and treated as a scope mismatch
    ///     (the filter is skipped — fail-safe). Implement fail-closed semantics inside the predicate
    ///     itself if needed for security-sensitive filters.
    /// </summary>
    Func<BotEvent, bool>? Predicate => null;

    /// <summary>
    ///     Called when an event arrives. Return <c>false</c> to short-circuit the event chain
    ///     (Command Manager and EventDispatcher will not execute).
    ///     Access pipeline context via <c>e.PipelineContext</c>.
    /// </summary>
    /// <param name="e">The bot event to filter.</param>
    /// <param name="ct">
    ///     Pipeline cancellation token. Throw with this token when it is canceled to stop the pipeline.
    ///     Other cancellation exceptions are logged and isolated; if this token is also canceled,
    ///     the pipeline then raises its own cancellation.
    /// </param>
    /// <returns><c>true</c> to continue processing; <c>false</c> to block the event.</returns>
    ValueTask<bool> OnEventAsync(BotEvent e, CancellationToken ct);
}