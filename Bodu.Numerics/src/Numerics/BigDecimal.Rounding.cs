// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.Rounding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct BigDecimal
{
    /// <summary>
    /// Rounds a value to the specified number of decimal places using the specified rounding mode.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <param name="scale">The number of fractional decimal places to round to.</param>
    /// <param name="mode">The rounding mode.</param>
    /// <returns>
    /// The rounded value; if <paramref name="value" /> already has fewer decimal places it is returned unchanged.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scale" /> is negative.</exception>
    public static BigDecimal Round(BigDecimal value, int scale, MidpointRounding mode)
    {
        ThrowHelper.ThrowIfNegative(scale);

        if (value._scale <= scale)
            return value;

        BigInteger divisor = BigInteger.Pow(new BigInteger(10), value._scale - scale);
        return new BigDecimal(DivideRounded(value._mantissa, divisor, mode), scale);
    }

    /// <summary>
    /// Rounds a value to the specified number of decimal places using <see cref="MidpointRounding.ToEven" />.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <param name="scale">The number of fractional decimal places to round to.</param>
    /// <returns>The rounded value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scale" /> is negative.</exception>
    public static BigDecimal Round(BigDecimal value, int scale) =>
        Round(value, scale, MidpointRounding.ToEven);

    /// <summary>
    /// Rounds a value to the nearest integer using <see cref="MidpointRounding.ToEven" />.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <returns>The nearest integer value.</returns>
    public static BigDecimal Round(BigDecimal value) =>
        Round(value, 0, MidpointRounding.ToEven);

    /// <summary>
    /// Returns the largest integer value less than or equal to <paramref name="value" />.
    /// </summary>
    /// <param name="value">The value to floor.</param>
    /// <returns>The floor of <paramref name="value" />.</returns>
    public static BigDecimal Floor(BigDecimal value) =>
        Round(value, 0, MidpointRounding.ToNegativeInfinity);

    /// <summary>
    /// Returns the smallest integer value greater than or equal to <paramref name="value" />.
    /// </summary>
    /// <param name="value">The value to ceiling.</param>
    /// <returns>The ceiling of <paramref name="value" />.</returns>
    public static BigDecimal Ceiling(BigDecimal value) =>
        Round(value, 0, MidpointRounding.ToPositiveInfinity);

    /// <summary>
    /// Returns the integer part of <paramref name="value" />, discarding any fractional digits.
    /// </summary>
    /// <param name="value">The value to truncate.</param>
    /// <returns>The truncated value.</returns>
    public static BigDecimal Truncate(BigDecimal value) =>
        Round(value, 0, MidpointRounding.ToZero);
}
