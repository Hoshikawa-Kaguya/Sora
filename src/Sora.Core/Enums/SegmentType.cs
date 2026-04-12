namespace Sora.Core.Enums;

/// <summary>
///     Discriminator for message segment types.
/// </summary>
public enum SegmentType
{
    /// <summary>Plain text.</summary>
    Text,

    /// <summary>Image.</summary>
    Image,

    /// <summary>Mention a specific user (@someone).</summary>
    Mention,

    /// <summary>Mention all members (@all).</summary>
    MentionAll,

    /// <summary>Reply to a message.</summary>
    Reply,

    /// <summary>QQ face emoji.</summary>
    Face,

    /// <summary>Audio/voice message.</summary>
    Audio,

    /// <summary>Video message.</summary>
    Video,

    /// <summary>File attachment.</summary>
    File,

    /// <summary>Merged forward message.</summary>
    Forward,

    /// <summary>Market face (QQ store emoji).</summary>
    MarketFace,

    /// <summary>Light app / mini program.</summary>
    LightApp,

    /// <summary>XML rich message.</summary>
    Xml
}