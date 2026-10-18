namespace Sora.Command.InternalEntities;

/// <summary>
///     Immutable execution segment containing the ordered, effectively enabled commands of one assembly.
/// </summary>
internal sealed class AssemblySegment
{
    /// <summary>The assembly registration that owns the commands.</summary>
    public required AssemblyRegistration Registration { get; init; }

    /// <summary>Commands ordered by descending priority and ascending registration sequence.</summary>
    public required CommandInfo[] Commands { get; init; }
}