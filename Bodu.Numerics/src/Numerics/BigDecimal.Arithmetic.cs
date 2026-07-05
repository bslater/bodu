// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.Arithmetic.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct BigDecimal
{
    /// <summary>
    /// Adds two values exactly.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The exact sum.</returns>
    public static BigDecimal Add(BigDecimal left, BigDecimal right)
    {
        int scale = Math.Max(left._scale, right._scale);
        return new BigDecimal(ScaleTo(left, scale) + ScaleTo(right, scale), scale);
    }

    /// <summary>
    /// Subtracts one value from another exactly.
    /// </summary>
    /// <param name="left">The value to subtract from.</param>
    /// <param name="right">The value to subtract.</param>
    /// <returns>The exact difference.</returns>
    public static BigDecimal Subtract(BigDecimal left, BigDecimal right)
    {
        int scale = Math.Max(left._scale, right._scale);
        return new BigDecimal(ScaleTo(left, scale) - ScaleTo(right, scale), scale);
    }

    /// <summary>
    /// Multiplies two values exactly.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The exact product.</returns>
    public static BigDecimal Multiply(BigDecimal left, BigDecimal right) =>
        new(left._mantissa * right._mantissa, left._scale + right._scale);

    /// <summary>
    /// Divides one value by another, rounding to <see cref="DefaultDivisionScale" /> decimal places using
    /// <see cref="MidpointRounding.ToEven" />.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The quotient; an exact quotient is trimmed to its shortest canonical form.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> is zero.</exception>
    public static BigDecimal Divide(BigDecimal left, BigDecimal right) =>
        Divide(left, right, DefaultDivisionScale, MidpointRounding.ToEven);

    /// <summary>
    /// Divides one value by another, rounding the result to the specified number of decimal places.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <param name="scale">The number of fractional decimal places to round the quotient to.</param>
    /// <param name="mode">The rounding mode applied at the requested scale.</param>
    /// <returns>The quotient rounded to <paramref name="scale" /> decimal places, in canonical form.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> is zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scale" /> is negative.</exception>
    public static BigDecimal Divide(BigDecimal left, BigDecimal right, int scale, MidpointRounding mode)
    {
        if (right.IsZero) throw new DivideByZeroException(NumericsResourceStrings.DivideByZero_BigDecimalDivision);
        ThrowHelper.ThrowIfNegative(scale);

        // left / right rounded to `scale` decimals: quotient Q = round( m_l / m_r * 10^(s_r - s_l + scale) ).
        int exponent = right._scale - left._scale + scale;
        var ten = new BigInteger(10);

        BigInteger numerator = left._mantissa;
        BigInteger denominator = right._mantissa;
        if (exponent >= 0)
            numerator *= BigInteger.Pow(ten, exponent);
        else
            denominator *= BigInteger.Pow(ten, -exponent);

        return new BigDecimal(DivideRounded(numerator, denominator, mode), scale);
    }

    /// <summary>
    /// Returns the remainder of dividing one value by another — the exact value of
    /// <c>left - Truncate(left / right) &#215; right</c>.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The exact remainder, whose sign matches <paramref name="left" />.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> is zero.</exception>
    public static BigDecimal Remainder(BigDecimal left, BigDecimal right)
    {
        if (right.IsZero) throw new DivideByZeroException(NumericsResourceStrings.DivideByZero_BigDecimalRemainder);

        BigDecimal integralQuotient = Divide(left, right, 0, MidpointRounding.ToZero);
        return Subtract(left, Multiply(integralQuotient, right));
    }

    /// <summary>
    /// Negates a value.
    /// </summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>The additive inverse of <paramref name="value" />.</returns>
    public static BigDecimal Negate(BigDecimal value) =>
        new(-value._mantissa, value._scale);

    /// <summary>
    /// Returns the absolute value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The magnitude of <paramref name="value" />.</returns>
    public static BigDecimal Abs(BigDecimal value) =>
        new(BigInteger.Abs(value._mantissa), value._scale);

    /// <summary>
    /// Raises a value to an integer power.
    /// </summary>
    /// <param name="value">The base value.</param>
    /// <param name="exponent">The integer exponent.</param>
    /// <returns>
    /// <paramref name="value" /> raised to <paramref name="exponent" />. A non-negative exponent is exact; a negative
    /// exponent divides into one at <see cref="DefaultDivisionScale" /> decimal places.
    /// </returns>
    /// <exception cref="DivideByZeroException">
    /// <paramref name="value" /> is zero and <paramref name="exponent" /> is negative.
    /// </exception>
    public static BigDecimal Pow(BigDecimal value, int exponent)
    {
        if (exponent == 0)
            return One;

        if (exponent > 0)
            return new BigDecimal(BigInteger.Pow(value._mantissa, exponent), value._scale * exponent);

        return Divide(One, Pow(value, -exponent));
    }

    /// <summary>
    /// Divides <paramref name="numerator" /> by <paramref name="denominator" /> as integers, rounding the exact
    /// quotient to the nearest integer under <paramref name="mode" />.
    /// </summary>
    /// <param name="numerator">The integer numerator.</param>
    /// <param name="denominator">The non-zero integer denominator.</param>
    /// <param name="mode">The rounding mode.</param>
    /// <returns>The rounded integer quotient.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="mode" /> is not a defined rounding mode.
    /// </exception>
    private static BigInteger DivideRounded(BigInteger numerator, BigInteger denominator, MidpointRounding mode)
    {
        BigInteger quotient = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
        if (remainder.IsZero)
            return quotient;

        // Truncation is toward zero; decide whether to step one further away from zero.
        int sign = numerator.Sign * denominator.Sign;
        int halfComparison = (BigInteger.Abs(remainder) * 2).CompareTo(BigInteger.Abs(denominator));

        bool roundAwayFromZero = mode switch
        {
            MidpointRounding.ToZero => false,
            MidpointRounding.AwayFromZero => halfComparison >= 0,
            MidpointRounding.ToEven => halfComparison > 0 || (halfComparison == 0 && !quotient.IsEven),
            MidpointRounding.ToPositiveInfinity => sign > 0,
            MidpointRounding.ToNegativeInfinity => sign < 0,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };

        return roundAwayFromZero ? quotient + sign : quotient;
    }
}
