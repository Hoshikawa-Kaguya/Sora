namespace Sora.Core.Types;

/// <summary>
///     Strongly-typed user/account identifier. Wraps a <see cref="long" /> value with zero allocation.
/// </summary>
public readonly record struct UserId(long Value) : IComparable<UserId>
{
    /// <inheritdoc />
    public int CompareTo(UserId other) => Value.CompareTo(other.Value);

    /// <summary>Implicitly converts a <see cref="long" /> to a <see cref="UserId" />.</summary>
    public static implicit operator UserId(long v) => new(v);

    /// <summary>Implicitly converts a <see cref="UserId" /> to a <see cref="long" />.</summary>
    public static implicit operator long(UserId id) => id.Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}