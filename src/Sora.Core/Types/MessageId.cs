namespace Sora.Core.Types;

/// <summary>
///     Strongly-typed message identifier. Wraps a <see cref="long" /> value with zero allocation.
/// </summary>
public readonly record struct MessageId(long Value) : IComparable<MessageId>
{
    /// <inheritdoc />
    public int CompareTo(MessageId other) => Value.CompareTo(other.Value);

    /// <summary>Implicitly converts a <see cref="long" /> to a <see cref="MessageId" />.</summary>
    public static implicit operator MessageId(long v) => new(v);

    /// <summary>Implicitly converts an <see cref="int" /> to a <see cref="MessageId" />.</summary>
    public static implicit operator MessageId(int v) => new(v);

    /// <summary>Implicitly converts a <see cref="MessageId" /> to a <see cref="long" />.</summary>
    public static implicit operator long(MessageId id) => id.Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}