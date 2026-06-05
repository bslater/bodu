// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRate.OfTBaseTQuote.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Strongly-typed companion to <see cref="ExchangeRate" /> where the base and quote currencies are domain invariants
/// encoded as type parameters.
/// </summary>
/// <typeparam name="TBase">The source-currency tag.</typeparam>
/// <typeparam name="TQuote">The destination-currency tag.</typeparam>
/// <remarks>
/// Use this typed form when an FX conversion's direction is fixed by the surrounding contract (settlement workflows,
/// ledger-to-ledger transfers, account-currency mappings). Direction errors that <see cref="ExchangeRate" /> can
/// surface only at runtime (mismatched <see cref="ExchangeRate.FromIsoCode" /> / <see cref="ExchangeRate.ToIsoCode" />
/// against the caller's expectation) become compile errors when the typed form is used. Bridge to and from the runtime
/// form via <see cref="ToRuntime" /> and <see cref="FromRuntime" />.
/// </remarks>
[DebuggerDisplay("{FromIsoCode,nq}->{ToIsoCode,nq} @ {Date,nq} = {Rate} ({Provider,nq})")]
public readonly record struct ExchangeRate<TBase, TQuote>
    where TBase : ICurrency
    where TQuote : ICurrency
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRate{TBase, TQuote}" /> struct.
    /// </summary>
    /// <param name="rate">
    /// The multiplier that converts a <typeparamref name="TBase" /> amount to <typeparamref name="TQuote" />.
    /// </param>
    /// <param name="date">The calendar date on which the rate was observed.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="isInverted">
    /// <see langword="true" /> when the rate was derived from the reverse pair; otherwise <see langword="false" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="provider" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="provider" /> is empty or white-space.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="rate" /> is zero or negative.</exception>
    public ExchangeRate(decimal rate, DateOnly date, string provider, bool isInverted = false)
    {
        FinancialThrowHelper.ThrowIfNullOrWhiteSpaceProvider(provider);
        ThrowHelper.ThrowIfZeroOrNegative(rate);

        Rate = rate;
        Date = date;
        Provider = provider;
        IsInverted = isInverted;
    }

    /// <summary>
    /// Gets the multiplier that converts a <typeparamref name="TBase" /> amount to <typeparamref name="TQuote" />.
    /// </summary>
    /// <returns>A strictly positive multiplier.</returns>
    public decimal Rate { get; }

    /// <summary>
    /// Gets the calendar date on which the rate was observed.
    /// </summary>
    /// <returns>The observation date.</returns>
    public DateOnly Date { get; }

    /// <summary>
    /// Gets the non-empty identifier of the publishing source.
    /// </summary>
    /// <returns>The provider identifier.</returns>
    public string Provider { get; }

    /// <summary>
    /// Gets a value indicating whether this rate was derived from the reverse pair.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when <see cref="Rate" /> is the reciprocal of an originally published reverse-direction
    /// rate; otherwise <see langword="false" />.
    /// </returns>
    public bool IsInverted { get; }

    /// <summary>
    /// Gets the ISO 4217 alphabetic code of the source currency, derived from <typeparamref name="TBase" />.
    /// </summary>
    /// <returns>The three-character uppercase ASCII source-currency code.</returns>
    public string FromIsoCode => CurrencyMetadata<TBase>.Value.IsoCode;

    /// <summary>
    /// Gets the ISO 4217 alphabetic code of the destination currency, derived from <typeparamref name="TQuote" />.
    /// </summary>
    /// <returns>The three-character uppercase ASCII destination-currency code.</returns>
    public string ToIsoCode => CurrencyMetadata<TQuote>.Value.IsoCode;

    /// <summary>
    /// Creates a strongly-typed exchange rate from the supplied parameters.
    /// </summary>
    /// <param name="rate">The multiplier; must be strictly positive.</param>
    /// <param name="date">The observation date.</param>
    /// <param name="provider">The non-empty source identifier.</param>
    /// <returns>The constructed rate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="provider" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="provider" /> is empty or white-space.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="rate" /> is zero or negative.</exception>
    public static ExchangeRate<TBase, TQuote> From(decimal rate, DateOnly date, string provider) =>
        new(rate, date, provider);

    /// <summary>
    /// Returns the reciprocal rate that converts in the opposite direction.
    /// </summary>
    /// <returns>
    /// An <see cref="ExchangeRate{TQuote, TBase}" /> whose <see cref="Rate" /> is <c>1 / this.Rate</c>.
    /// </returns>
    public ExchangeRate<TQuote, TBase> Inverse() =>
        new(1m / Rate, Date, Provider, !IsInverted);

    /// <summary>
    /// Bridges this typed rate to the runtime-tagged <see cref="ExchangeRate" /> record.
    /// </summary>
    /// <returns>
    /// An <see cref="ExchangeRate" /> carrying the same fields with the ISO codes inlined as strings.
    /// </returns>
    public ExchangeRate ToRuntime() =>
        new(FromIsoCode, ToIsoCode, Date, Rate, Provider, IsInverted);

    /// <summary>
    /// Adopts a runtime-tagged <see cref="ExchangeRate" /> as the typed form when the runtime ISO codes match
    /// <typeparamref name="TBase" /> and <typeparamref name="TQuote" />.
    /// </summary>
    /// <param name="rate">The runtime-tagged rate.</param>
    /// <returns>The strongly-typed equivalent.</returns>
    /// <exception cref="InvalidOperationException">
    /// The runtime rate's <see cref="ExchangeRate.FromIsoCode" /> or <see cref="ExchangeRate.ToIsoCode" /> does not
    /// match the ISO code of <typeparamref name="TBase" /> or <typeparamref name="TQuote" /> respectively.
    /// </exception>
    public static ExchangeRate<TBase, TQuote> FromRuntime(ExchangeRate rate)
    {
        var baseIso = CurrencyMetadata<TBase>.Value.IsoCode;
        var quoteIso = CurrencyMetadata<TQuote>.Value.IsoCode;

        if (!string.Equals(rate.FromIsoCode, baseIso, StringComparison.Ordinal) ||
            !string.Equals(rate.ToIsoCode, quoteIso, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.Op_Invalid_ExchangeRateRuntimeMismatch,
                    rate.FromIsoCode,
                    rate.ToIsoCode,
                    baseIso,
                    quoteIso));
        }

        return new ExchangeRate<TBase, TQuote>(rate.Rate, rate.Date, rate.Provider, rate.IsInverted);
    }

    /// <summary>
    /// Converts a <typeparamref name="TBase" /> amount to <typeparamref name="TQuote" />, rounding the result to the
    /// destination currency's minor-unit precision.
    /// </summary>
    /// <param name="amount">The amount in <typeparamref name="TBase" /> to convert.</param>
    /// <param name="rounding">The midpoint-rounding rule applied at the destination precision.</param>
    /// <returns>The converted amount in <typeparamref name="TQuote" />.</returns>
    public Money<TQuote> Convert(Money<TBase> amount, MidpointRounding rounding = MidpointRounding.ToEven) =>
        new(amount.Amount * Rate, rounding);
}
