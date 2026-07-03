// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.Conversions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct BigDecimal
{
    /// <summary>
    /// Implicitly converts a 32-bit signed integer to a <see cref="BigDecimal" />.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>The exact decimal value.</returns>
    public static implicit operator BigDecimal(int value) =>
        new(new BigInteger(value));

    /// <summary>
    /// Implicitly converts a 64-bit signed integer to a <see cref="BigDecimal" />.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>The exact decimal value.</returns>
    public static implicit operator BigDecimal(long value) =>
        new(new BigInteger(value));

    /// <summary>
    /// Implicitly converts a <see cref="BigInteger" /> to a <see cref="BigDecimal" />.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>The exact decimal value.</returns>
    public static implicit operator BigDecimal(BigInteger value) =>
        new(value);

    /// <summary>
    /// Implicitly converts a <see cref="decimal" /> to a <see cref="BigDecimal" /> exactly.
    /// </summary>
    /// <param name="value">The decimal value.</param>
    /// <returns>The exact decimal value.</returns>
    public static implicit operator BigDecimal(decimal value) =>
        FromDecimal(value);

    /// <summary>
    /// Explicitly converts a <see cref="BigDecimal" /> to a <see cref="BigInteger" />, truncating toward zero.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The integer part of <paramref name="value" />.</returns>
    public static explicit operator BigInteger(BigDecimal value) =>
        value.ToBigInteger();

    /// <summary>
    /// Explicitly converts a <see cref="BigDecimal" /> to a <see cref="decimal" />.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value as a <see cref="decimal" />.</returns>
    /// <exception cref="OverflowException">
    /// <paramref name="value" /> is outside the range of <see cref="decimal" />.
    /// </exception>
    public static explicit operator decimal(BigDecimal value) =>
        value.ToDecimal();

    /// <summary>
    /// Explicitly converts a <see cref="BigDecimal" /> to the nearest <see cref="double" />.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The nearest <see cref="double" />.</returns>
    public static explicit operator double(BigDecimal value) =>
        value.ToDouble();

    /// <summary>
    /// Explicitly converts a <see cref="double" /> to a <see cref="BigDecimal" /> via its shortest round-trip decimal
    /// text — so <c>0.1d</c> becomes <c>0.1</c>, not its exact binary expansion.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The decimal value.</returns>
    /// <exception cref="ArgumentException"><paramref name="value" /> is not a finite number.</exception>
    public static explicit operator BigDecimal(double value) =>
        FromDouble(value);

    /// <summary>
    /// Converts a <see cref="decimal" /> to a <see cref="BigDecimal" /> exactly.
    /// </summary>
    /// <param name="value">The decimal value.</param>
    /// <returns>The exact decimal value.</returns>
    public static BigDecimal FromDecimal(decimal value) =>
        Parse(value.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts a <see cref="double" /> to a <see cref="BigDecimal" /> via its shortest round-trip decimal text.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The decimal value.</returns>
    /// <exception cref="ArgumentException"><paramref name="value" /> is not a finite number.</exception>
    public static BigDecimal FromDouble(double value)
    {
        if (!double.IsFinite(value))
            throw new ArgumentException(NumericsResourceStrings.Arg_Invalid_NonFiniteToBigDecimal, nameof(value));

        return Parse(value.ToString("R", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the integer part of the value, truncated toward zero.
    /// </summary>
    /// <returns>The truncated integer value.</returns>
    public BigInteger ToBigInteger() =>
        _scale == 0 ? _mantissa : _mantissa / BigInteger.Pow(new BigInteger(10), _scale);

    /// <summary>
    /// Converts the value to a <see cref="decimal" />.
    /// </summary>
    /// <returns>The value as a <see cref="decimal" />.</returns>
    /// <exception cref="OverflowException">The value is outside the range of <see cref="decimal" />.</exception>
    public decimal ToDecimal() =>
        decimal.Parse(ToString(null, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts the value to the nearest <see cref="double" />.
    /// </summary>
    /// <returns>The nearest <see cref="double" />.</returns>
    public double ToDouble() =>
        double.Parse(ToString(null, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
}
