// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.CashRounding.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public readonly partial struct Money<TCurrency>
{
    /// <summary>
    /// Gets the smallest cash denomination of <typeparamref name="TCurrency" />, expressed in the major unit.
    /// </summary>
    /// <returns>
    /// The cash-rounding increment in the major unit (for example, <c>0.05m</c> for CHF), or <c>0m</c> when the
    /// currency does not require special cash rounding beyond its <see cref="MinorUnits" /> precision.
    /// </returns>
    /// <remarks>
    /// A return value of <c>0m</c> indicates that cash totals use the same precision as electronic totals, and
    /// <see cref="RoundToCash(MidpointRounding)" /> is a no-op for the currency.
    /// </remarks>
    public static decimal CashRoundingIncrement =>
        CurrencyMetadata<TCurrency>.Value.CashRoundingIncrement;

    /// <summary>
    /// Rounds this amount to the nearest physical cash denomination of <typeparamref name="TCurrency" /> using
    /// banker's rounding.
    /// </summary>
    /// <returns>
    /// A <see cref="Money{TCurrency}" /> whose amount is the nearest multiple of
    /// <see cref="CashRoundingIncrement" />, or this instance unchanged when the currency does not declare a
    /// cash-rounding increment.
    /// </returns>
    /// <remarks>
    /// Cash rounding applies to physical cash totals only; electronic transactions retain the full
    /// <see cref="MinorUnits" /> precision. For Switzerland (CHF, 5-rappen rounding), Canada (CAD, 5-cent cash
    /// rounding since 2013), Australia (AUD, 5-cent cash rounding since 1992), and other currencies whose
    /// smallest circulating coin is larger than <c>10^-MinorUnits</c>, this method snaps to the cash
    /// denomination — for example, <c>CHF 12.34</c> rounds to <c>CHF 12.35</c>.
    /// </remarks>
    public Money<TCurrency> RoundToCash() =>
        RoundToCash(MidpointRounding.ToEven);

    /// <summary>
    /// Rounds this amount to the nearest physical cash denomination of <typeparamref name="TCurrency" /> using
    /// the specified midpoint-rounding rule.
    /// </summary>
    /// <param name="rounding">The midpoint-rounding rule applied when the amount sits exactly between two cash denominations.</param>
    /// <returns>
    /// A <see cref="Money{TCurrency}" /> whose amount is the nearest multiple of
    /// <see cref="CashRoundingIncrement" />, or this instance unchanged when the currency does not declare a
    /// cash-rounding increment.
    /// </returns>
    public Money<TCurrency> RoundToCash(MidpointRounding rounding)
    {
        CurrencyMetadataDescriptor metadata = CurrencyMetadata<TCurrency>.Value;
        decimal increment = metadata.CashRoundingIncrement;
        if (increment == 0m)
            return this;

        // increment is validated to be representable at MinorUnits, so multiplier × increment is also at
        // MinorUnit precision and can take the FromNormalizedAmount fast path without a second rounding step.
        decimal multiplier = decimal.Round(_amount / increment, 0, rounding);
        return FromNormalizedAmount(multiplier * increment);
    }

    /// <summary>
    /// Gets a value indicating whether <typeparamref name="TCurrency" /> has been demonetized.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the currency is a historic predecessor; otherwise <see langword="false" />.
    /// </returns>
    public static bool IsHistoric =>
        CurrencyMetadata<TCurrency>.Value.IsHistoric;

    /// <summary>
    /// Gets the date <typeparamref name="TCurrency" /> was withdrawn from circulation, if known.
    /// </summary>
    /// <returns>The demonetization date, or <see langword="null" /> when the currency is active or the date is unknown.</returns>
    public static DateOnly? DemonetizedOn =>
        CurrencyMetadata<TCurrency>.Value.DemonetizedOn;

    /// <summary>
    /// Gets the ISO 4217 alphabetic code of the currency that replaced <typeparamref name="TCurrency" />, when
    /// applicable.
    /// </summary>
    /// <returns>
    /// The three-letter ISO code of the successor currency, or <see langword="null" /> when there is no defined
    /// successor.
    /// </returns>
    public static string? SuccessorIsoCode =>
        CurrencyMetadata<TCurrency>.Value.SuccessorIsoCode;
}
