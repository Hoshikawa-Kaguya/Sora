namespace Sora.Command;

/// <summary>
///     Immutable snapshot of one command owner type inside an assembly registration.
/// </summary>
public sealed class CommandTypeState
{
    /// <summary>The owner type.</summary>
    public required Type Type { get; init; }

    /// <summary>The type's local enabled state, independent of its assembly.</summary>
    public required bool IsEnabled { get; init; }

    /// <summary>Whether the type's commands can match: both the assembly and the type are enabled.</summary>
    public required bool IsEffectivelyEnabled { get; init; }

    /// <summary>The number of scanned and dynamic commands owned by the type.</summary>
    public required int CommandCount { get; init; }
}