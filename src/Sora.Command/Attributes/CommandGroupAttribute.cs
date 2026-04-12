namespace Sora.Command.Attributes;

/// <summary>
///     Groups commands in a class, optionally adding a shared prefix.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandGroupAttribute : Attribute
{
    /// <summary>Group name for organization.</summary>
    public string Name { get; set; } = "";

    /// <summary>Shared prefix prepended to all command expressions in this group.</summary>
    public string Prefix { get; set; } = "";
}