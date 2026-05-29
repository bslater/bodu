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
        Money<TCurrency>.FromNormalizedAmount(left._amount + right._amount);

    /// <summary>
    /// Subtracts one monetary value from another, where both are denominated in the same currency.
    /// </summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The difference <c>left - right</c>.</returns>
    public static Money<TCurrency> operator -(Money<TCurrency> left, Money<TCurrency> right) =>
        Money<TCurrency>.FromNormalizedAmount(left._amount - right._amount);

    /// <summary>
    /// Negates a monetary value.
    /// </summary>
    /// <param name="value">The amount to negate.</param>
    /// <returns>A <see cref="Money{TCurrency}" /> whose amount is the negation of <paramref name="value" />.</returns>
    public static Money<TCurrency> operator -(Money<TCurrency> value) =>
        Money<TCurrency>.FromNormalizedAmount(-value._amount);

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
    /// <remarks>
    /// Every scalar multiplication rounds at the call site, so a chain of multiplications can accumulate
    /// rounding error. For chains where the intermediate precision matters (compound interest, tax stacking,
    /// rate-conversion percentages), perform the calculation through <see cref="ToFraction" /> and snap to
    /// <see cref="Money{TCurrency}" /> only once, at the end, via
    /// <see cref="FromFraction(Fraction{System.Numerics.BigInteger}, MidpointRounding)" /> or the
    /// <see cref="MultiplyExact(Fraction{System.Numerics.BigInteger}, MidpointRounding)" /> shortcut.
    /// </remarks>
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
    /// <para>
    /// Scalar division rounds at every call. To divide an amount into shares while preserving the total exactly,
    /// use <see cref="Money{TCurrency}.Allocate(int)" /> or
    /// <see cref="Money{TCurrency}.Allocate(ReadOnlySpan{decimal})" />.
    /// </para>
    /// <para>
    /// For chains involving division that must avoid per-step rounding drift, perform the calculation through
    /// <see cref="ToFraction" /> and snap back to <see cref="Money{TCurrency}" /> only at the final step via
    /// <see cref="FromFraction(Fraction{System.Numerics.BigInteger}, MidpointRounding)" />.
    /// </para>
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
