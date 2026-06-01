// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.IEquatable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct Fraction<T> :
    IEquatable<Fraction<T>>
{
    /// <summary>
    /// Determines whether the specified object is a <see cref="Fraction{T}" /> equal to this value.
    /// </summary>
    /// <param name="obj">The object to compare with this value.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="Fraction{T}" /> with the same canonical
    /// value; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is Fraction<T> other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="Fraction{T}" /> is equal to this value.
    /// </summary>
    /// <param name="other">The value to compare with this value.</param>
    /// <returns>
    /// <see langword="true" /> if both values share the same canonical numerator and denominator; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool Equals(Fraction<T> other) =>
        _numerator == other._numerator && Denominator == other.Denominator;

    /// <summary>
    /// Returns a hash code for this rational value.
    /// </summary>
    /// <returns>A hash code derived from the canonical numerator and denominator.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(_numerator, Denominator);
}
