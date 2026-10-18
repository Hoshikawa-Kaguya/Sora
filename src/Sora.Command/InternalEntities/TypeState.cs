namespace Sora.Command.InternalEntities;

/// <summary>
///     Type-level command state inside one assembly registration.
/// </summary>
internal sealed class TypeState
{
    /// <summary>Local enabled state of the type.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Registration sequence of the first command owned by the type.</summary>
    public required long FirstSequence { get; init; }
}