// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.Conversions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
    public static implicit operator Fraction<T>(T value) =>
        new Fraction<T>(value);

    /// <summary>
    /// Converts a <see cref="decimal" /> to its exact rational representation.
    /// </summary>
    /// <param name="value">The decimal value to convert.</param>
    /// <returns>A <see cref="Fraction{T}" /> equal to <paramref name="value" />.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public static explicit operator Fraction<T>(decimal value) =>
        FromDecimal(value);

    /// <summary>
    /// Converts a finite <see cref="double" /> to its exact rational representation.
    /// </summary>
    /// <param name="value">The double-precision value to convert.</param>
    /// <returns>A <see cref="Fraction{T}" /> equal to <paramref name="value" />.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value" /> is not a finite number.</exception>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public static explicit operator Fraction<T>(double value) =>
        FromDouble(value);

    /// <summary>
    /// Converts a rational value to the nearest <see cref="decimal" />.
    /// </summary>
    /// <param name="value">The rational value to convert.</param>
    /// <returns>The decimal approximation of <paramref name="value" />.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if <paramref name="value" /> lies outside the range of <see cref="decimal" />.
    /// </exception>
    public static explicit operator decimal(Fraction<T> value) =>
        value.ToDecimal();

    /// <summary>
    /// Converts a rational value to the nearest <see cref="double" />.
    /// </summary>
    /// <param name="value">The rational value to convert.</param>
    /// <returns>The double-precision approximation of <paramref name="value" />.</returns>
    public static explicit operator double(Fraction<T> value) =>
        value.ToDouble();

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
        int[] bits = decimal.GetBits(value);
        BigInteger mantissa = (new BigInteger((uint)bits[2]) << 64)
            | (new BigInteger((uint)bits[1]) << 32)
            | new BigInteger((uint)bits[0]);

        bool negative = (bits[3] & unchecked((int)0x80000000)) != 0;
        int scale = (bits[3] >> 16) & 0xFF;

        if (negative)
            mantissa = -mantissa;

        return FromBigInteger(mantissa, BigInteger.Pow(10, scale));
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
        if (!double.IsFinite(value))
            throw new ArgumentException("Only finite values can be converted to a fraction.", nameof(value));

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
            ? FromBigInteger(numerator << exponent, BigInteger.One)
            : FromBigInteger(numerator, BigInteger.One << -exponent);
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
    /// Converts this rational value to an equivalent <see cref="Fraction{T}" /> over a different backing integer type.
    /// </summary>
    /// <typeparam name="TOther">The backing integer type of the result.</typeparam>
    /// <returns>A <see cref="Fraction{TOther}" /> with the same canonical value.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="TOther" />.
    /// </exception>
    public Fraction<TOther> As<TOther>()
        where TOther : IBinaryInteger<TOther> =>
        new Fraction<TOther>(TOther.CreateChecked(_numerator), TOther.CreateChecked(Denominator));
}
