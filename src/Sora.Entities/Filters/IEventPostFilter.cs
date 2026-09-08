namespace Sora.Entities.Filters;

/// <summary>
///     Event post-filter. Runs after normal event processing or short-circuit.
///     Sora cancellation immediately terminates the pipeline, including any remaining post-filters.
/// </summary>
/// <remarks>
///     <para>
///         Scope is opt-in via <see cref="EventTypes" />, <see cref="SourceTypes" />, and <see cref="Predicate" />.
///         When all scope properties are <c>null</c> or empty, the filter applies to every event (registration-wide global).
///         Setting any scope property narrows the filter to only events that satisfy it.
///     </para>
///     <para>
///         Register via <c>SoraService.UseEventPostFilter(...)</c>.
///     </para>
/// </remarks>
public interface IEventPostFilter
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
    ///     Called after event processing completes or short-circuits, unless Sora cancellation has terminated execution.
    ///     Access pipeline context via <c>e.PipelineContext</c>.
    /// </summary>
    /// <param name="e">The bot event that was processed.</param>
    /// <param name="chainCompleted">
    ///     <c>true</c> if <see cref="EventDispatcher.DispatchAsync" /> returned normally;
    ///     <c>false</c> if the earlier chain was short-circuited before that point.
    /// </param>
    /// <param name="ct">
    ///     Pipeline cancellation token. Cancellation carrying this token is propagated immediately when it is canceled.
    ///     Other cancellation exceptions are logged and isolated; if this token is also canceled,
    ///     the stage stops and the pipeline raises its own cancellation instead.
    /// </param>
    ValueTask OnEventProcessedAsync(BotEvent e, bool chainCompleted, CancellationToken ct);
}
