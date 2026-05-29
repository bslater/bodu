// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Conversion.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Money<TCurrency>
{
    /// <summary>
    /// Converts this amount to <typeparamref name="TTarget" /> at the supplied exchange rate, rounding the result
    /// to the minor-unit precision of <typeparamref name="TTarget" />.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency.</typeparam>
    /// <param name="exchangeRate">
    /// The exchange rate, expressed as units of <typeparamref name="TTarget" /> per single unit of
    /// <typeparamref name="TCurrency" />.
    /// </param>
    /// <param name="rounding">The midpoint-rounding rule applied when narrowing to the target precision.</param>
    /// <returns>The converted monetary amount in <typeparamref name="TTarget" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="exchangeRate" /> is negative.</exception>
    public Money<TTarget> Convert<TTarget>(decimal exchangeRate, MidpointRounding rounding = MidpointRounding.ToEven)
        where TTarget : ICurrency
    {
        ThrowHelper.ThrowIfNegative(exchangeRate);

        return new Money<TTarget>(_amount * exchangeRate, rounding);
    }

    /// <summary>
    /// Returns a new <see cref="Money{TCurrency}" /> with the amount rounded to <paramref name="decimals" />
    /// fractional digits using the specified rule.
    /// </summary>
    /// <param name="decimals">The number of fractional digits to keep. Must be non-negative and not greater than the currency's <see cref="MinorUnits" />.</param>
    /// <param name="rounding">The midpoint-rounding rule.</param>
    /// <returns>The rounded amount.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="decimals" /> is negative or greater than <see cref="MinorUnits" />.
    /// </exception>
    /// <remarks>
    /// Use this to coarsen below the currency's natural precision — for example, rounding USD to whole dollars
    /// before display. Rounding above <see cref="MinorUnits" /> has no effect because the stored amount is already
    /// limited to the minor-unit precision.
    /// </remarks>
    public Money<TCurrency> Round(int decimals, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        ThrowHelper.ThrowIfNegative(decimals);
        ThrowHelper.ThrowIfGreaterThan(decimals, TCurrency.MinorUnits);

        return Money<TCurrency>.FromNormalizedAmount(decimal.Round(_amount, decimals, rounding));
    }

    /// <summary>
    /// Converts this amount to an exact <see cref="Fraction{T}" /> over <see cref="BigInteger" /> for arithmetic
    /// that must not round at each step.
    /// </summary>
    /// <returns>The amount as an exact rational value.</returns>
    /// <remarks>
    /// Use <see cref="ToFraction" /> together with <see cref="FromFraction(Fraction{BigInteger}, MidpointRounding)" />
    /// to defer rounding until the end of a calculation chain. For example, multiplying by an exchange-rate
    /// fraction and a fee-percentage fraction before converting back to <see cref="Money{TCurrency}" /> rounds only
    /// once on return.
    /// </remarks>
    public Fraction<BigInteger> ToFraction() =>
        Fraction<BigInteger>.FromDecimal(_amount);

    /// <summary>
    /// Creates a <see cref="Money{TCurrency}" /> from an exact rational value, rounding to the currency's minor-unit
    /// precision using the specified rule.
    /// </summary>
    /// <param name="value">The exact amount.</param>
    /// <param name="rounding">The midpoint-rounding rule.</param>
    /// <returns>The rounded monetary amount.</returns>
    /// <exception cref="OverflowException">Thrown when <paramref name="value" /> cannot be represented as a <see cref="decimal" />.</exception>
    public static Money<TCurrency> FromFraction(Fraction<BigInteger> value, MidpointRounding rounding = MidpointRounding.ToEven) =>
        new Money<TCurrency>(value.ToDecimal(), rounding);

    /// <summary>
    /// Multiplies this amount by an exact rational factor and rounds the result to the currency's minor-unit
    /// precision.
    /// </summary>
    /// <param name="factor">The exact multiplier.</param>
    /// <param name="rounding">The midpoint-rounding rule.</param>
    /// <returns>The product rounded to <c>TCurrency.MinorUnits</c>.</returns>
    /// <exception cref="OverflowException">Thrown when the result cannot be represented as a <see cref="decimal" />.</exception>
    /// <remarks>
    /// Equivalent to <c>FromFraction(ToFraction() * factor, rounding)</c> but expresses the common
    /// multiply-then-round pattern in a single call.
    /// </remarks>
    public Money<TCurrency> MultiplyExact(Fraction<BigInteger> factor, MidpointRounding rounding = MidpointRounding.ToEven) =>
        FromFraction(ToFraction() * factor, rounding);
}
