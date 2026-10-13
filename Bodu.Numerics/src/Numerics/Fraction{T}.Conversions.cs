// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction{T}.Conversions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Fraction<T>
{
    /// <summary>The number of bits in the significand of a <see cref="decimal" />.</summary>
    private const int DecimalSignificandBits = 96;

    /// <summary>The largest scale (number of fractional digits) a <see cref="decimal" /> can carry.</summary>
    private const int DecimalMaximumScale = 28;

    /// <summary>
    /// Converts an integer of the backing type to a whole-number <see cref="Fraction{T}" />.
    /// </summary>
    /// <param name="value">The integer value to lift into a rational value.</param>
    /// <returns>A <see cref="Fraction{T}" /> with denominator one.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Integers lift implicitly to a whole-number fraction.
    /// Fraction<int> whole = 5;                         // 5/1
    ///
    /// // double and decimal convert explicitly to their exact rational value.
    /// var fromDouble = (Fraction<int>)0.25;            // 1/4
    /// var fromDecimal = (Fraction<int>)1.5m;           // 3/2
    ///
    /// // Convert back to floating point or to the truncated integer part.
    /// double approx = (double)fromDecimal;             // 1.5
    /// int truncated = fromDecimal.ToInteger();         // 1
    ///]]>
    /// </code>
    /// </example>
    public static implicit operator Fraction<T>(T value)
    {
        return new(value);
    }

    /// <summary>
    /// Converts a <see cref="decimal" /> to its exact rational representation.
    /// </summary>
    /// <param name="value">The decimal value to convert.</param>
    /// <returns>A <see cref="Fraction{T}" /> equal to <paramref name="value" />.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public static explicit operator Fraction<T>(decimal value)
    {
        return FromDecimal(value);
    }

    /// <summary>
    /// Converts a finite <see cref="double" /> to its exact rational representation.
    /// </summary>
    /// <param name="value">The double-precision value to convert.</param>
    /// <returns>A <see cref="Fraction{T}" /> equal to <paramref name="value" />.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value" /> is not a finite number.</exception>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public static explicit operator Fraction<T>(double value)
    {
        return FromDouble(value);
    }

    /// <summary>
    /// Converts a rational value to the nearest <see cref="decimal" />.
    /// </summary>
    /// <param name="value">The rational value to convert.</param>
    /// <returns>The decimal approximation of <paramref name="value" />.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if <paramref name="value" /> lies outside the range of <see cref="decimal" />.
    /// </exception>
    public static explicit operator decimal(Fraction<T> value)
    {
        return value.ToDecimal();
    }

    /// <summary>
    /// Converts a rational value to the nearest <see cref="double" />.
    /// </summary>
    /// <param name="value">The rational value to convert.</param>
    /// <returns>The double-precision approximation of <paramref name="value" />.</returns>
    public static explicit operator double(Fraction<T> value)
    {
        return value.ToDouble();
    }

    /// <summary>
    /// Converts a rational value to the nearest <see cref="float" />.
    /// </summary>
    /// <param name="value">The rational value to convert.</param>
    /// <returns>The single-precision approximation of <paramref name="value" />.</returns>
    public static explicit operator float(Fraction<T> value)
    {
        return value.ToSingle();
    }

    /// <summary>
    /// Converts a <see cref="decimal" /> to its exact rational representation.
    /// </summary>
    /// <param name="value">The decimal value to convert.</param>
    /// <returns>A <see cref="Fraction{T}" /> equal to <paramref name="value" />.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> FromDecimal(decimal value)
    {
        (BigInteger numerator, BigInteger denominator) = DecimalToRational(value);

        return FromBigInteger(numerator, denominator);
    }

    /// <summary>
    /// Attempts to convert a <see cref="decimal" /> to its exact rational representation.
    /// </summary>
    /// <param name="value">The decimal value to convert.</param>
    /// <param name="result">When this method returns, contains the converted value, or zero on failure.</param>
    /// <returns><see langword="true" /> if the value was converted; otherwise, <see langword="false" />.</returns>
    public static bool TryFromDecimal(decimal value, out Fraction<T> result)
    {
        try
        {
            result = FromDecimal(value);
            return true;
        }
        catch (OverflowException)
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Converts a finite <see cref="double" /> to its exact rational representation.
    /// </summary>
    /// <param name="value">The double-precision value to convert.</param>
    /// <returns>A <see cref="Fraction{T}" /> equal to <paramref name="value" />.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value" /> is not a finite number.</exception>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> FromDouble(double value)
    {
        (BigInteger numerator, BigInteger denominator) = DoubleToRational(value);

        return FromBigInteger(numerator, denominator);
    }

    /// <summary>
    /// Attempts to convert a <see cref="double" /> to its exact rational representation.
    /// </summary>
    /// <param name="value">The double-precision value to convert.</param>
    /// <param name="result">When this method returns, contains the converted value, or zero on failure.</param>
    /// <returns><see langword="true" /> if the value was converted; otherwise, <see langword="false" />.</returns>
    public static bool TryFromDouble(double value, out Fraction<T> result)
    {
        if (!double.IsFinite(value))
        {
            result = default;
            return false;
        }

        try
        {
            result = FromDouble(value);
            return true;
        }
        catch (OverflowException)
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Converts this rational value to the nearest <see cref="decimal" />.
    /// </summary>
    /// <returns>The decimal approximation of this value.</returns>
    /// <remarks>
    /// The value is rounded to the nearest <see cref="decimal" />, ties to even, with as many fractional digits as
    /// <see cref="decimal" /> can hold for it (at most 28), so a value closer to zero than to the smallest decimal step
    /// converts to zero. The conversion depends only on the value: it succeeds whenever the value lies inside the range
    /// of <see cref="decimal" />, even when the canonical numerator or denominator lies outside it.
    /// </remarks>
    /// <exception cref="OverflowException">
    /// Thrown if this value lies outside the range of <see cref="decimal" />.
    /// </exception>
    public decimal ToDecimal()
    {
        BigInteger numerator = BigNumerator;
        BigInteger denominator = BigDenominator;

        // Components that fit decimal's 96-bit significand convert exactly and decimal division rounds their quotient to
        // the nearest decimal; wider components are divided in BigInteger arithmetic instead, so that only a value
        // outside decimal's range overflows.
        return BigInteger.Abs(numerator).GetBitLength() <= DecimalSignificandBits
            && denominator.GetBitLength() <= DecimalSignificandBits
            ? (decimal)numerator / (decimal)denominator
            : ToDecimalScaled(numerator, denominator);
    }

    /// <summary>
    /// Converts this rational value to the nearest <see cref="double" />.
    /// </summary>
    /// <returns>
    /// The double-precision approximation of this value, or <see cref="double.PositiveInfinity" /> /
    /// <see cref="double.NegativeInfinity" /> when the value lies outside the finite <see cref="double" /> range.
    /// </returns>
    public double ToDouble()
    {
        BigInteger numerator = BigNumerator;
        BigInteger denominator = BigDenominator;

        double numeratorValue = (double)numerator;
        double denominatorValue = (double)denominator;

        // The per-component fast path is valid only while both components are individually representable; once either
        // saturates to infinity the quotient degenerates (Inf/Inf = NaN, Inf/finite = Inf, finite/Inf = 0) even when
        // the true quotient is an ordinary finite value, so the ratio is scaled instead.
        return double.IsFinite(numeratorValue) && double.IsFinite(denominatorValue)
            ? numeratorValue / denominatorValue
            : ToDoubleScaled(numerator, denominator);
    }

    /// <summary>
    /// Converts this rational value to the nearest <see cref="float" />.
    /// </summary>
    /// <returns>
    /// The single-precision approximation of this value, or <see cref="float.PositiveInfinity" /> /
    /// <see cref="float.NegativeInfinity" /> when the value lies outside the finite <see cref="float" /> range.
    /// </returns>
    public float ToSingle() =>
        (float)ToDouble();

    /// <summary>
    /// Computes the quotient of two arbitrary-magnitude integers as a <see cref="double" /> by scaling the ratio with a
    /// common power of two, used when a component is individually unrepresentable as a finite double.
    /// </summary>
    /// <param name="numerator">The (signed) numerator.</param>
    /// <param name="denominator">The strictly positive denominator.</param>
    /// <returns>The quotient approximation, saturating to an infinity or zero beyond the double range.</returns>
    private static double ToDoubleScaled(BigInteger numerator, BigInteger denominator)
    {
        bool negative = numerator.Sign < 0;
        BigInteger magnitude = BigInteger.Abs(numerator);

        // The quotient's binary magnitude is within one bit of the bit-length difference; far outside the double
        // exponent range the result saturates without any division work.
        long quotientBits = magnitude.GetBitLength() - denominator.GetBitLength();
        if (quotientBits > 1026)
            return negative ? double.NegativeInfinity : double.PositiveInfinity;
        if (quotientBits < -1080)
            return negative ? -0.0 : 0.0;

        // Shift the numerator so the truncated integer quotient carries ~108 significant bits - twice the double
        // mantissa - making the division's truncation error vanish below the final rounding, then undo the shift with
        // an exact power-of-two scale (ScaleB handles overflow to infinity and gradual underflow).
        int shift = 108 - (int)quotientBits;
        BigInteger quotient = shift >= 0
            ? (magnitude << shift) / denominator
            : magnitude / (denominator << -shift);

        double result = Math.ScaleB((double)quotient, -shift);
        return negative ? -result : result;
    }

    /// <summary>
    /// Computes the quotient of two arbitrary-magnitude integers as the nearest <see cref="decimal" /> in
    /// <see cref="BigInteger" /> arithmetic, used when a component does not fit the decimal significand.
    /// </summary>
    /// <param name="numerator">The (signed) numerator.</param>
    /// <param name="denominator">The strictly positive denominator.</param>
    /// <returns>
    /// The quotient rounded half to even at the finest scale, at most 28, whose significand fits in 96 bits, without
    /// trailing fractional zeros, as decimal division gives it.
    /// </returns>
    /// <exception cref="OverflowException">The quotient lies outside the range of <see cref="decimal" />.</exception>
    private static decimal ToDecimalScaled(BigInteger numerator, BigInteger denominator)
    {
        bool negative = numerator.Sign < 0;
        BigInteger magnitude = BigInteger.Abs(numerator);

        // The quotient exceeds 2^(q - 1), where q is the difference of the operands' bit lengths, so from q = 97 it is
        // beyond every decimal. Below that, a significand at scale s can fit only while 10^s < 2^(97 - q), so the search
        // starts at that bound (0.30103 rounds log10(2) up) or 28, whichever is smaller, and coarsens until the rounded
        // significand fits.
        long quotientBits = magnitude.GetBitLength() - denominator.GetBitLength();
        if (quotientBits > DecimalSignificandBits)
            throw new OverflowException(NumericsResourceStrings.Overflow_FractionOutsideDecimalRange);

        int scale = (int)Math.Min(DecimalMaximumScale, (DecimalSignificandBits + 1 - quotientBits) * 30103 / 100000);
        BigInteger significand;
        while (true)
        {
            significand = BigInteger.DivRem(magnitude * BigInteger.Pow(10, scale), denominator, out BigInteger remainder);

            BigInteger twiceRemainder = remainder << 1;
            if (twiceRemainder > denominator || (twiceRemainder == denominator && !significand.IsEven))
                significand++;

            if (significand.GetBitLength() <= DecimalSignificandBits)
                break;

            if (scale == 0)
                throw new OverflowException(NumericsResourceStrings.Overflow_FractionOutsideDecimalRange);

            scale--;
        }

        if (significand.IsZero)
            return decimal.Zero;

        // Decimal division of whole operands leaves no trailing fractional zeros, so neither does this path.
        while (scale > 0)
        {
            BigInteger shorter = BigInteger.DivRem(significand, 10, out BigInteger digit);
            if (!digit.IsZero)
                break;

            significand = shorter;
            scale--;
        }

        return new decimal(
            unchecked((int)(uint)(significand & uint.MaxValue)),
            unchecked((int)(uint)((significand >> 32) & uint.MaxValue)),
            unchecked((int)(uint)(significand >> 64)),
            negative,
            (byte)scale);
    }

    /// <summary>
    /// Converts this rational value to a <see cref="BigInteger" />, truncating any fractional component.
    /// </summary>
    /// <returns>The integer part of this value, rounded toward zero.</returns>
    public BigInteger ToBigInteger() =>
        BigNumerator / BigDenominator;

    /// <summary>
    /// Converts this rational value to a value of the backing type, truncating any fractional component.
    /// </summary>
    /// <returns>The integer part of this value, rounded toward zero.</returns>
    public T ToInteger() =>
        T.CreateChecked(BigNumerator / BigDenominator);

    /// <summary>
    /// Attempts to convert this rational value to the nearest <see cref="decimal" />.
    /// </summary>
    /// <param name="result">When this method returns, contains the converted value, or zero on failure.</param>
    /// <returns><see langword="true" /> if the value was converted; otherwise, <see langword="false" />.</returns>
    public bool TryToDecimal(out decimal result)
    {
        try
        {
            result = ToDecimal();
            return true;
        }
        catch (OverflowException)
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Attempts to convert this rational value to the nearest <see cref="double" />.
    /// </summary>
    /// <param name="result">When this method returns, contains the converted value, or zero on failure.</param>
    /// <returns>
    /// <see langword="true" /> if the value has a finite double-precision approximation; <see langword="false" /> when
    /// it lies outside the finite <see cref="double" /> range.
    /// </returns>
    public bool TryToDouble(out double result)
    {
        double value = ToDouble();
        if (double.IsFinite(value))
        {
            result = value;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to convert this rational value to the nearest <see cref="float" />.
    /// </summary>
    /// <param name="result">When this method returns, contains the converted value, or zero on failure.</param>
    /// <returns>
    /// <see langword="true" /> if the value has a finite single-precision approximation; <see langword="false" /> when
    /// it lies outside the finite <see cref="float" /> range.
    /// </returns>
    public bool TryToSingle(out float result)
    {
        float value = ToSingle();
        if (float.IsFinite(value))
        {
            result = value;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Converts the integer part of this rational value to a <see cref="BigInteger" />.
    /// </summary>
    /// <param name="result">When this method returns, contains the converted value.</param>
    /// <returns><see langword="true" />, because every rational value has an integer part.</returns>
    public bool TryToBigInteger(out BigInteger result)
    {
        result = ToBigInteger();
        return true;
    }

    /// <summary>
    /// Converts the integer part of this rational value to a value of the backing type.
    /// </summary>
    /// <param name="result">When this method returns, contains the converted value.</param>
    /// <returns><see langword="true" />, because the integer part always fits the backing type.</returns>
    public bool TryToInteger(out T result)
    {
        result = ToInteger();
        return true;
    }

    /// <summary>
    /// Converts this rational value to an equivalent <see cref="Fraction{T}" /> over a different backing integer type.
    /// </summary>
    /// <typeparam name="TOther">The backing integer type of the result.</typeparam>
    /// <returns>A <see cref="Fraction{TOther}" /> with the same canonical value.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="TOther" />.
    /// </exception>
    public Fraction<TOther> As<TOther>()
        where TOther : IBinaryInteger<TOther> =>
        new(TOther.CreateChecked(Numerator), TOther.CreateChecked(Denominator));

    /// <summary>
    /// Decomposes a finite <see cref="double" /> into the exact numerator and denominator of its rational value.
    /// </summary>
    /// <param name="value">The double-precision value to decompose.</param>
    /// <returns>The exact numerator and denominator of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value" /> is not a finite number.</exception>
    private static (BigInteger Numerator, BigInteger Denominator) DoubleToRational(double value)
    {
        if (!double.IsFinite(value))
            throw new ArgumentException(NumericsResourceStrings.Arg_Invalid_NonFiniteToFraction, nameof(value));

        long bits = BitConverter.DoubleToInt64Bits(value);
        bool negative = bits < 0;
        int exponent = (int)((bits >> 52) & 0x7FF);
        long mantissa = bits & 0xFFFFFFFFFFFFF;

        if (exponent == 0)
            exponent++;
        else
            mantissa |= 0x10000000000000;

        exponent -= 1075;

        BigInteger numerator = mantissa;
        if (negative)
            numerator = -numerator;

        return exponent >= 0
            ? (numerator << exponent, BigInteger.One)
            : (numerator, BigInteger.One << -exponent);
    }

    /// <summary>
    /// Decomposes a <see cref="decimal" /> into the exact numerator and denominator of its rational value.
    /// </summary>
    /// <param name="value">The decimal value to decompose.</param>
    /// <returns>The exact numerator and denominator of <paramref name="value" />.</returns>
    private static (BigInteger Numerator, BigInteger Denominator) DecimalToRational(decimal value)
    {
        int[] bits = decimal.GetBits(value);
        BigInteger mantissa = (new BigInteger((uint)bits[2]) << 64)
            | (new BigInteger((uint)bits[1]) << 32)
            | new BigInteger((uint)bits[0]);

        bool negative = (bits[3] & unchecked((int)0x80000000)) != 0;
        int scale = (bits[3] >> 16) & 0xFF;

        if (negative)
            mantissa = -mantissa;

        return (mantissa, BigInteger.Pow(10, scale));
    }
}
