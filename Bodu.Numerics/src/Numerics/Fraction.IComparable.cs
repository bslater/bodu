// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.IComparable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        return obj is null
            ? 1
            : obj is Fraction<T> other
                ? Compare(this, other)
                : throw new ArgumentException($"Object must be of type {typeof(Fraction<T>)}.", nameof(obj));
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
    public static int Compare(Fraction<T> left, Fraction<T> right) =>
        (left.BigNumerator * right.BigDenominator).CompareTo(right.BigNumerator * left.BigDenominator);

    /// <summary>
    /// Returns the smaller of two rational values.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>Whichever of <paramref name="left" /> and <paramref name="right" /> is smaller.</returns>
    public static Fraction<T> Min(Fraction<T> left, Fraction<T> right) =>
        Compare(left, right) <= 0 ? left : right;

    /// <summary>
    /// Returns the larger of two rational values.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>Whichever of <paramref name="left" /> and <paramref name="right" /> is larger.</returns>
    public static Fraction<T> Max(Fraction<T> left, Fraction<T> right) =>
        Compare(left, right) >= 0 ? left : right;

    /// <summary>
    /// Returns <paramref name="value" /> constrained to the inclusive range bounded by two rational values.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <returns>The clamped value, constrained to the inclusive range.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="min" /> is greater than <paramref name="max" />.
    /// </exception>
    public static Fraction<T> Clamp(Fraction<T> value, Fraction<T> min, Fraction<T> max)
    {
        return Compare(min, max) <= 0
            ? Compare(value, min) < 0
                ? min
                : Compare(value, max) > 0
                    ? max
                    : value
            : throw new ArgumentException("The minimum bound must not exceed the maximum bound.", nameof(min));
    }
}
