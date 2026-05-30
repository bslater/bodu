// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValue.Operators.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct MoneyValue
{
    /// <summary>
    /// Adds two monetary values denominated in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// The operands have different ISO codes.
    /// </exception>
    /// <exception cref="OverflowException">
    /// The sum falls outside the range of <see cref="decimal" />.
    /// </exception>
    public static MoneyValue operator +(MoneyValue left, MoneyValue right)
    {
        EnsureSameCurrency(left, right);
        return FromNormalized(left._amount + right._amount, left.IsoCode);
    }

    /// <summary>
    /// Subtracts one monetary value from another.
    /// </summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The difference.</returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    /// <exception cref="OverflowException">The difference falls outside the range of <see cref="decimal" />.</exception>
    public static MoneyValue operator -(MoneyValue left, MoneyValue right)
    {
        EnsureSameCurrency(left, right);
        return FromNormalized(left._amount - right._amount, left.IsoCode);
    }

    /// <summary>
    /// Negates a monetary value.
    /// </summary>
    /// <param name="value">The amount to negate.</param>
    /// <returns>A <see cref="MoneyValue" /> with the same ISO code and negated amount.</returns>
    public static MoneyValue operator -(MoneyValue value) =>
        FromNormalized(-value._amount, value.IsoCode);

    /// <summary>
    /// Returns the operand unchanged.
    /// </summary>
    /// <param name="value">The amount.</param>
    /// <returns>The unchanged value.</returns>
    public static MoneyValue operator +(MoneyValue value) =>
        value;

    /// <summary>
    /// Multiplies a monetary value by a scalar, rounding to the currency's minor-unit precision.
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar.</param>
    /// <returns>The product.</returns>
    public static MoneyValue operator *(MoneyValue left, decimal right) =>
        new(left._amount * right, left.IsoCode);

    /// <summary>
    /// Multiplies a scalar by a monetary value.
    /// </summary>
    /// <param name="left">The scalar.</param>
    /// <param name="right">The amount.</param>
    /// <returns>The product.</returns>
    public static MoneyValue operator *(decimal left, MoneyValue right) =>
        new(left * right._amount, right.IsoCode);

    /// <summary>
    /// Divides a monetary value by a scalar.
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar divisor.</param>
    /// <returns>The quotient.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> is zero.</exception>
    public static MoneyValue operator /(MoneyValue left, decimal right) =>
        new(left._amount / right, left.IsoCode);

    /// <summary>
    /// Returns the dimensionless ratio of two monetary values in the same currency.
    /// </summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The ratio.</returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> has an amount of zero.</exception>
    public static decimal operator /(MoneyValue left, MoneyValue right)
    {
        EnsureSameCurrency(left, right);
        return left._amount / right._amount;
    }

    /// <summary>
    /// Compares two amounts in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> is less than <paramref name="right" />; otherwise <see langword="false" />.</returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    public static bool operator <(MoneyValue left, MoneyValue right)
    {
        EnsureSameCurrency(left, right);
        return left._amount < right._amount;
    }

    /// <summary>
    /// Compares two amounts in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> is less than or equal to <paramref name="right" />; otherwise <see langword="false" />.</returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    public static bool operator <=(MoneyValue left, MoneyValue right)
    {
        EnsureSameCurrency(left, right);
        return left._amount <= right._amount;
    }

    /// <summary>
    /// Compares two amounts in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> is greater than <paramref name="right" />; otherwise <see langword="false" />.</returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    public static bool operator >(MoneyValue left, MoneyValue right)
    {
        EnsureSameCurrency(left, right);
        return left._amount > right._amount;
    }

    /// <summary>
    /// Compares two amounts in the same currency.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns><see langword="true" /> when <paramref name="left" /> is greater than or equal to <paramref name="right" />; otherwise <see langword="false" />.</returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    public static bool operator >=(MoneyValue left, MoneyValue right)
    {
        EnsureSameCurrency(left, right);
        return left._amount >= right._amount;
    }

    /// <summary>
    /// Asserts that two operands share the same ISO code, throwing otherwise.
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    private static void EnsureSameCurrency(MoneyValue left, MoneyValue right)
    {
        if (!string.Equals(left.IsoCode, right.IsoCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"MoneyValue operations require the same currency; got '{left.IsoCode}' and '{right.IsoCode}'.");
        }
    }
}
