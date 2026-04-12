namespace Sora.Adapter.OneBot11.Events;

/// <summary>Raised when a flash file starts downloading. OB11-specific.</summary>
public sealed record FlashFileDownloadingEvent : BotEvent
{
    /// <summary>File set identifier.</summary>
    public string FileSetId { get; internal init; } = "";

    /// <summary>Title of the flash file.</summary>
    public string Title { get; internal init; } = "";

    /// <summary>Scene type of the flash file operation.</summary>
    public int SceneType { get; internal init; }
}

/// <summary>Raised when a flash file has been downloaded. OB11-specific.</summary>
public sealed record FlashFileDownloadedEvent : BotEvent
{
    /// <summary>File set identifier.</summary>
    public string FileSetId { get; internal init; } = "";

    /// <summary>Title of the flash file.</summary>
    public string Title { get; internal init; } = "";

    /// <summary>Scene type of the flash file operation.</summary>
    public int SceneType { get; internal init; }

    /// <summary>URL of the downloaded file.</summary>
    public string FileUrl { get; internal init; } = "";
}

/// <summary>Raised when a flash file starts uploading. OB11-specific.</summary>
public sealed record FlashFileUploadingEvent : BotEvent
{
    /// <summary>File set identifier.</summary>
    public string FileSetId { get; internal init; } = "";

    /// <summary>Title of the flash file.</summary>
    public string Title { get; internal init; } = "";

    /// <summary>Scene type of the flash file operation.</summary>
    public int SceneType { get; internal init; }
}

/// <summary>Raised when a flash file has been uploaded. OB11-specific.</summary>
public sealed record FlashFileUploadedEvent : BotEvent
{
    /// <summary>File set identifier.</summary>
    public string FileSetId { get; internal init; } = "";

    /// <summary>Title of the flash file.</summary>
    public string Title { get; internal init; } = "";

    /// <summary>Scene type of the flash file operation.</summary>
    public int SceneType { get; internal init; }
}