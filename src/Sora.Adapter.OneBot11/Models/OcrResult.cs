namespace Sora.Adapter.OneBot11.Models;

/// <summary>Result of an OCR (text recognition) operation. OB11-specific.</summary>
public sealed record OcrResult
{
    /// <summary>Detected language code.</summary>
    public string Language { get; init; } = "";

    /// <summary>Recognized text regions.</summary>
    public IReadOnlyList<OcrTextDetection> Texts { get; init; } = [];
}

/// <summary>A single text detection result from OCR.</summary>
public sealed record OcrTextDetection
{
    /// <summary>Recognized text content.</summary>
    public string Text { get; init; } = "";

    /// <summary>Recognition confidence (0-100).</summary>
    public int Confidence { get; init; }
}