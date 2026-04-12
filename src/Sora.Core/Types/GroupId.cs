namespace Sora.Core.Types;

/// <summary>
///     Strongly-typed group/channel identifier. Wraps a <see cref="long" /> value with zero allocation.
/// </summary>
public readonly record struct GroupId(long Value) : IComparable<GroupId>
{
    /// <inheritdoc />
    public int CompareTo(GroupId other) => Value.CompareTo(other.Value);

    /// <summary>Implicitly converts a <see cref="long" /> to a <see cref="GroupId" />.</summary>
    public static implicit operator GroupId(long v) => new(v);

    /// <summary>Implicitly converts a <see cref="GroupId" /> to a <see cref="long" />.</summary>
    public static implicit operator long(GroupId id) => id.Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}