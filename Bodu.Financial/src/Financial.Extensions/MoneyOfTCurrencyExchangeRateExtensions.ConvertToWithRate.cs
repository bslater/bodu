// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExchangeRateExtensions.ConvertToWithRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Extensions;

public static partial class MoneyOfTCurrencyExchangeRateExtensions
{
    /// <summary>
    /// Converts <paramref name="amount" /> to <typeparamref name="TTarget" /> and additionally returns the full
    /// <see cref="RateLookupResult" /> used so callers can audit the selected rate.
    /// </summary>
    /// <typeparam name="TSource">The source currency.</typeparam>
    /// <typeparam name="TTarget">The destination currency.</typeparam>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="provider">The dated provider that resolves the exchange rate.</param>
    /// <param name="date">The valuation date.</param>
    /// <param name="options">The lookup rules to apply.</param>
    /// <param name="rounding">
    /// The rounding mode applied to the converted amount. Defaults to <see cref="MidpointRounding.ToEven" />.
    /// </param>
    /// <returns>
    /// A <see cref="MoneyConversionResult{TSource, TTarget}" /> containing the source, target, and rate metadata.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if no rate is available for the requested pair under the supplied options.
    /// </exception>
    public static MoneyConversionResult<TSource, TTarget> ConvertToWithRate<TSource, TTarget>(
        this Money<TSource> amount,
        IDatedRateProvider provider,
        DateOnly date,
        RateLookupOptions? options = null,
        MidpointRounding rounding = MidpointRounding.ToEven)
        where TSource : ICurrency
        where TTarget : ICurrency
    {
        ThrowHelper.ThrowIfNull(provider);

        RateLookupResult lookup = provider.GetRate(
            Money<TSource>.IsoCode,
            Money<TTarget>.IsoCode,
            date,
            options);

        Money<TTarget> target = amount.Convert<TTarget>(lookup.Rate.Rate, rounding);
        return new MoneyConversionResult<TSource, TTarget>(amount, target, lookup);
    }
}
