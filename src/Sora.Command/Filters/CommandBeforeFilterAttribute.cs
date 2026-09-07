namespace Sora.Command.Filters;

/// <summary>
///     Abstract base for command before-filters. Implementations are applied as attributes on
///     <c>[Command]</c> methods or <c>[CommandGroup]</c> classes; the framework only invokes a filter for
///     commands whose method or declaring type carries it (opt-in only — there is no global registration).
/// </summary>
/// <remarks>
///     <para>
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
public abstract class CommandBeforeFilterAttribute : Attribute
{
    /// <summary>
    ///     Filter priority. Lower values execute first.
    ///     Sort order: ascending <see cref="Order" />; ties resolved by declaration scope
    ///     (class-level attributes precede method-level), then by source declaration order.
    /// </summary>
    public virtual int Order => 0;

    /// <summary>
    ///     Called before command execution. Return <c>false</c> to short-circuit execution
    ///     (the command method will not be invoked, but after-filters still execute).
    ///     Access pipeline context via <c>e.PipelineContext</c>.
    /// </summary>
    /// <param name="e">The message event that triggered the command.</param>
    /// <param name="cmd">Read-only metadata about the matched command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> to proceed with execution; <c>false</c> to block.</returns>
    public abstract ValueTask<bool> OnBeforeExecuteAsync(
        MessageReceivedEvent e,
        CommandFilterContext cmd,
        CancellationToken    ct);
}