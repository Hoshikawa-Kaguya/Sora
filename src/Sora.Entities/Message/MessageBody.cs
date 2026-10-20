using System.Collections;

namespace Sora.Entities.Message;

/// <summary>
///     Ordered collection of message segments forming a complete message.
/// </summary>
public sealed class MessageBody : IList<Segment>, IEquatable<MessageBody>
{
    private static ILogger? _logger;

    private readonly List<Segment> _segments = [];

#region Internal Members

    /// <summary>Logger for dropped segments, obtained on first use and replaceable in tests.</summary>
    internal static ILogger Logger
    {
        get => _logger ??= SoraLogger.CreateLogger(typeof(MessageBody).FullName!);
        set => _logger = value;
    }

    /// <summary>
    ///     Creates a MessageBody from received segments.
    ///     Used by adapter converters when constructing bodies from incoming protocol data.
    /// </summary>
    /// <param name="segments">The incoming segments.</param>
    /// <returns>A new <see cref="MessageBody" /> containing all provided segments.</returns>
    internal static MessageBody FromIncoming(IEnumerable<Segment> segments) => new(segments);

#endregion

#region Constructors

    /// <summary>Creates an empty message body.</summary>
    public MessageBody()
    {
    }

    /// <summary>Creates a message body from a single text string.</summary>
    /// <param name="text">The text content.</param>
    public MessageBody(string text)
    {
        _segments.Add(new TextSegment { Text = text });
    }

    /// <summary>Creates a message body from a single segment.</summary>
    /// <param name="segment">The segment to add.</param>
    public MessageBody(Segment segment)
    {
        _segments.Add(segment);
    }

    /// <summary>Creates a message body from a collection of segments.</summary>
    /// <param name="segments">The segments to add.</param>
    public MessageBody(IEnumerable<Segment> segments)
    {
        _segments.AddRange(segments);
    }

#endregion

#region Indexer & Collection Properties

    /// <inheritdoc />
    public Segment this[int index]
    {
        get => _segments[index];
        set => _segments[index] = value;
    }

    // IList<Segment> implementation
    /// <inheritdoc />
    public int Count => _segments.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

#endregion

#region IList<Segment> Implementation

    /// <inheritdoc />
    public void Add(Segment item) => _segments.Add(item);

    /// <inheritdoc />
    public void Clear() => _segments.Clear();

    /// <inheritdoc />
    public bool Contains(Segment item) => _segments.Contains(item);

    /// <inheritdoc />
    public void CopyTo(Segment[] array, int arrayIndex) => _segments.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<Segment> GetEnumerator() => _segments.GetEnumerator();

    /// <inheritdoc />
    public int IndexOf(Segment item) => _segments.IndexOf(item);

    /// <inheritdoc />
    public void Insert(int index, Segment item) => _segments.Insert(index, item);

    /// <inheritdoc />
    public bool Remove(Segment item) => _segments.Remove(item);

    /// <inheritdoc />
    public void RemoveAt(int index) => _segments.RemoveAt(index);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#endregion

#region Fluent Add Methods

    /// <summary>Adds an audio/voice segment for sending.</summary>
    /// <param name="fileUri">Audio URI (file://, http(s)://, or base64://).</param>
    /// <returns>This <see cref="MessageBody" /> for fluent chaining.</returns>
    public MessageBody AddAudio(string fileUri)
    {
        _segments.Add(new AudioSegment { FileUri = fileUri });
        return this;
    }

    /// <summary>Adds a face emoji segment.</summary>
    /// <param name="faceId">Face emoji identifier.</param>
    /// <param name="isLarge">Whether this is a super/large face.</param>
    /// <returns>This <see cref="MessageBody" /> for fluent chaining.</returns>
    public MessageBody AddFace(string faceId, bool isLarge = false)
    {
        _segments.Add(new FaceSegment { FaceId = faceId, IsLarge = isLarge });
        return this;
    }

    /// <summary>Adds an image segment for sending.</summary>
    /// <param name="fileUri">Image URI (file://, http(s)://, or base64://).</param>
    /// <param name="subType">Image sub-type.</param>
    /// <returns>This <see cref="MessageBody" /> for fluent chaining.</returns>
    public MessageBody AddImage(string fileUri, ImageSubType subType = ImageSubType.Normal)
    {
        _segments.Add(new ImageSegment { FileUri = fileUri, SubType = subType });
        return this;
    }

    /// <summary>Adds a light app segment for sending.</summary>
    /// <param name="appName">App name.</param>
    /// <param name="jsonPayload">JSON payload string.</param>
    /// <returns>This <see cref="MessageBody" /> for fluent chaining.</returns>
    public MessageBody AddLightApp(string appName, string jsonPayload)
    {
        _segments.Add(new LightAppSegment { AppName = appName, JsonPayload = jsonPayload });
        return this;
    }

    /// <summary>Adds a mention (@someone) segment.</summary>
    /// <param name="target">The user ID to mention.</param>
    /// <returns>This <see cref="MessageBody" /> for fluent chaining.</returns>
    public MessageBody AddMention(UserId target)
    {
        _segments.Add(new MentionSegment { Target = target });
        return this;
    }

    /// <summary>Adds a mention all (@all) segment.</summary>
    /// <returns>This <see cref="MessageBody" /> for fluent chaining.</returns>
    public MessageBody AddMentionAll()
    {
        _segments.Add(new MentionAllSegment());
        return this;
    }

    /// <summary>Adds a reply segment.</summary>
    /// <param name="targetId">The message ID being replied to.</param>
    /// <returns>This <see cref="MessageBody" /> for fluent chaining.</returns>
    public MessageBody AddReply(MessageId targetId)
    {
        _segments.Add(new ReplySegment { TargetId = targetId });
        return this;
    }

    /// <summary>Adds a text segment.</summary>
    /// <param name="text">The text content.</param>
    /// <returns>This <see cref="MessageBody" /> for fluent chaining.</returns>
    public MessageBody AddText(string text)
    {
        _segments.Add(new TextSegment { Text = text });
        return this;
    }

    /// <summary>Adds a video segment for sending.</summary>
    /// <param name="fileUri">Video URI (file://, http(s)://, or base64://).</param>
    /// <param name="thumbUri">Optional thumbnail image URI.</param>
    /// <returns>This <see cref="MessageBody" /> for fluent chaining.</returns>
    public MessageBody AddVideo(string fileUri, string thumbUri = "")
    {
        _segments.Add(new VideoSegment { FileUri = fileUri, ThumbUri = thumbUri });
        return this;
    }

#endregion

#region Conversion

    /// <summary>Gets all segments of the specified type.</summary>
    /// <typeparam name="T">The segment type to filter by.</typeparam>
    /// <returns>An enumerable of all matching segments.</returns>
    public IEnumerable<T> GetAll<T>() where T : Segment => _segments.OfType<T>();

    /// <summary>Gets the first segment of the specified type, or default.</summary>
    /// <typeparam name="T">The segment type to search for.</typeparam>
    /// <returns>The first matching segment, or null if none found.</returns>
    public T? GetFirst<T>() where T : Segment => _segments.OfType<T>().FirstOrDefault();

    /// <summary>
    ///     Extracts all plain text content from this message, concatenated.
    /// </summary>
    /// <returns>All text content joined together.</returns>
    public string GetText() => string.Concat(_segments.OfType<TextSegment>().Select(t => t.Text));

    /// <summary>
    ///     Returns true if this message body satisfies the composition rules for sending.
    /// </summary>
    /// <returns>True if no validation issues exist; false otherwise.</returns>
    public bool IsValidForSending() => Validate().Count == 0;

    /// <summary>Creates a new body with the outgoing form of each segment, dropping unsendable ones with a warning.</summary>
    /// <returns>A new <see cref="MessageBody" /> with only sendable segments.</returns>
    public MessageBody ToOutgoing()
    {
        MessageBody outgoing = new();
        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i].ToOutgoing() is { } converted)
            {
                outgoing._segments.Add(converted);
                continue;
            }

            Logger.LogWarning(
                "Segment {SegmentType} at index {Index} cannot be sent and was dropped",
                _segments[i].Type,
                i);
        }

        return outgoing;
    }

    /// <summary>Validates the segment composition rules of this message body for sending.</summary>
    /// <returns>A list of validation issue descriptions; empty if valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        List<string> issues = [];
        if (_segments.Count <= 1) return issues;

        foreach (Segment seg in _segments)
            switch (seg)
            {
                case AudioSegment:
                    issues.Add("Audio (record) segment must be sent alone");
                    break;
                case VideoSegment:
                    issues.Add("Video segment must be sent alone");
                    break;
                case ForwardSegment:
                    issues.Add("Forward segment must be sent alone");
                    break;
                case FaceSegment { IsLarge: true }:
                    issues.Add("Large face (super emoji) must be sent alone");
                    break;
            }

        return issues;
    }

#endregion

#region Equality

    /// <summary>Determines whether another body contains equal segments in the same order.</summary>
    /// <param name="other">The body to compare with.</param>
    /// <returns>True if both bodies contain equal segments in the same order.</returns>
    public bool Equals(MessageBody? other) =>
        other is not null && (ReferenceEquals(this, other) || _segments.SequenceEqual(other._segments));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MessageBody other && Equals(other);

    /// <summary>Returns a hash code of the current segments, which changes when the body is modified.</summary>
    /// <returns>The hash code of the current content.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (Segment segment in _segments) hash.Add(segment);
        return hash.ToHashCode();
    }

    /// <summary>Determines whether two bodies contain equal segments in the same order.</summary>
    /// <param name="left">The first body.</param>
    /// <param name="right">The second body.</param>
    /// <returns>True if both are null or contain equal segments in the same order.</returns>
    public static bool operator ==(MessageBody? left, MessageBody? right) => left?.Equals(right) ?? right is null;

    /// <summary>Determines whether two bodies differ in content.</summary>
    /// <param name="left">The first body.</param>
    /// <param name="right">The second body.</param>
    /// <returns>True if the bodies do not contain equal segments in the same order.</returns>
    public static bool operator !=(MessageBody? left, MessageBody? right) => !(left == right);

#endregion

#region Operators

    /// <summary>Appends a segment to a message body, returning a new MessageBody.</summary>
    /// <param name="body">The source message body.</param>
    /// <param name="segment">The segment to append.</param>
    /// <returns>A new <see cref="MessageBody" /> with the segment appended.</returns>
    public static MessageBody operator +(MessageBody body, Segment segment) => new([.. body, segment]);

    /// <summary>Prepends a segment to a message body, returning a new MessageBody.</summary>
    /// <param name="segment">The segment to prepend.</param>
    /// <param name="body">The source message body.</param>
    /// <returns>A new <see cref="MessageBody" /> with the segment prepended.</returns>
    public static MessageBody operator +(Segment segment, MessageBody body) => new([segment, .. body]);

    /// <summary>Appends a text string to a message body, returning a new MessageBody.</summary>
    /// <param name="body">The source message body.</param>
    /// <param name="text">The text to append.</param>
    /// <returns>A new <see cref="MessageBody" /> with a text segment appended.</returns>
    public static MessageBody operator +(MessageBody body, string text) =>
        new([.. body, new TextSegment { Text = text }]);

    /// <summary>Prepends a text string to a message body, returning a new MessageBody.</summary>
    /// <param name="text">The text to prepend.</param>
    /// <param name="body">The source message body.</param>
    /// <returns>A new <see cref="MessageBody" /> with a text segment prepended.</returns>
    public static MessageBody operator +(string text, MessageBody body) =>
        new([new TextSegment { Text = text }, .. body]);

    /// <summary>Concatenates two message bodies, returning a new MessageBody.</summary>
    /// <param name="left">The first message body.</param>
    /// <param name="right">The second message body.</param>
    /// <returns>A new <see cref="MessageBody" /> containing all segments from both.</returns>
    public static MessageBody operator +(MessageBody left, MessageBody right) => new([.. left, .. right]);

    /// <summary>Implicitly converts a string to a text-only MessageBody.</summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>A new <see cref="MessageBody" /> containing a single text segment.</returns>
    public static implicit operator MessageBody(string text) => new(text);

    /// <summary>Implicitly converts a single segment to a MessageBody.</summary>
    /// <param name="segment">The segment to convert.</param>
    /// <returns>A new <see cref="MessageBody" /> containing the segment.</returns>
    public static implicit operator MessageBody(Segment segment) => new(segment);

#endregion
}