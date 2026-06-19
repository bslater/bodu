// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval{T}.IEquatable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct Interval<T> : IEquatable<Interval<T>>
{
    /// <summary>
    /// Determines whether this interval equals <paramref name="other" /> as a set — same lower endpoint, same upper
    /// endpoint, and matching inclusivity on each side. Any two empty intervals are equal regardless of the bounds used
    /// to construct them.
    /// </summary>
    /// <param name="other">The interval to compare against.</param>
    /// <returns>
    /// <see langword="true" /> when the two intervals describe the same set of values; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool Equals(Interval<T> other)
    {
        return IsEmpty
            ? other.IsEmpty
            : !other.IsEmpty
                && _lower == other._lower
                && _upper == other._upper
                && _flags == other._flags;
    }

    /// <summary>
    /// Determines whether this interval equals the boxed <paramref name="obj" /> reference.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="obj" /> is an <see cref="Interval{T}" /> equal to this one;
    /// otherwise <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is Interval<T> other && Equals(other);

    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(Interval{T})" />. All empty intervals share the same hash;
    /// non-empty intervals hash on their endpoints and inclusivity flags.
    /// </summary>
    /// <returns>A 32-bit hash code suitable for use in hash-based collections.</returns>
    public override int GetHashCode() =>
        IsEmpty ? 0 : HashCode.Combine(_lower, _upper, _flags);

    /// <summary>
    /// Determines whether two intervals are equal as sets. See <see cref="Equals(Interval{T})" />.
    /// </summary>
    /// <param name="left">The first interval.</param>
    /// <param name="right">The second interval.</param>
    /// <returns><see langword="true" /> when the intervals are equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(Interval<T> left, Interval<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two intervals are not equal as sets. See <see cref="Equals(Interval{T})" />.
    /// </summary>
    /// <param name="left">The first interval.</param>
    /// <param name="right">The second interval.</param>
    /// <returns><see langword="true" /> when the intervals differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(Interval<T> left, Interval<T> right)
    {
        return !left.Equals(right);
    }
}
