// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExchangeRateExtensions.ConvertToWithRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Extensions;

public static partial class MoneyOfTCurrencyExchangeRateExtensions
{
    /// <summary>
    /// Converts <paramref name="amount" /> to <typeparamref name="TTarget" /> and additionally returns the full
    /// <see cref="ExchangeRateLookupResult" /> used so callers can audit the selected rate.
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
    /// A <see cref="TypedMoneyConversionResult{TSource, TTarget}" /> containing the source, target, and rate metadata.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if no rate is available for the requested pair under the supplied options.
    /// </exception>
    public static TypedMoneyConversionResult<TSource, TTarget> ConvertToWithRate<TSource, TTarget>(
        this Money<TSource> amount,
        IDatedExchangeRateProvider provider,
        DateOnly date,
        ExchangeRateLookupOptions? options = null,
        MidpointRounding rounding = MidpointRounding.ToEven)
        where TSource : ICurrency
        where TTarget : ICurrency
    {
        ThrowHelper.ThrowIfNull(provider);

        ExchangeRateLookupResult lookup = provider.GetRate(
            Money<TSource>.IsoCode,
            Money<TTarget>.IsoCode,
            date,
            options);

        Money<TTarget> target = amount.Convert<TTarget>(lookup.Rate.Rate, rounding);
        return new TypedMoneyConversionResult<TSource, TTarget>(amount, target, lookup);
    }
}
