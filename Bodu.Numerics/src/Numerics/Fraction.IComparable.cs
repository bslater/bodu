// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.IComparable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct Fraction<T> :
    IComparable,
    IComparable<Fraction<T>>
{
    /// <summary>
    /// Compares this rational value with another and indicates their relative order.
    /// </summary>
    /// <param name="other">The value to compare with this value.</param>
    /// <returns>
    /// A negative number if this value precedes <paramref name="other" />, zero if they are equal, and a positive
    /// number if this value follows <paramref name="other" />.
    /// </returns>
    public int CompareTo(Fraction<T> other) =>
        Compare(this, other);

    /// <summary>
    /// Compares this rational value with the specified object and indicates their relative order.
    /// </summary>
    /// <param name="obj">The object to compare with this value.</param>
    /// <returns>
    /// A negative number if this value precedes <paramref name="obj" />, zero if they are equal, and a positive number
    /// if this value follows <paramref name="obj" />. A <see langword="null" /> object sorts before any value.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="obj" /> is not a <see cref="Fraction{T}" />.
    /// </exception>
    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;

        if (obj is Fraction<T> other)
            return Compare(this, other);

        throw new ArgumentException($"Object must be of type {typeof(Fraction<T>)}.", nameof(obj));
    }

    /// <summary>
    /// Compares two rational values and indicates their relative order.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// A negative number if <paramref name="left" /> precedes <paramref name="right" />, zero if they are equal, and a
    /// positive number if <paramref name="left" /> follows <paramref name="right" />.
    /// </returns>
    private static int Compare(Fraction<T> left, Fraction<T> right) =>
        (left.BigNumerator * right.BigDenominator).CompareTo(right.BigNumerator * left.BigDenominator);
}
