// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public readonly partial struct Money
{
    /// <summary>
    /// Adds two monetary values denominated in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    /// <exception cref="OverflowException">The sum falls outside the range of <see cref="decimal" />.</exception>
    /// <remarks>
    /// When the operands report different minor-unit scales — for example a two-decimal settled amount and a
    /// six-decimal unit price — the result carries the finer (maximum) of the two scales, mirroring
    /// <see cref="decimal" /> addition semantics. The sum is exact at that scale, so no rounding occurs and the
    /// reported precision is the same regardless of operand order.
    /// </remarks>
    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.WithAdditiveAmount(right, left._amount + right._amount);
    }

    /// <summary>
    /// Subtracts one monetary value from another.
    /// </summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The difference.</returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    /// <exception cref="OverflowException">
    /// The difference falls outside the range of <see cref="decimal" />.
    /// </exception>
    /// <remarks>
    /// When the operands report different minor-unit scales, the result carries the finer (maximum) of the two scales,
    /// mirroring <see cref="decimal" /> subtraction semantics; the difference is exact at that scale and the reported
    /// precision does not depend on operand order.
    /// </remarks>
    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.WithAdditiveAmount(right, left._amount - right._amount);
    }

    /// <summary>
    /// Negates a monetary value.
    /// </summary>
    /// <param name="value">The amount to negate.</param>
    /// <returns>A <see cref="Money" /> with the same ISO code and negated amount.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="value" /> is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    public static Money operator -(Money value)
    {
        value.EnsureHasCurrency();
        return value.WithAmount(-value._amount);
    }

    /// <summary>
    /// Returns the operand unchanged.
    /// </summary>
    /// <param name="value">The amount.</param>
    /// <returns>The unchanged value.</returns>
    public static Money operator +(Money value)
    {
        return value;
    }

    /// <summary>
    /// Multiplies a monetary value by a scalar, rounding the product to the currency's minor-unit precision using
    /// banker's rounding (<see cref="MidpointRounding.ToEven" />).
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar.</param>
    /// <returns>The product, rounded to the currency's minor units using banker's rounding.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="left" /> is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    /// <remarks>
    /// Use <see cref="Multiply(decimal, MidpointRounding)" /> when a rounding rule other than banker's rounding is
    /// required; the operator cannot accept a rounding-mode parameter.
    /// </remarks>
    public static Money operator *(Money left, decimal right)
    {
        left.EnsureHasCurrency();
        return left.WithRoundedAmount(left._amount * right);
    }

    /// <summary>
    /// Multiplies a scalar by a monetary value, rounding the product to the currency's minor-unit precision using
    /// banker's rounding (<see cref="MidpointRounding.ToEven" />).
    /// </summary>
    /// <param name="left">The scalar.</param>
    /// <param name="right">The amount.</param>
    /// <returns>The product, rounded to the currency's minor units using banker's rounding.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="right" /> is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    /// <remarks>
    /// Use <see cref="Multiply(decimal, MidpointRounding)" /> when a rounding rule other than banker's rounding is
    /// required; the operator cannot accept a rounding-mode parameter.
    /// </remarks>
    public static Money operator *(decimal left, Money right)
    {
        right.EnsureHasCurrency();
        return right.WithRoundedAmount(left * right._amount);
    }

    /// <summary>
    /// Divides a monetary value by a scalar, rounding the quotient to the currency's minor-unit precision using
    /// banker's rounding (<see cref="MidpointRounding.ToEven" />).
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar divisor.</param>
    /// <returns>The quotient, rounded to the currency's minor units using banker's rounding.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="left" /> is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> is zero.</exception>
    /// <remarks>
    /// Use <see cref="Divide(decimal, MidpointRounding)" /> when a rounding rule other than banker's rounding is
    /// required; the operator cannot accept a rounding-mode parameter.
    /// </remarks>
    public static Money operator /(Money left, decimal right)
    {
        left.EnsureHasCurrency();
        return left.WithRoundedAmount(left._amount / right);
    }

    /// <summary>
    /// Multiplies this amount by <paramref name="multiplier" /> using banker's rounding, identical to the <c>*</c>
    /// operator.
    /// </summary>
    /// <param name="multiplier">The scalar multiplier.</param>
    /// <returns>The product, rounded to the currency's minor units using banker's rounding.</returns>
    /// <exception cref="InvalidOperationException">
    /// This value is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    public Money Multiply(decimal multiplier)
    {
        EnsureHasCurrency();
        return WithRoundedAmount(_amount * multiplier);
    }

    /// <summary>
    /// Multiplies this amount by <paramref name="multiplier" /> and rounds the result to the currency's minor-unit
    /// precision using the supplied rule.
    /// </summary>
    /// <param name="multiplier">The scalar multiplier.</param>
    /// <param name="rounding">The midpoint-rounding rule applied to the product.</param>
    /// <returns>The product, rounded to the currency's minor units using <paramref name="rounding" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// This value is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    /// <remarks>
    /// Use this overload when a non-default rounding rule (for example, <see cref="MidpointRounding.AwayFromZero" />
    /// for retail-tax workflows) must be applied. The <c>*</c> operator forces banker's rounding because operator
    /// signatures cannot accept a rounding-mode parameter.
    /// </remarks>
    public Money Multiply(decimal multiplier, MidpointRounding rounding)
    {
        EnsureHasCurrency();
        return WithRoundedAmount(_amount * multiplier, rounding);
    }

    /// <summary>
    /// Divides this amount by <paramref name="divisor" /> using banker's rounding, identical to the <c>/</c> operator.
    /// </summary>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The quotient, rounded to the currency's minor units using banker's rounding.</returns>
    /// <exception cref="InvalidOperationException">
    /// This value is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    /// <exception cref="DivideByZeroException"><paramref name="divisor" /> is zero.</exception>
    public Money Divide(decimal divisor)
    {
        EnsureHasCurrency();
        return WithRoundedAmount(_amount / divisor);
    }

    /// <summary>
    /// Divides this amount by <paramref name="divisor" /> and rounds the result to the currency's minor-unit precision
    /// using the supplied rule.
    /// </summary>
    /// <param name="divisor">The scalar divisor.</param>
    /// <param name="rounding">The midpoint-rounding rule applied to the quotient.</param>
    /// <returns>The quotient, rounded to the currency's minor units using <paramref name="rounding" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// This value is a default-initialised, currency-less <see cref="Money" />.
    /// </exception>
    /// <exception cref="DivideByZeroException"><paramref name="divisor" /> is zero.</exception>
    /// <remarks>
    /// Use this overload when a non-default rounding rule must be applied. The <c>/</c> operator forces banker's
    /// rounding because operator signatures cannot accept a rounding-mode parameter.
    /// </remarks>
    public Money Divide(decimal divisor, MidpointRounding rounding)
    {
        EnsureHasCurrency();
        return WithRoundedAmount(_amount / divisor, rounding);
    }

    /// <summary>
    /// Returns the dimensionless ratio of two monetary values in the same currency.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The ratio.</returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> has an amount of zero.</exception>
    public static decimal operator /(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left._amount / right._amount;
    }

    /// <summary>
    /// Compares two amounts in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="left" /> is less than <paramref name="right" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left._amount < right._amount;
    }

    /// <summary>
    /// Compares two amounts in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="left" /> is less than or equal to <paramref name="right" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left._amount <= right._amount;
    }

    /// <summary>
    /// Compares two amounts in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="left" /> is greater than <paramref name="right" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left._amount > right._amount;
    }

    /// <summary>
    /// Compares two amounts in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="left" /> is greater than or equal to <paramref name="right" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left._amount >= right._amount;
    }

    /// <summary>
    /// Asserts that both operands carry a currency (i.e. neither is a default-initialised value) and that the two
    /// currencies match.
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <exception cref="InvalidOperationException">
    /// Either operand carries no currency (a default-initialised <see cref="Money" />), or the two currencies differ.
    /// </exception>
    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left._code == CurrencyCode.None || right._code == CurrencyCode.None)
        {
            throw new InvalidOperationException(
                FinancialResourceStrings.Op_Invalid_MoneyRequiresCurrency);
        }

        if (left._code != right._code)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Op_Invalid_MoneySameCurrencyRequired,
                    left.IsoCodeOrEmpty,
                    right.IsoCodeOrEmpty));
        }
    }
}
