using System.Collections;

namespace Sora.Entities.Message;

/// <summary>
///     Ordered collection of message segments forming a complete message.
/// </summary>
public sealed class MessageBody : IList<Segment>
{
    private readonly List<Segment> _segments = [];

#region Internal Methods

    /// <summary>
    ///     Creates a MessageBody from received segments without direction validation.
    ///     Used by adapter converters when constructing bodies from incoming protocol data.
    /// </summary>
    /// <param name="segments">The incoming segments.</param>
    /// <returns>A new <see cref="MessageBody" /> containing all provided segments.</returns>
    internal static MessageBody FromIncoming(IEnumerable<Segment> segments)
    {
        MessageBody body = [];
        body._segments.AddRange(segments);
        return body;
    }

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

    /// <summary>Creates a message body from a single segment. Rejects incoming-only segments.</summary>
    /// <param name="segment">The segment to add.</param>
    public MessageBody(Segment segment)
    {
        Add(segment);
    }

    /// <summary>Creates a message body from a collection of segments. Rejects incoming-only segments.</summary>
    /// <param name="segments">The segments to add.</param>
    public MessageBody(IEnumerable<Segment> segments)
    {
        _segments.AddRange(segments.Select(s => s.ToOutgoing()).OfType<Segment>());
    }

#endregion

#region Indexer & Collection Properties

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when setting an incoming-only segment.</exception>
    public Segment this[int index]
    {
        get => _segments[index];
        set
        {
            if (value.ToOutgoing() is not { } outgoing)
                throw new InvalidOperationException($"Cannot set incoming-only segment {value.Type} in a sendable MessageBody.");
            _segments[index] = outgoing;
        }
    }

    // IList<Segment> implementation
    /// <inheritdoc />
    public int Count => _segments.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

#endregion

#region IList<Segment> Implementation

    /// <summary> Add a segment to the message body. Skip incoming-only segments. </summary>
    /// <param name="item">segment</param>
    public void Add(Segment item)
    {
        if (item.ToOutgoing() is { } outgoing) _segments.Add(outgoing);
    }

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

    /// <summary> Insert a segment to the message body. Skip incoming-only segments. </summary>
    /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
    /// <param name="item">segment</param>
    public void Insert(int index, Segment item)
    {
        if (item.ToOutgoing() is { } outgoing) _segments.Insert(index, outgoing);
    }

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
    ///     Returns true if this message body is valid for sending.
    /// </summary>
    /// <returns>True if no validation issues exist; false otherwise.</returns>
    public bool IsValidForSending() => Validate().Count == 0;

    /// <summary>
    ///     Creates a new MessageBody containing only outgoing-compatible segments.
    ///     Segments that cannot be converted are silently skipped.
    /// </summary>
    /// <returns>A new <see cref="MessageBody" /> with only sendable segments.</returns>
    public MessageBody ToOutgoing() => new(_segments.Select(s => s.ToOutgoing()).OfType<Segment>());

    /// <summary>
    ///     Validates this message body for sending. Returns a list of issues found.
    /// </summary>
    /// <returns>A list of validation issue descriptions; empty if valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        List<string> issues =
            [
                .._segments.Where(s => s.Direction == SegmentDirection.Incoming)
                           .Select(s => $"Segment type {s.Type} is incoming-only and cannot be sent")
            ];

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

#region Operators

    /// <summary>Appends a segment to a message body, returning a new MessageBody.</summary>
    /// <param name="body">The source message body.</param>
    /// <param name="segment">The segment to append.</param>
    /// <returns>A new <see cref="MessageBody" /> with the segment appended.</returns>
    public static MessageBody operator +(MessageBody body, Segment segment) => new([..body, segment]);

    /// <summary>Prepends a segment to a message body, returning a new MessageBody.</summary>
    /// <param name="segment">The segment to prepend.</param>
    /// <param name="body">The source message body.</param>
    /// <returns>A new <see cref="MessageBody" /> with the segment prepended.</returns>
    public static MessageBody operator +(Segment segment, MessageBody body) => new([segment, ..body]);

    /// <summary>Appends a text string to a message body, returning a new MessageBody.</summary>
    /// <param name="body">The source message body.</param>
    /// <param name="text">The text to append.</param>
    /// <returns>A new <see cref="MessageBody" /> with a text segment appended.</returns>
    public static MessageBody operator +(MessageBody body, string text) => new([..body, new TextSegment { Text = text }]);

    /// <summary>Prepends a text string to a message body, returning a new MessageBody.</summary>
    /// <param name="text">The text to prepend.</param>
    /// <param name="body">The source message body.</param>
    /// <returns>A new <see cref="MessageBody" /> with a text segment prepended.</returns>
    public static MessageBody operator +(string text, MessageBody body) => new([new TextSegment { Text = text }, ..body]);

    /// <summary>Concatenates two message bodies, returning a new MessageBody.</summary>
    /// <param name="left">The first message body.</param>
    /// <param name="right">The second message body.</param>
    /// <returns>A new <see cref="MessageBody" /> containing all segments from both.</returns>
    public static MessageBody operator +(MessageBody left, MessageBody right) => new([..left, ..right]);

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