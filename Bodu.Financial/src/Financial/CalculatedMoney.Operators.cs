// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoney.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

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
    public static CalculatedMoney operator -(CalculatedMoney value) =>
        value.WithAmount(-value._amount);

    /// <summary>
    /// Returns the operand unchanged.
    /// </summary>
    /// <param name="value">The amount.</param>
    /// <returns>The unchanged value.</returns>
    public static CalculatedMoney operator +(CalculatedMoney value) =>
        value;

    /// <summary>
    /// Multiplies a high-precision amount by a scalar without rounding.
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar.</param>
    /// <returns>The full-precision product.</returns>
    public static CalculatedMoney operator *(CalculatedMoney left, decimal right) =>
        left.WithAmount(left._amount * right);

    /// <summary>
    /// Multiplies a scalar by a high-precision amount without rounding.
    /// </summary>
    /// <param name="left">The scalar.</param>
    /// <param name="right">The amount.</param>
    /// <returns>The full-precision product.</returns>
    public static CalculatedMoney operator *(decimal left, CalculatedMoney right) =>
        right.WithAmount(left * right._amount);

    /// <summary>
    /// Divides a high-precision amount by a scalar without rounding.
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar divisor.</param>
    /// <returns>The full-precision quotient.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> is zero.</exception>
    public static CalculatedMoney operator /(CalculatedMoney left, decimal right) =>
        left.WithAmount(left._amount / right);

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
    /// Asserts that both operands carry a non-empty ISO code and that the two codes match.
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <exception cref="InvalidOperationException">
    /// Either operand carries no ISO code, or the two codes differ.
    /// </exception>
    private static void EnsureSameCurrency(CalculatedMoney left, CalculatedMoney right)
    {
        if (string.IsNullOrEmpty(left.IsoCode) || string.IsNullOrEmpty(right.IsoCode))
            throw new InvalidOperationException(FinancialResourceStrings.Op_Invalid_MoneyRequiresCurrency);

        if (!string.Equals(left.IsoCode, right.IsoCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Op_Invalid_MoneySameCurrencyRequired, left.IsoCode, right.IsoCode));
        }
    }
}
