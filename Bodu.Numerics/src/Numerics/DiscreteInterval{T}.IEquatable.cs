// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteInterval{T}.IEquatable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct DiscreteInterval<T> : IEquatable<DiscreteInterval<T>>
{
    /// <summary>
    /// Determines whether this interval equals <paramref name="other" /> as a set of integers. Because every interval is
    /// canonicalized to a single representation, equal sets compare equal by their stored fields.
    /// </summary>
    /// <param name="other">The interval to compare against.</param>
    /// <returns><see langword="true" /> when the two describe the same integer set; otherwise <see langword="false" />.</returns>
    public bool Equals(DiscreteInterval<T> other) =>
        _flags == other._flags && _first == other._first && _last == other._last;

    /// <summary>
    /// Determines whether this interval equals the boxed <paramref name="obj" />.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><see langword="true" /> when <paramref name="obj" /> is an equal <see cref="DiscreteInterval{T}" />; otherwise <see langword="false" />.</returns>
    public override bool Equals(object? obj) =>
        obj is DiscreteInterval<T> other && Equals(other);

    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(DiscreteInterval{T})" />.
    /// </summary>
    /// <returns>A 32-bit hash code.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(_first, _last, _flags);

    /// <summary>
    /// Determines whether two intervals are equal as sets.
    /// </summary>
    /// <param name="left">The first interval.</param>
    /// <param name="right">The second interval.</param>
    /// <returns><see langword="true" /> when equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(DiscreteInterval<T> left, DiscreteInterval<T> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two intervals are not equal as sets.
    /// </summary>
    /// <param name="left">The first interval.</param>
    /// <param name="right">The second interval.</param>
    /// <returns><see langword="true" /> when they differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(DiscreteInterval<T> left, DiscreteInterval<T> right) =>
        !left.Equals(right);
}
