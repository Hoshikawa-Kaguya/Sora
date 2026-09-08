using System.Reflection;

namespace Sora.Command.Filters;

/// <summary>
///     Read-only snapshot of command metadata provided to filters.
///     Created internally by <see cref="CommandManager" /> when invoking filter chains.
/// </summary>
public sealed class CommandFilterContext
{
    /// <summary>The command method that matched.</summary>
    public required MethodInfo Method { get; init; }

    /// <summary>The match expressions registered for this command.</summary>
    public required IReadOnlyList<string> Expressions { get; init; }

    /// <summary>The match type used for this command.</summary>
    public required MatchType MatchType { get; init; }

    /// <summary>The declaring type of the command method.</summary>
    public required Type DeclaringType { get; init; }

    /// <summary>
    ///     All custom attributes on the command method and its declaring type (cached).
    /// </summary>
    public required IReadOnlyList<Attribute> Attributes { get; init; }

    /// <summary>The command description (from <see cref="CommandAttribute.Description" />).</summary>
    public required string Description { get; init; }

    /// <summary>Whether the command blocks the event chain after matching.</summary>
    public required bool BlockAfterMatch { get; init; }
}