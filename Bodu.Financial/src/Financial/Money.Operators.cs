// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public readonly partial struct Money<TCurrency>
{
    /// <summary>
    /// Adds two monetary values denominated in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
    /// <exception cref="OverflowException">The sum falls outside the range of <see cref="decimal" />.</exception>
    /// <remarks>
    /// Cross-currency addition is a compile error because the operator signature requires both operands to share
    /// <typeparamref name="TCurrency" />. Convert one side with
    /// <see cref="Convert{TTarget}(decimal, MidpointRounding)" /> when the operands are in different currencies.
    /// </remarks>
    public static Money<TCurrency> operator +(Money<TCurrency> left, Money<TCurrency> right)
    {
        return Money<TCurrency>.FromNormalizedAmount(left._amount + right._amount);
    }

    /// <summary>
    /// Subtracts one monetary value from another, where both are denominated in the same currency.
    /// </summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The difference <c>left - right</c>.</returns>
    /// <exception cref="OverflowException">
    /// The difference falls outside the range of <see cref="decimal" />.
    /// </exception>
    public static Money<TCurrency> operator -(Money<TCurrency> left, Money<TCurrency> right)
    {
        return Money<TCurrency>.FromNormalizedAmount(left._amount - right._amount);
    }

    /// <summary>
    /// Negates a monetary value.
    /// </summary>
    /// <param name="value">The amount to negate.</param>
    /// <returns>A <see cref="Money{TCurrency}" /> whose amount is the negation of <paramref name="value" />.</returns>
    /// <exception cref="OverflowException">
    /// The negated value falls outside the range of <see cref="decimal" />.
    /// </exception>
    public static Money<TCurrency> operator -(Money<TCurrency> value)
    {
        return Money<TCurrency>.FromNormalizedAmount(-value._amount);
    }

    /// <summary>
    /// Returns the operand unchanged.
    /// </summary>
    /// <param name="value">The amount.</param>
    /// <returns>The unchanged <paramref name="value" />.</returns>
    public static Money<TCurrency> operator +(Money<TCurrency> value)
    {
        return value;
    }

    /// <summary>
    /// Multiplies a monetary value by a scalar, rounding the result to the currency's minor-unit precision using
    /// banker's rounding (<see cref="MidpointRounding.ToEven" />).
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar multiplier.</param>
    /// <returns>The product, rounded to <c>TCurrency.MinorUnits</c> using banker's rounding.</returns>
    /// <exception cref="OverflowException">The product falls outside the range of <see cref="decimal" />.</exception>
    /// <remarks>
    /// Every scalar multiplication rounds at the call site, so a chain of multiplications can accumulate rounding
    /// error. For chains where the intermediate precision matters (compound interest, tax stacking, rate-conversion
    /// percentages), perform the calculation through <see cref="ToFraction" /> and snap to
    /// <see cref="Money{TCurrency}" /> only once, at the end, via
    /// <see cref="FromFraction(Bodu.Numerics.Fraction{System.Numerics.BigInteger}, MidpointRounding)" /> or the
    /// <see cref="MultiplyExact(Bodu.Numerics.Fraction{System.Numerics.BigInteger}, MidpointRounding)" /> shortcut. To
    /// control the rounding rule on a single scalar multiplication, use
    /// <see cref="Multiply(decimal, MidpointRounding)" /> in place of the operator.
    /// </remarks>
    public static Money<TCurrency> operator *(Money<TCurrency> left, decimal right)
    {
        return new Money<TCurrency>(left._amount * right);
    }

    /// <summary>
    /// Multiplies a scalar by a monetary value, rounding the result to the currency's minor-unit precision using
    /// banker's rounding (<see cref="MidpointRounding.ToEven" />).
    /// </summary>
    /// <param name="left">The scalar multiplier.</param>
    /// <param name="right">The amount.</param>
    /// <returns>The product, rounded to <c>TCurrency.MinorUnits</c> using banker's rounding.</returns>
    /// <exception cref="OverflowException">The product falls outside the range of <see cref="decimal" />.</exception>
    public static Money<TCurrency> operator *(decimal left, Money<TCurrency> right)
    {
        return new Money<TCurrency>(left * right._amount);
    }

    /// <summary>
    /// Divides a monetary value by a scalar, rounding the result to the currency's minor-unit precision using banker's
    /// rounding (<see cref="MidpointRounding.ToEven" />).
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar divisor.</param>
    /// <returns>The quotient, rounded to <c>TCurrency.MinorUnits</c> using banker's rounding.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> is zero.</exception>
    /// <exception cref="OverflowException">The quotient falls outside the range of <see cref="decimal" />.</exception>
    /// <remarks>
    /// <para>
    /// Scalar division rounds at every call. To divide an amount into shares while preserving the total exactly, use
    /// <see cref="Money{TCurrency}.Allocate(int)" /> or <see cref="Money{TCurrency}.Allocate(ReadOnlySpan{decimal})" />
    /// .
    /// </para>
    /// <para>
    /// For chains involving division that must avoid per-step rounding drift, perform the calculation through
    /// <see cref="ToFraction" /> and snap back to <see cref="Money{TCurrency}" /> only at the final step via
    /// <see cref="FromFraction(Bodu.Numerics.Fraction{System.Numerics.BigInteger}, MidpointRounding)" />. To control
    /// the rounding rule on a single scalar division, use <see cref="Divide(decimal, MidpointRounding)" /> in place of
    /// the operator.
    /// </para>
    /// </remarks>
    public static Money<TCurrency> operator /(Money<TCurrency> left, decimal right)
    {
        return new Money<TCurrency>(left._amount / right);
    }

    /// <summary>
    /// Returns the dimensionless ratio of two monetary values denominated in the same currency.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The dimensionless ratio <c>left.Amount / right.Amount</c>.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> has an amount of zero.</exception>
    /// <remarks>
    /// Equivalent to <see cref="RatioTo(Money{TCurrency})" />; use the named method when the operator is ambiguous in
    /// domain code or when the ratio is the primary computation being expressed.
    /// </remarks>
    public static decimal operator /(Money<TCurrency> left, Money<TCurrency> right)
    {
        return left.RatioTo(right);
    }

    /// <summary>
    /// Multiplies this amount by <paramref name="multiplier" /> using banker's rounding, identical to the <c>*</c>
    /// operator.
    /// </summary>
    /// <param name="multiplier">The scalar multiplier.</param>
    /// <returns>The product, rounded to <c>TCurrency.MinorUnits</c> using banker's rounding.</returns>
    /// <exception cref="OverflowException">The product falls outside the range of <see cref="decimal" />.</exception>
    /// <remarks>
    /// Use the <see cref="Multiply(decimal, MidpointRounding)" /> overload when a rounding rule other than banker's
    /// rounding is required.
    /// </remarks>
    public Money<TCurrency> Multiply(decimal multiplier) =>
        new(_amount * multiplier);

    /// <summary>
    /// Multiplies this amount by <paramref name="multiplier" /> and rounds the result to the currency's minor-unit
    /// precision using the supplied rule.
    /// </summary>
    /// <param name="multiplier">The scalar multiplier.</param>
    /// <param name="rounding">The midpoint-rounding rule applied to the product.</param>
    /// <returns>The product, rounded to <c>TCurrency.MinorUnits</c> using <paramref name="rounding" />.</returns>
    /// <exception cref="OverflowException">The product falls outside the range of <see cref="decimal" />.</exception>
    /// <remarks>
    /// Use this overload when a non-default rounding rule (for example, <see cref="MidpointRounding.AwayFromZero" />
    /// for retail-tax workflows) must be applied. The <c>*</c> operator forces banker's rounding because operator
    /// signatures cannot accept a rounding-mode parameter.
    /// </remarks>
    public Money<TCurrency> Multiply(decimal multiplier, MidpointRounding rounding) =>
        new(_amount * multiplier, rounding);

    /// <summary>
    /// Divides this amount by <paramref name="divisor" /> using banker's rounding, identical to the <c>/</c> operator.
    /// </summary>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The quotient, rounded to <c>TCurrency.MinorUnits</c> using banker's rounding.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="divisor" /> is zero.</exception>
    /// <exception cref="OverflowException">The quotient falls outside the range of <see cref="decimal" />.</exception>
    public Money<TCurrency> Divide(decimal divisor) =>
        new(_amount / divisor);

    /// <summary>
    /// Divides this amount by <paramref name="divisor" /> and rounds the result to the currency's minor-unit precision
    /// using the supplied rule.
    /// </summary>
    /// <param name="divisor">The scalar divisor.</param>
    /// <param name="rounding">The midpoint-rounding rule applied to the quotient.</param>
    /// <returns>The quotient, rounded to <c>TCurrency.MinorUnits</c> using <paramref name="rounding" />.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="divisor" /> is zero.</exception>
    /// <exception cref="OverflowException">The quotient falls outside the range of <see cref="decimal" />.</exception>
    public Money<TCurrency> Divide(decimal divisor, MidpointRounding rounding) =>
        new(_amount / divisor, rounding);

    /// <summary>
    /// Returns the dimensionless ratio of this amount to <paramref name="other" />.
    /// </summary>
    /// <param name="other">The denominator amount, in the same currency.</param>
    /// <returns>The dimensionless ratio <c>this.Amount / other.Amount</c>.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="other" /> has an amount of zero.</exception>
    /// <remarks>
    /// Provides a readable named alternative to the <c>Money&lt;TCurrency&gt; / Money&lt;TCurrency&gt;</c> operator,
    /// which returns the same value. Both forms are equivalent.
    /// </remarks>
    public decimal RatioTo(Money<TCurrency> other) =>
        _amount / other._amount;

    /// <summary>
    /// Determines whether one monetary value is strictly less than another in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="left" /> is less than <paramref name="right" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool operator <(Money<TCurrency> left, Money<TCurrency> right)
    {
        return left._amount < right._amount;
    }

    /// <summary>
    /// Determines whether one monetary value is less than or equal to another in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="left" /> is less than or equal to <paramref name="right" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    public static bool operator <=(Money<TCurrency> left, Money<TCurrency> right)
    {
        return left._amount <= right._amount;
    }

    /// <summary>
    /// Determines whether one monetary value is strictly greater than another in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="left" /> is greater than <paramref name="right" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool operator >(Money<TCurrency> left, Money<TCurrency> right)
    {
        return left._amount > right._amount;
    }

    /// <summary>
    /// Determines whether one monetary value is greater than or equal to another in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="left" /> is greater than or equal to <paramref name="right" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    public static bool operator >=(Money<TCurrency> left, Money<TCurrency> right)
    {
        return left._amount >= right._amount;
    }
}
