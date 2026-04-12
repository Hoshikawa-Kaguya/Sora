namespace Sora.Entities.Info;

/// <summary>Protocol implementation information (Milky-specific).</summary>
public sealed record ImplInfo
{
    /// <summary>Implementation name.</summary>
    public string ImplName { get; internal init; } = "";

    /// <summary>Implementation version.</summary>
    public string ImplVersion { get; internal init; } = "";

    /// <summary>Implemented protocol version.</summary>
    public string ProtocolVersion { get; internal init; } = "";

    /// <summary>QQ protocol platform type.</summary>
    public string QqProtocolType { get; internal init; } = "";

    /// <summary>QQ protocol version used.</summary>
    public string QqProtocolVersion { get; internal init; } = "";
}