// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.Arithmetic.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Fraction<T>
{
    /// <summary>
    /// Adds two rational values.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The canonical sum of <paramref name="left" /> and <paramref name="right" />.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical result does not fit <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> Add(Fraction<T> left, Fraction<T> right) =>
        left + right;

    /// <summary>
    /// Subtracts one rational value from another.
    /// </summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The canonical difference <paramref name="left" /> minus <paramref name="right" />.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical result does not fit <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> Subtract(Fraction<T> left, Fraction<T> right) =>
        left - right;

    /// <summary>
    /// Multiplies two rational values.
    /// </summary>
    /// <param name="left">The first factor.</param>
    /// <param name="right">The second factor.</param>
    /// <returns>The canonical product of <paramref name="left" /> and <paramref name="right" />.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical result does not fit <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> Multiply(Fraction<T> left, Fraction<T> right) =>
        left * right;

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
    public static Fraction<T> Divide(Fraction<T> left, Fraction<T> right) =>
        left / right;

    /// <summary>
    /// Returns the additive inverse of this rational value.
    /// </summary>
    /// <returns>The canonical value with its sign reversed.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the negated numerator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public Fraction<T> Negate() =>
        -this;

    /// <summary>
    /// Returns the absolute value of this rational value.
    /// </summary>
    /// <returns>The canonical magnitude of this value.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if the magnitude of the numerator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public Fraction<T> Abs() =>
        Sign < 0 ? FromBigInteger(-BigNumerator, BigDenominator) : this;

    /// <summary>
    /// Returns the multiplicative inverse of this rational value.
    /// </summary>
    /// <returns>The canonical value with its numerator and denominator exchanged.</returns>
    /// <exception cref="DivideByZeroException">Thrown if this value is zero.</exception>
    /// <exception cref="OverflowException">
    /// Thrown if the reciprocal cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public Fraction<T> Reciprocal()
    {
        if (IsZero)
            throw new DivideByZeroException("Cannot compute the reciprocal of zero.");

        return FromBigInteger(BigDenominator, BigNumerator);
    }

    /// <summary>
    /// Raises this rational value to the specified integer power.
    /// </summary>
    /// <param name="exponent">
    /// The exponent. A negative value raises the reciprocal to the corresponding magnitude.
    /// </param>
    /// <returns>The canonical value of this rational raised to <paramref name="exponent" />.</returns>
    /// <exception cref="DivideByZeroException">
    /// Thrown if <paramref name="exponent" /> is negative and this value is zero.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical result does not fit <typeparamref name="T" />, or if the magnitude of a negative
    /// <paramref name="exponent" /> exceeds <see cref="int.MaxValue" />.
    /// </exception>
    public Fraction<T> Pow(int exponent)
    {
        if (exponent == 0)
            return One;

        if (exponent < 0)
        {
            if (IsZero)
                throw new DivideByZeroException("Cannot raise zero to a negative power.");

            long magnitude = -(long)exponent;
            if (magnitude > int.MaxValue)
                throw new OverflowException("The magnitude of the exponent is too large to evaluate.");

            return FromBigInteger(
                BigInteger.Pow(BigDenominator, (int)magnitude),
                BigInteger.Pow(BigNumerator, (int)magnitude));
        }

        return FromBigInteger(
            BigInteger.Pow(BigNumerator, exponent),
            BigInteger.Pow(BigDenominator, exponent));
    }

    /// <summary>
    /// Returns the remainder produced by dividing this rational value by another, with the sign of the dividend.
    /// </summary>
    /// <param name="divisor">The value to divide by.</param>
    /// <returns>The canonical remainder of this value divided by <paramref name="divisor" />.</returns>
    /// <exception cref="DivideByZeroException">Thrown if <paramref name="divisor" /> is zero.</exception>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical result does not fit <typeparamref name="T" />.
    /// </exception>
    public Fraction<T> Remainder(Fraction<T> divisor) =>
        this % divisor;
}
