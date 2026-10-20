namespace Sora.Entities.Message;

/// <summary>Immutable snapshot of received message content, exposed publicly only as editable copies.</summary>
internal sealed class ReceivedMessageBody : IEquatable<ReceivedMessageBody>
{
    private readonly Segment[] _segments;

    private ReceivedMessageBody(Segment[] segments)
    {
        _segments = segments;
    }

#region Factories

    /// <summary>Gets the empty snapshot.</summary>
    internal static ReceivedMessageBody Empty { get; } = new([]);

    /// <summary>Creates a snapshot holding copies of the specified segments.</summary>
    /// <param name="segments">The source segments; later changes to them do not affect the snapshot.</param>
    /// <returns>A new snapshot.</returns>
    internal static ReceivedMessageBody From(IEnumerable<Segment> segments) =>
        new([.. segments.Select(s => s with { })]);

#endregion

#region Read Access

    /// <summary>Gets the number of segments.</summary>
    internal int Count => _segments.Length;

    /// <summary>Gets the snapshot segments for framework-internal reads. Callers must not modify them.</summary>
    internal IReadOnlyList<Segment> Segments => _segments;

    /// <summary>Extracts all plain text content, concatenated.</summary>
    /// <returns>All text content joined together.</returns>
    internal string GetText() => string.Concat(_segments.OfType<TextSegment>().Select(t => t.Text));

    /// <summary>Creates an independent editable copy containing every segment, including incoming-only ones.</summary>
    /// <returns>A new <see cref="MessageBody" /> whose changes never affect this snapshot.</returns>
    internal MessageBody ToEditable() => [.. _segments.Select(s => s with { })];

#endregion

#region Equality

    /// <inheritdoc />
    public bool Equals(ReceivedMessageBody? other) =>
        other is not null && (ReferenceEquals(this, other) || _segments.SequenceEqual(other._segments));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ReceivedMessageBody other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (Segment segment in _segments) hash.Add(segment);
        return hash.ToHashCode();
    }

#endregion
}
