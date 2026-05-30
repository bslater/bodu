// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValueExchangeRateExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Provides extension methods that resolve a dated exchange rate from an <see cref="IDatedExchangeRateProvider" />
/// and apply it to a <see cref="MoneyValue" /> — the runtime-tagged counterpart of
/// <see cref="MoneyExchangeRateExtensions" />.
/// </summary>
public static class MoneyValueExchangeRateExtensions
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
    /// <param name="rounding">The rounding mode applied at the destination precision. Defaults to <see cref="MidpointRounding.ToEven" />.</param>
    /// <returns>The converted amount as a <see cref="MoneyValue" /> tagged with <paramref name="targetIsoCode" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="provider" /> or <paramref name="targetIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="targetIsoCode" /> is not a three-character uppercase ISO-style code.
    /// </exception>
    /// <exception cref="KeyNotFoundException">No rate is available for the requested pair under <paramref name="options" />.</exception>
    public static MoneyValue ConvertTo(
        this MoneyValue amount,
        IDatedExchangeRateProvider provider,
        string targetIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions options,
        MidpointRounding rounding = MidpointRounding.ToEven)
    {
        ThrowHelper.ThrowIfNull(provider);

        ExchangeRateLookupResult lookup = provider.GetRate(amount.IsoCode, targetIsoCode, date, options);
        return new MoneyValue(amount.Amount * lookup.Rate.Rate, targetIsoCode, rounding);
    }

    /// <summary>
    /// Converts <paramref name="amount" /> to a strongly-typed <see cref="Money{TTarget}" /> using the rate
    /// resolved by <paramref name="provider" /> for <paramref name="date" /> under <paramref name="options" />.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="provider">The dated provider that resolves the exchange rate.</param>
    /// <param name="date">The valuation date.</param>
    /// <param name="options">The lookup rules to apply.</param>
    /// <param name="rounding">The rounding mode applied at <typeparamref name="TTarget" />'s precision. Defaults to <see cref="MidpointRounding.ToEven" />.</param>
    /// <returns>The converted typed monetary value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="provider" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">No rate is available for the requested pair under <paramref name="options" />.</exception>
    public static Money<TTarget> ConvertTo<TTarget>(
        this MoneyValue amount,
        IDatedExchangeRateProvider provider,
        DateOnly date,
        ExchangeRateLookupOptions options,
        MidpointRounding rounding = MidpointRounding.ToEven)
        where TTarget : ICurrency
    {
        ThrowHelper.ThrowIfNull(provider);

        ExchangeRateLookupResult lookup = provider.GetRate(amount.IsoCode, Money<TTarget>.IsoCode, date, options);
        return new Money<TTarget>(amount.Amount * lookup.Rate.Rate, rounding);
    }

    /// <summary>
    /// Converts <paramref name="amount" /> to <paramref name="targetIsoCode" /> and returns the converted value
    /// alongside the full <see cref="ExchangeRateLookupResult" /> used so callers can audit the selected rate.
    /// </summary>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="provider">The dated provider that resolves the exchange rate.</param>
    /// <param name="targetIsoCode">The destination-currency ISO code.</param>
    /// <param name="date">The valuation date.</param>
    /// <param name="options">The lookup rules to apply.</param>
    /// <param name="rounding">The rounding mode. Defaults to <see cref="MidpointRounding.ToEven" />.</param>
    /// <returns>A tuple of the converted amount and the lookup metadata that produced it.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="provider" /> or <paramref name="targetIsoCode" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="targetIsoCode" /> is not a three-character uppercase ISO-style code.
    /// </exception>
    /// <exception cref="KeyNotFoundException">No rate is available for the requested pair under <paramref name="options" />.</exception>
    public static (MoneyValue Target, ExchangeRateLookupResult Rate) ConvertToWithRate(
        this MoneyValue amount,
        IDatedExchangeRateProvider provider,
        string targetIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions options,
        MidpointRounding rounding = MidpointRounding.ToEven)
    {
        ThrowHelper.ThrowIfNull(provider);

        ExchangeRateLookupResult lookup = provider.GetRate(amount.IsoCode, targetIsoCode, date, options);
        MoneyValue target = new(amount.Amount * lookup.Rate.Rate, targetIsoCode, rounding);
        return (target, lookup);
    }
}
