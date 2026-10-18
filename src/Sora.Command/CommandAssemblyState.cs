using System.Reflection;

namespace Sora.Command;

/// <summary>
///     Immutable snapshot of one assembly registration, returned by <see cref="CommandManager.GetAssemblyStates" />.
/// </summary>
public sealed class CommandAssemblyState
{
    /// <summary>The registered assembly.</summary>
    public required Assembly Assembly { get; init; }

    /// <summary>The assembly's local enabled state.</summary>
    public required bool IsEnabled { get; init; }

    /// <summary>The assembly priority. Higher priority assemblies execute first.</summary>
    public required int Priority { get; init; }

    /// <summary>Types owning commands in the assembly, ordered by their first command registration.</summary>
    public required IReadOnlyList<CommandTypeState> Types { get; init; }
}