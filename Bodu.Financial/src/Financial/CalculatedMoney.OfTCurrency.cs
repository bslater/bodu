// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoney.OfTCurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Financial;

/// <summary>
/// Represents a high-precision monetary amount denominated in <typeparamref name="TCurrency" /> whose rounding is
/// deferred until it is converted back to a settlement <see cref="Money{TCurrency}" />.
/// </summary>
/// <typeparam name="TCurrency">A type implementing <see cref="ICurrency" /> that identifies the currency.</typeparam>
/// <remarks>
/// This is the strongly-typed counterpart of <see cref="CalculatedMoney" />. Arithmetic preserves full
/// <see cref="decimal" /> precision; rounding to <c>TCurrency.MinorUnits</c> happens once, through
/// <see cref="RoundToMoney(MonetaryContext?)" />.
/// </remarks>
[DebuggerDisplay("{Amount} {IsoCode,nq} (unrounded)")]
public readonly struct CalculatedMoney<TCurrency>
    : IEquatable<CalculatedMoney<TCurrency>>
    where TCurrency : ICurrency
{
    /// <summary>
    /// The unrounded amount in the major unit of <typeparamref name="TCurrency" />.
    /// </summary>
    private readonly decimal _amount;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculatedMoney{TCurrency}" /> struct, preserving the amount's full
    /// precision.
    /// </summary>
    /// <param name="amount">
    /// The unrounded monetary amount in the major unit of <typeparamref name="TCurrency" />.
    /// </param>
    public CalculatedMoney(decimal amount)
    {
        _amount = amount;
    }

    /// <summary>
    /// Gets the ISO 4217 alphabetic code of <typeparamref name="TCurrency" />.
    /// </summary>
    /// <returns>The currency's ISO code.</returns>
    public static string IsoCode =>
        CurrencyMetadata<TCurrency>.Value.IsoCode;

    /// <summary>
    /// Gets the unrounded monetary amount in the major unit of <typeparamref name="TCurrency" />.
    /// </summary>
    /// <returns>The full-precision amount stored by this instance.</returns>
    public decimal Amount =>
        _amount;

    /// <summary>
    /// Gets a value indicating whether this amount is zero.
    /// </summary>
    /// <returns><see langword="true" /> when the amount is zero; otherwise <see langword="false" />.</returns>
    public bool IsZero =>
        _amount == 0m;

    /// <summary>
    /// Adds two high-precision amounts, preserving full precision.
    /// </summary>
    /// <param name="left">The first amount.</param>
    /// <param name="right">The second amount.</param>
    /// <returns>The sum.</returns>
    public static CalculatedMoney<TCurrency> operator +(CalculatedMoney<TCurrency> left, CalculatedMoney<TCurrency> right)
    {
        return new(left._amount + right._amount);
    }

    /// <summary>
    /// Subtracts one high-precision amount from another, preserving full precision.
    /// </summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    /// <returns>The difference.</returns>
    public static CalculatedMoney<TCurrency> operator -(CalculatedMoney<TCurrency> left, CalculatedMoney<TCurrency> right)
    {
        return new(left._amount - right._amount);
    }

    /// <summary>
    /// Negates a high-precision amount.
    /// </summary>
    /// <param name="value">The amount to negate.</param>
    /// <returns>The negated amount.</returns>
    public static CalculatedMoney<TCurrency> operator -(CalculatedMoney<TCurrency> value)
    {
        return new(-value._amount);
    }

    /// <summary>
    /// Multiplies a high-precision amount by a scalar without rounding.
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar.</param>
    /// <returns>The full-precision product.</returns>
    public static CalculatedMoney<TCurrency> operator *(CalculatedMoney<TCurrency> left, decimal right)
    {
        return new(left._amount * right);
    }

    /// <summary>
    /// Divides a high-precision amount by a scalar without rounding.
    /// </summary>
    /// <param name="left">The amount.</param>
    /// <param name="right">The scalar divisor.</param>
    /// <returns>The full-precision quotient.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="right" /> is zero.</exception>
    public static CalculatedMoney<TCurrency> operator /(CalculatedMoney<TCurrency> left, decimal right)
    {
        return new(left._amount / right);
    }

    /// <summary>
    /// Materialises this high-precision amount as a settlement <see cref="Money{TCurrency}" />, rounding to
    /// <c>TCurrency.MinorUnits</c> using <paramref name="context" />'s rounding strategy.
    /// </summary>
    /// <param name="context">
    /// The monetary context whose rounding strategy is applied; <see langword="null" /> selects
    /// <see cref="MonetaryContext.Default" />.
    /// </param>
    /// <returns>The settled <see cref="Money{TCurrency}" />.</returns>
    public Money<TCurrency> RoundToMoney(MonetaryContext? context = null)
    {
        MonetaryContext effective = context ?? MonetaryContext.Default;
        var minorUnits = CurrencyMetadata<TCurrency>.Value.MinorUnits;
        return Money<TCurrency>.FromNormalizedAmount(effective.Rounding.Round(_amount, minorUnits));
    }

    /// <summary>
    /// Materialises this high-precision amount as a settlement <see cref="Money{TCurrency}" />, rounding to
    /// <c>TCurrency.MinorUnits</c> using the supplied midpoint rule.
    /// </summary>
    /// <param name="rounding">The midpoint-rounding rule.</param>
    /// <returns>The settled <see cref="Money{TCurrency}" />.</returns>
    public Money<TCurrency> RoundToMoney(MidpointRounding rounding) =>
        RoundToMoney(MonetaryContext.Default with { Rounding = new MidpointRoundingStrategy(rounding) });

    /// <summary>
    /// Determines whether this value equals <paramref name="other" />.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns><see langword="true" /> when the amounts are equal.</returns>
    public bool Equals(CalculatedMoney<TCurrency> other) =>
        _amount == other._amount;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is CalculatedMoney<TCurrency> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _amount.GetHashCode();

    /// <summary>
    /// Determines whether two values are equal.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when equal.</returns>
    public static bool operator ==(CalculatedMoney<TCurrency> left, CalculatedMoney<TCurrency> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two values differ.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when they differ.</returns>
    public static bool operator !=(CalculatedMoney<TCurrency> left, CalculatedMoney<TCurrency> right)
    {
        return !left.Equals(right);
    }
}
