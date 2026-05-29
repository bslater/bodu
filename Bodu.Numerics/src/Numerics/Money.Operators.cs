// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Operators.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct Money<TCurrency>
{
    /// <summary>
    /// Adds two monetary values denominated in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
    /// <remarks>
    /// Cross-currency addition is a compile error because the operator signature requires both operands to share
    /// <typeparamref name="TCurrency" />. Convert one side with
    /// <see cref="Convert{TTarget}(decimal, MidpointRounding)" /> when the operands are in different currencies.
    /// </remarks>
    public static Money<TCurrency> operator +(Money<TCurrency> left, Money<TCurrency> right) =>
        new Money<TCurrency>(left._amount + right._amount, normalized: true);

    /// <summary>
    /// Subtracts one monetary value from another, where both are denominated in the same currency.
    /// </summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The difference <c>left - right</c>.</returns>
    public static Money<TCurrency> operator -(Money<TCurrency> left, Money<TCurrency> right) =>
        new Money<TCurrency>(left._amount - right._amount, normalized: true);

    /// <summary>
    /// Negates a monetary value.
    /// </summary>
    /// <param name="value">The amount to negate.</param>
    /// <returns>A <see cref="Money{TCurrency}" /> whose amount is the negation of <paramref name="value" />.</returns>
    public static Money<TCurrency> operator -(Money<TCurrency> value) =>
        new Money<TCurrency>(-value._amount, normalized: true);

    /// <summary>
    /// Returns the operand unchanged.
    /// </summary>
    /// <param name="value">The amount.</param>
    /// <returns>The unchanged <paramref name="value" />.</returns>
    public static Money<TCurrency> operator +(Money<TCurrency> value) =>
        value;

    /// <summary>
    /// Multiplies a monetary value by a scalar, rounding the result to the currency's minor-unit precision.
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar multiplier.</param>
    /// <returns>The product, rounded to <c>TCurrency.MinorUnits</c> using banker's rounding.</returns>
    public static Money<TCurrency> operator *(Money<TCurrency> left, decimal right) =>
        new Money<TCurrency>(left._amount * right);

    /// <summary>
    /// Multiplies a scalar by a monetary value, rounding the result to the currency's minor-unit precision.
    /// </summary>
    /// <param name="left">The scalar multiplier.</param>
    /// <param name="right">The amount.</param>
    /// <returns>The product, rounded to <c>TCurrency.MinorUnits</c> using banker's rounding.</returns>
    public static Money<TCurrency> operator *(decimal left, Money<TCurrency> right) =>
        new Money<TCurrency>(left * right._amount);

    /// <summary>
    /// Divides a monetary value by a scalar, rounding the result to the currency's minor-unit precision.
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar divisor.</param>
    /// <returns>The quotient, rounded to <c>TCurrency.MinorUnits</c> using banker's rounding.</returns>
    /// <exception cref="DivideByZeroException">Thrown when <paramref name="right" /> is zero.</exception>
    /// <remarks>
    /// Scalar division rounds at every call. To divide an amount into shares while preserving the total exactly, use
    /// <see cref="Money{TCurrency}.Allocate(int)" /> or <see cref="Money{TCurrency}.Allocate(ReadOnlySpan{decimal})" />.
    /// </remarks>
    public static Money<TCurrency> operator /(Money<TCurrency> left, decimal right) =>
        new Money<TCurrency>(left._amount / right);

    /// <summary>
    /// Returns the ratio of two monetary values denominated in the same currency.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The dimensionless ratio <c>left.Amount / right.Amount</c>.</returns>
    /// <exception cref="DivideByZeroException">Thrown when <paramref name="right" /> is zero.</exception>
    public static decimal operator /(Money<TCurrency> left, Money<TCurrency> right) =>
        left._amount / right._amount;

    /// <summary>
    /// Determines whether one monetary value is strictly less than another in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> is less than <paramref name="right" />; otherwise <see langword="false" />.</returns>
    public static bool operator <(Money<TCurrency> left, Money<TCurrency> right) =>
        left._amount < right._amount;

    /// <summary>
    /// Determines whether one monetary value is less than or equal to another in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> is less than or equal to <paramref name="right" />; otherwise <see langword="false" />.</returns>
    public static bool operator <=(Money<TCurrency> left, Money<TCurrency> right) =>
        left._amount <= right._amount;

    /// <summary>
    /// Determines whether one monetary value is strictly greater than another in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> is greater than <paramref name="right" />; otherwise <see langword="false" />.</returns>
    public static bool operator >(Money<TCurrency> left, Money<TCurrency> right) =>
        left._amount > right._amount;

    /// <summary>
    /// Determines whether one monetary value is greater than or equal to another in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> is greater than or equal to <paramref name="right" />; otherwise <see langword="false" />.</returns>
    public static bool operator >=(Money<TCurrency> left, Money<TCurrency> right) =>
        left._amount >= right._amount;
}
