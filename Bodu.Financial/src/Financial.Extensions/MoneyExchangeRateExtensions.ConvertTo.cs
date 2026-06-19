// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyExchangeRateExtensions.ConvertTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Extensions;

public static partial class MoneyExchangeRateExtensions
{
    /// <summary>
    /// Converts <paramref name="amount" /> to <paramref name="targetIsoCode" /> using the rate resolved by
    /// <paramref name="provider" /> for <paramref name="date" /> under <paramref name="options" />.
    /// </summary>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="provider">The dated provider that resolves the exchange rate.</param>
    /// <param name="targetIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The valuation date.</param>
    /// <param name="options">The lookup rules to apply.</param>
    /// <param name="rounding">
    /// The rounding mode applied at the destination precision. Defaults to <see cref="MidpointRounding.ToEven" />.
    /// </param>
    /// <returns>The converted amount as a <see cref="Money" /> tagged with <paramref name="targetIsoCode" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="provider" /> or <paramref name="targetIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="targetIsoCode" /> is not a three-character uppercase ISO-style code.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No rate is available for the requested pair under <paramref name="options" />.
    /// </exception>
    public static Money ConvertTo(
        this Money amount,
        IDatedExchangeRateProvider provider,
        string targetIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options = null,
        MidpointRounding rounding = MidpointRounding.ToEven)
    {
        ThrowHelper.ThrowIfNull(provider);

        ExchangeRateLookupResult lookup = provider.GetRate(amount.Code.ToString(), targetIsoCode, date, options);
        return new Money(amount.Amount * lookup.Rate.Rate, CurrencyInfo.ParseCurrencyCode(targetIsoCode), rounding);
    }

    /// <summary>
    /// Converts <paramref name="amount" /> to a strongly-typed <see cref="Money{TTarget}" /> using the rate resolved by
    /// <paramref name="provider" /> for <paramref name="date" /> under <paramref name="options" />.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="provider">The dated provider that resolves the exchange rate.</param>
    /// <param name="date">The valuation date.</param>
    /// <param name="options">The lookup rules to apply.</param>
    /// <param name="rounding">
    /// The rounding mode applied at <typeparamref name="TTarget" />'s precision. Defaults to
    /// <see cref="MidpointRounding.ToEven" />.
    /// </param>
    /// <returns>The converted typed monetary value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="provider" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">
    /// No rate is available for the requested pair under <paramref name="options" />.
    /// </exception>
    public static Money<TTarget> ConvertTo<TTarget>(
        this Money amount,
        IDatedExchangeRateProvider provider,
        DateOnly date,
        ExchangeRateLookupOptions? options = null,
        MidpointRounding rounding = MidpointRounding.ToEven)
        where TTarget : ICurrency
    {
        ThrowHelper.ThrowIfNull(provider);

        ExchangeRateLookupResult lookup = provider.GetRate(amount.Code.ToString(), Money<TTarget>.IsoCode, date, options);
        return new Money<TTarget>(amount.Amount * lookup.Rate.Rate, rounding);
    }
}
