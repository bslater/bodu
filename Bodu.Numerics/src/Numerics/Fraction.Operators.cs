// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Fraction<T>
{
    /// <summary>
    /// Gets the canonical numerator promoted to <see cref="BigInteger" /> precision.
    /// </summary>
    /// <returns>The numerator as a <see cref="BigInteger" />.</returns>
    private BigInteger BigNumerator =>
        BigInteger.CreateChecked(_numerator);

    /// <summary>
    /// Gets the canonical denominator promoted to <see cref="BigInteger" /> precision.
    /// </summary>
    /// <returns>The denominator as a <see cref="BigInteger" />.</returns>
    private BigInteger BigDenominator =>
        BigInteger.CreateChecked(Denominator);

    /// <summary>
    /// Adds two rational values.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The canonical sum of <paramref name="left" /> and <paramref name="right" />.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical result does not fit <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> operator +(Fraction<T> left, Fraction<T> right)
    {
        return FromBigInteger(
            (left.BigNumerator * right.BigDenominator) + (right.BigNumerator * left.BigDenominator),
            left.BigDenominator * right.BigDenominator);
    }

    /// <summary>
    /// Subtracts one rational value from another.
    /// </summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The canonical difference <paramref name="left" /> minus <paramref name="right" />.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical result does not fit <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> operator -(Fraction<T> left, Fraction<T> right)
    {
        return FromBigInteger(
            (left.BigNumerator * right.BigDenominator) - (right.BigNumerator * left.BigDenominator),
            left.BigDenominator * right.BigDenominator);
    }

    /// <summary>
    /// Multiplies two rational values.
    /// </summary>
    /// <param name="left">The first factor.</param>
    /// <param name="right">The second factor.</param>
    /// <returns>The canonical product of <paramref name="left" /> and <paramref name="right" />.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical result does not fit <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> operator *(Fraction<T> left, Fraction<T> right)
    {
        return FromBigInteger(
            left.BigNumerator * right.BigNumerator,
            left.BigDenominator * right.BigDenominator);
    }

    /// <summary>
    /// Divides one rational value by another.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The canonical quotient <paramref name="left" /> divided by <paramref name="right" />.</returns>
    /// <exception cref="DivideByZeroException">Thrown if <paramref name="right" /> is zero.</exception>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical result does not fit <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> operator /(Fraction<T> left, Fraction<T> right)
    {
        return !right.IsZero
            ? FromBigInteger(
                left.BigNumerator * right.BigDenominator,
                left.BigDenominator * right.BigNumerator)
            : throw new DivideByZeroException(NumericsResourceStrings.DivideByZero_Division);
    }

    /// <summary>
    /// Returns the remainder produced by dividing one rational value by another, with the sign of the dividend.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The canonical remainder of <paramref name="left" /> divided by <paramref name="right" />.</returns>
    /// <exception cref="DivideByZeroException">Thrown if <paramref name="right" /> is zero.</exception>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical result does not fit <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> operator %(Fraction<T> left, Fraction<T> right)
    {
        if (right.IsZero)
            throw new DivideByZeroException(NumericsResourceStrings.DivideByZero_Remainder);

        BigInteger ln = left.BigNumerator;
        BigInteger ld = left.BigDenominator;
        BigInteger rn = right.BigNumerator;
        BigInteger rd = right.BigDenominator;

        BigInteger quotient = (ln * rd) / (ld * rn);

        return FromBigInteger((ln * rd) - (quotient * rn * ld), ld * rd);
    }

    /// <summary>
    /// Returns the additive inverse of a rational value.
    /// </summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>The canonical value with its sign reversed.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the negated numerator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> operator -(Fraction<T> value)
    {
        return FromBigInteger(-value.BigNumerator, value.BigDenominator);
    }

    /// <summary>
    /// Returns the rational value unchanged.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <returns>The unmodified <paramref name="value" />.</returns>
    public static Fraction<T> operator +(Fraction<T> value)
    {
        return value;
    }

    /// <summary>
    /// Returns the rational value increased by one.
    /// </summary>
    /// <param name="value">The value to increment.</param>
    /// <returns>The canonical value of <paramref name="value" /> plus one.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical result does not fit <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> operator ++(Fraction<T> value)
    {
        return value + One;
    }

    /// <summary>
    /// Returns the rational value decreased by one.
    /// </summary>
    /// <param name="value">The value to decrement.</param>
    /// <returns>The canonical value of <paramref name="value" /> minus one.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical result does not fit <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> operator --(Fraction<T> value)
    {
        return value - One;
    }

    /// <summary>
    /// Determines whether two rational values are equal.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> if the values are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(Fraction<T> left, Fraction<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two rational values are unequal.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> if the values are unequal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(Fraction<T> left, Fraction<T> right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Determines whether one rational value is less than another.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is less than <paramref name="right" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool operator <(Fraction<T> left, Fraction<T> right)
    {
        return Compare(left, right) < 0;
    }

    /// <summary>
    /// Determines whether one rational value is less than or equal to another.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is less than or equal to <paramref name="right" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator <=(Fraction<T> left, Fraction<T> right)
    {
        return Compare(left, right) <= 0;
    }

    /// <summary>
    /// Determines whether one rational value is greater than another.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is greater than <paramref name="right" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool operator >(Fraction<T> left, Fraction<T> right)
    {
        return Compare(left, right) > 0;
    }

    /// <summary>
    /// Determines whether one rational value is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is greater than or equal to <paramref name="right" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator >=(Fraction<T> left, Fraction<T> right)
    {
        return Compare(left, right) >= 0;
    }
}
