namespace Sora.Command.InternalEntities;

/// <summary>
///     Immutable two-level command execution table. It is rebuilt when registrations change and read once per message.
/// </summary>
internal sealed class ExecutionTable
{
    /// <summary>An execution table without commands.</summary>
    public static ExecutionTable Empty { get; } = new() { Segments = [] };

    /// <summary>Assembly segments ordered by descending assembly priority and ascending registration sequence.</summary>
    public required AssemblySegment[] Segments { get; init; }
}