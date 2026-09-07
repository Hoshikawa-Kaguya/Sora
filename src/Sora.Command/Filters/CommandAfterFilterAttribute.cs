namespace Sora.Command.Filters;

/// <summary>
///     Abstract base for command after-filters. Implementations are applied as attributes on
///     <c>[Command]</c> methods or <c>[CommandGroup]</c> classes; the framework only invokes a filter for
///     commands whose method or declaring type carries it (opt-in only — there is no global registration).
/// </summary>
/// <remarks>
///     <para>
///         Always executes after the command method returns, or after a before-filter short-circuits the chain.
///         Sort order: ascending <see cref="Order" />; ties resolved by declaration scope
///         (class-level attributes precede method-level), then by source declaration order.
///     </para>
///     <para>
///         The framework caches attribute instances at scan time, so a single class-level filter attribute
///         is shared across all commands in the same <c>[CommandGroup]</c>, and a single method-level filter
///         attribute is shared across all invocations of the same command. Mutable state inside the filter
///         (e.g., counters, cooldown dictionaries) <b>must</b> be thread-safe — multiple events can invoke
///         the filter concurrently.
///     </para>
///     <para>
///         Attribute constructors cannot accept runtime dependencies (logger, DB, services). Use
///         <c>SoraLogger.CreateLogger&lt;T&gt;()</c> or static singletons inside the filter for such needs.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public abstract class CommandAfterFilterAttribute : Attribute
{
    /// <summary>
    ///     Filter priority. Lower values execute first.
    ///     Sort order: ascending <see cref="Order" />; ties resolved by declaration scope
    ///     (class-level attributes precede method-level), then by source declaration order.
    /// </summary>
    public virtual int Order => 0;

    /// <summary>
    ///     Called after command execution (or short-circuit). Always executes.
    ///     Access pipeline context via <c>e.PipelineContext</c>.
    /// </summary>
    /// <param name="e">The message event that triggered the command.</param>
    /// <param name="cmd">Read-only metadata about the matched command.</param>
    /// <param name="shortCircuited"><c>true</c> if the command method was not invoked (blocked by a before-filter).</param>
    /// <param name="exception">The exception thrown by the command method, if any; <c>null</c> when short-circuited or successful.</param>
    /// <param name="ct">Cancellation token.</param>
    public abstract ValueTask OnAfterExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        bool                 shortCircuited,
        Exception?           exception,
        CancellationToken    ct);
}