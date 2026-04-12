namespace Sora.Entities.Message;

/// <summary>Static factory methods for creating outgoing segments.</summary>
public static class SegmentBuilder
{
#region Text & Emoji

    /// <summary>Creates a text segment.</summary>
    /// <param name="text">The text content.</param>
    /// <returns>A new <see cref="TextSegment" />.</returns>
    public static TextSegment Text(string text) => new() { Text = text };

    /// <summary>Creates a face segment.</summary>
    /// <param name="faceId">Face emoji identifier.</param>
    /// <param name="isLarge">Whether this is a super/large face.</param>
    /// <returns>A new <see cref="FaceSegment" />.</returns>
    public static FaceSegment Face(string faceId, bool isLarge = false) => new() { FaceId = faceId, IsLarge = isLarge };

#endregion

#region Media

    /// <summary>Creates an image segment for sending.</summary>
    /// <param name="fileUri">Image URI (file://, http(s)://, or base64://).</param>
    /// <param name="subType">Image sub-type.</param>
    /// <returns>A new <see cref="ImageSegment" />.</returns>
    public static ImageSegment Image(string fileUri, ImageSubType subType = ImageSubType.Normal) =>
        new() { FileUri = fileUri, SubType = subType };

    /// <summary>Creates an audio segment for sending.</summary>
    /// <param name="fileUri">Audio URI (file://, http(s)://, or base64://).</param>
    /// <returns>A new <see cref="AudioSegment" />.</returns>
    public static AudioSegment Audio(string fileUri) => new() { FileUri = fileUri };

    /// <summary>Creates a video segment for sending.</summary>
    /// <param name="fileUri">Video URI (file://, http(s)://, or base64://).</param>
    /// <param name="thumbUri">Optional thumbnail image URI.</param>
    /// <returns>A new <see cref="VideoSegment" />.</returns>
    public static VideoSegment Video(string fileUri, string thumbUri = "") => new() { FileUri = fileUri, ThumbUri = thumbUri };

#endregion

#region Structured

    /// <summary>Creates a forward segment for sending with nested messages.</summary>
    /// <param name="messages">The nested messages to forward.</param>
    /// <param name="title">Optional custom title.</param>
    /// <param name="preview">Optional preview text lines. 1-4 lines. skip after 4 line.</param>
    /// <param name="summary">Optional custom summary text.</param>
    /// <param name="prompt">Optional preview prompt text for mobile QQ.</param>
    /// <returns>A new <see cref="ForwardSegment" />.</returns>
    public static ForwardSegment Forward(
        IReadOnlyList<ForwardedMessageNode> messages,
        string                              title   = "",
        string[]?                           preview = null,
        string                              summary = "",
        string                              prompt  = "") =>
        new() { Messages = messages, Title = title, Preview = preview?.Take(4).ToList() ?? [], Summary = summary, Prompt = prompt };

    /// <summary>Creates a light app segment for sending.</summary>
    /// <param name="appName">App name.</param>
    /// <param name="jsonPayload">JSON payload string.</param>
    /// <returns>A new <see cref="LightAppSegment" />.</returns>
    public static LightAppSegment LightApp(string appName, string jsonPayload) =>
        new() { AppName = appName, JsonPayload = jsonPayload };

#endregion

#region Targeting

    /// <summary>Creates a mention segment.</summary>
    /// <param name="target">The user ID to mention.</param>
    /// <returns>A new <see cref="MentionSegment" />.</returns>
    public static MentionSegment Mention(UserId target) => new() { Target = target };

    /// <summary>Creates a mention all segment.</summary>
    /// <returns>A new <see cref="MentionAllSegment" />.</returns>
    public static MentionAllSegment MentionAll() => new();

    /// <summary>Creates a reply segment.</summary>
    /// <param name="targetId">The message ID being replied to.</param>
    /// <returns>A new <see cref="ReplySegment" />.</returns>
    public static ReplySegment Reply(MessageId targetId) => new() { TargetId = targetId };

#endregion
}