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
    /// <remarks>
    /// The comparison decides on sign, then on equal scales, then on the operands' integer-magnitude bounds, and only
    /// rescales to a common scale when the magnitudes genuinely overlap. Rescaling multiplies an unscaled value by
    /// <c>10</c> raised to the scale gap, so short-circuiting first bounds the transient allocation that a pathological
    /// scale difference would otherwise force on every comparison.
    /// </remarks>
    public int CompareTo(BigDecimal other)
    {
        int sign = _mantissa.Sign;
        int otherSign = other._mantissa.Sign;
        if (sign != otherSign)
            return sign.CompareTo(otherSign);

        if (sign == 0)
            return 0;

        if (_scale == other._scale)
            return _mantissa.CompareTo(other._mantissa);

        // Same nonzero sign, different scales: compare by integer-magnitude bounds. Disjoint bounds decide the
        // ordering of the absolute values; the sign then orients the result.
        (long thisLow, long thisHigh) = IntegerExponentBounds(in this);
        (long otherLow, long otherHigh) = IntegerExponentBounds(in other);

        if (thisHigh < otherLow)
            return -sign;

        if (thisLow > otherHigh)
            return sign;

        int scale = Math.Max(_scale, other._scale);
        return ScaleTo(this, scale).CompareTo(ScaleTo(other, scale));
    }

    /// <summary>
    /// Computes conservative lower and upper bounds on a nonzero value's base-ten integer exponent — the <c>D</c> for
    /// which the absolute value lies in <c>[10^(D-1), 10^D)</c> — from the unscaled value's bit length.
    /// </summary>
    /// <param name="value">The nonzero value whose exponent bounds are computed.</param>
    /// <returns>An inclusive <c>(low, high)</c> exponent range guaranteed to contain the true exponent.</returns>
    /// <remarks>
    /// A positive integer with bit length <c>b</c> has between <c>⌊(b−1)·log₁₀2⌋+1</c> and <c>⌊b·log₁₀2⌋+1</c> decimal
    /// digits. The bounds are widened by one digit on each side to absorb double-precision rounding at the floor
    /// boundaries, keeping the range conservative — a wider range only means falling back to the exact comparison.
    /// </remarks>
    private static (long Low, long High) IntegerExponentBounds(in BigDecimal value)
    {
        const double Log10Of2 = 0.30102999566398119521;

        long bits = BigInteger.Abs(value._mantissa).GetBitLength();
        long digitsLow = (long)Math.Floor((bits - 1) * Log10Of2) + 1 - 1;
        long digitsHigh = (long)Math.Floor(bits * Log10Of2) + 1 + 1;

        return (digitsLow - value._scale, digitsHigh - value._scale);
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
