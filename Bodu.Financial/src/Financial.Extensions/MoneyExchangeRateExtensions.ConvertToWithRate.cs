// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyExchangeRateExtensions.ConvertToWithRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Extensions;

public static partial class MoneyExchangeRateExtensions
{
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
    /// <exception cref="KeyNotFoundException">
    /// No rate is available for the requested pair under <paramref name="options" />.
    /// </exception>
    public static (Money Target, ExchangeRateLookupResult Rate) ConvertToWithRate(
        this Money amount,
        IDatedExchangeRateProvider provider,
        string targetIsoCode,
        DateOnly date,
        ExchangeRateLookupOptions? options = null,
        MidpointRounding rounding = MidpointRounding.ToEven)
    {
        ThrowHelper.ThrowIfNull(provider);

        ExchangeRateLookupResult lookup = provider.GetRate(amount.IsoCode, targetIsoCode, date, options);
        Money target = new(amount.Amount * lookup.Rate.Rate, targetIsoCode, rounding);
        return (target, lookup);
    }
}
