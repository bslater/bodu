// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct BigDecimal
{
    /// <summary>
    /// Adds two values exactly.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The exact sum.</returns>
    public static BigDecimal operator +(BigDecimal left, BigDecimal right) =>
        Add(left, right);

    /// <summary>
    /// Subtracts one value from another exactly.
    /// </summary>
    /// <param name="left">The value to subtract from.</param>
    /// <param name="right">The value to subtract.</param>
    /// <returns>The exact difference.</returns>
    public static BigDecimal operator -(BigDecimal left, BigDecimal right) =>
        Subtract(left, right);

    /// <summary>
    /// Multiplies two values exactly.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The exact product.</returns>
    public static BigDecimal operator *(BigDecimal left, BigDecimal right) =>
        Multiply(left, right);

    /// <summary>
    /// Divides one value by another, rounding to <see cref="DefaultDivisionScale" /> decimal places.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The rounded quotient.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> is zero.</exception>
    public static BigDecimal operator /(BigDecimal left, BigDecimal right) =>
        Divide(left, right);

    /// <summary>
    /// Returns the remainder of dividing one value by another.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The exact remainder.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> is zero.</exception>
    public static BigDecimal operator %(BigDecimal left, BigDecimal right) =>
        Remainder(left, right);

    /// <summary>
    /// Returns the value unchanged.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The same value.</returns>
    public static BigDecimal operator +(BigDecimal value) =>
        value;

    /// <summary>
    /// Negates a value.
    /// </summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>The additive inverse of <paramref name="value" />.</returns>
    public static BigDecimal operator -(BigDecimal value) =>
        Negate(value);

    /// <summary>
    /// Increments a value by one.
    /// </summary>
    /// <param name="value">The value to increment.</param>
    /// <returns>The value plus one.</returns>
    public static BigDecimal operator ++(BigDecimal value) =>
        Add(value, One);

    /// <summary>
    /// Decrements a value by one.
    /// </summary>
    /// <param name="value">The value to decrement.</param>
    /// <returns>The value minus one.</returns>
    public static BigDecimal operator --(BigDecimal value) =>
        Subtract(value, One);

    /// <summary>
    /// Determines whether two values are numerically equal.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when the values are equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(BigDecimal left, BigDecimal right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two values are not numerically equal.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when the values differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(BigDecimal left, BigDecimal right) =>
        !left.Equals(right);

    /// <summary>
    /// Determines whether one value is less than another.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="left" /> is less than <paramref name="right" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool operator <(BigDecimal left, BigDecimal right) =>
        left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether one value is less than or equal to another.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="left" /> is less than or equal to <paramref name="right" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    public static bool operator <=(BigDecimal left, BigDecimal right) =>
        left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether one value is greater than another.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="left" /> is greater than <paramref name="right" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool operator >(BigDecimal left, BigDecimal right) =>
        left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether one value is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="left" /> is greater than or equal to <paramref name="right" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    public static bool operator >=(BigDecimal left, BigDecimal right) =>
        left.CompareTo(right) >= 0;
}
