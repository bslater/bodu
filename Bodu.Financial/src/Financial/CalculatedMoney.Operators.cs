// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoney.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public readonly partial struct CalculatedMoney
{
    /// <summary>
    /// Adds two high-precision amounts denominated in the same currency, preserving full precision.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>The sum.</returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    public static CalculatedMoney operator +(CalculatedMoney left, CalculatedMoney right)
    {
        EnsureSameCurrency(left, right);
        return left.WithAmount(left._amount + right._amount);
    }

    /// <summary>
    /// Subtracts one high-precision amount from another in the same currency, preserving full precision.
    /// </summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The difference.</returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    public static CalculatedMoney operator -(CalculatedMoney left, CalculatedMoney right)
    {
        EnsureSameCurrency(left, right);
        return left.WithAmount(left._amount - right._amount);
    }

    /// <summary>
    /// Negates a high-precision amount.
    /// </summary>
    /// <param name="value">The amount to negate.</param>
    /// <returns>The negated amount.</returns>
    public static CalculatedMoney operator -(CalculatedMoney value)
    {
        return value.WithAmount(-value._amount);
    }

    /// <summary>
    /// Returns the operand unchanged.
    /// </summary>
    /// <param name="value">The amount.</param>
    /// <returns>The unchanged value.</returns>
    public static CalculatedMoney operator +(CalculatedMoney value)
    {
        return value;
    }

    /// <summary>
    /// Multiplies a high-precision amount by a scalar without rounding.
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar.</param>
    /// <returns>The full-precision product.</returns>
    public static CalculatedMoney operator *(CalculatedMoney left, decimal right)
    {
        return left.WithAmount(left._amount * right);
    }

    /// <summary>
    /// Multiplies a scalar by a high-precision amount without rounding.
    /// </summary>
    /// <param name="left">The scalar.</param>
    /// <param name="right">The amount.</param>
    /// <returns>The full-precision product.</returns>
    public static CalculatedMoney operator *(decimal left, CalculatedMoney right)
    {
        return right.WithAmount(left * right._amount);
    }

    /// <summary>
    /// Divides a high-precision amount by a scalar without rounding.
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar divisor.</param>
    /// <returns>The full-precision quotient.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> is zero.</exception>
    public static CalculatedMoney operator /(CalculatedMoney left, decimal right)
    {
        return left.WithAmount(left._amount / right);
    }

    /// <summary>
    /// Multiplies this amount by <paramref name="multiplier" /> without rounding.
    /// </summary>
    /// <param name="multiplier">The scalar multiplier.</param>
    /// <returns>The full-precision product.</returns>
    public CalculatedMoney Multiply(decimal multiplier) =>
        WithAmount(_amount * multiplier);

    /// <summary>
    /// Divides this amount by <paramref name="divisor" /> without rounding.
    /// </summary>
    /// <param name="divisor">The scalar divisor.</param>
    /// <returns>The full-precision quotient.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="divisor" /> is zero.</exception>
    public CalculatedMoney Divide(decimal divisor) =>
        WithAmount(_amount / divisor);

    /// <summary>
    /// Asserts that both operands carry a currency and that the two currencies match.
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <exception cref="InvalidOperationException">
    /// Either operand carries no currency, or the two currencies differ.
    /// </exception>
    private static void EnsureSameCurrency(CalculatedMoney left, CalculatedMoney right)
    {
        if (left._code == CurrencyCode.None || right._code == CurrencyCode.None)
            throw new InvalidOperationException(FinancialResourceStrings.Op_Invalid_MoneyRequiresCurrency);

        if (left._code != right._code)
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Op_Invalid_MoneySameCurrencyRequired, left.IsoCodeOrEmpty, right.IsoCodeOrEmpty));
        }
    }
}
