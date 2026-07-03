// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.IComparable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct BigDecimal
    : IComparable, IComparable<BigDecimal>
{
    /// <summary>
    /// Compares this value with another <see cref="BigDecimal" />.
    /// </summary>
    /// <param name="other">The value to compare with.</param>
    /// <returns>
    /// A negative number when this value is less than <paramref name="other" />, zero when they are equal, and a
    /// positive number when this value is greater.
    /// </returns>
    public int CompareTo(BigDecimal other)
    {
        int scale = Math.Max(_scale, other._scale);
        return ScaleTo(this, scale).CompareTo(ScaleTo(other, scale));
    }

    /// <summary>
    /// Compares this value with another object.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with, which must be a <see cref="BigDecimal" /> or <see langword="null" />.
    /// </param>
    /// <returns>
    /// A negative number when this value is less than <paramref name="obj" />, zero when they are equal, and a positive
    /// number when this value is greater. A <see langword="null" /> object sorts before every value.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="obj" /> is not a <see cref="BigDecimal" />.</exception>
    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;

        return obj is BigDecimal other
            ? CompareTo(other)
            : throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, NumericsResourceStrings.Arg_Invalid_ComparandType, nameof(BigDecimal)),
                nameof(obj));
    }

    /// <summary>
    /// Returns the smaller of two values.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The lesser of the two values.</returns>
    public static BigDecimal Min(BigDecimal left, BigDecimal right) =>
        left.CompareTo(right) <= 0 ? left : right;

    /// <summary>
    /// Returns the larger of two values.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The greater of the two values.</returns>
    public static BigDecimal Max(BigDecimal left, BigDecimal right) =>
        left.CompareTo(right) >= 0 ? left : right;
}
