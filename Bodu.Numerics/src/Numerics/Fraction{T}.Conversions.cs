// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction{T}.Conversions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Fraction<T>
{
    /// <summary>
    /// Converts an integer of the backing type to a whole-number <see cref="Fraction{T}" />.
    /// </summary>
    /// <param name="value">The integer value to lift into a rational value.</param>
    /// <returns>A <see cref="Fraction{T}" /> with denominator one.</returns>
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
    /// <exception cref="OverflowException">
    /// Thrown if this value lies outside the range of <see cref="decimal" />.
    /// </exception>
    public decimal ToDecimal() =>
        (decimal)BigNumerator / (decimal)BigDenominator;

    /// <summary>
    /// Converts this rational value to the nearest <see cref="double" />.
    /// </summary>
    /// <returns>The double-precision approximation of this value.</returns>
    public double ToDouble() =>
        (double)BigNumerator / (double)BigDenominator;

    /// <summary>
    /// Converts this rational value to the nearest <see cref="float" />.
    /// </summary>
    /// <returns>The single-precision approximation of this value.</returns>
    public float ToSingle() =>
        (float)BigNumerator / (float)BigDenominator;

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
    /// Converts this rational value to the nearest <see cref="double" />.
    /// </summary>
    /// <param name="result">When this method returns, contains the converted value.</param>
    /// <returns><see langword="true" />, because every rational value has a double-precision approximation.</returns>
    public bool TryToDouble(out double result)
    {
        result = ToDouble();
        return true;
    }

    /// <summary>
    /// Converts this rational value to the nearest <see cref="float" />.
    /// </summary>
    /// <param name="result">When this method returns, contains the converted value.</param>
    /// <returns><see langword="true" />, because every rational value has a single-precision approximation.</returns>
    public bool TryToSingle(out float result)
    {
        result = ToSingle();
        return true;
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
