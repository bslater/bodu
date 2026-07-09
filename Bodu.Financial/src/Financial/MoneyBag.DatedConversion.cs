// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBag.DatedConversion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial;

public sealed partial class MoneyBag
{
    /// <summary>
    /// Converts the entire bag to a single target currency by resolving every non-target balance through a dated
    /// provider at the supplied valuation date and options. Uses the
    /// <see cref="MoneyBagConversionRoundingPolicy.SumRawThenRound" /> policy.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="rates">The dated provider that resolves each rate.</param>
    /// <param name="date">The valuation date supplied to every lookup.</param>
    /// <param name="options">The lookup rules supplied to every lookup.</param>
    /// <returns>The aggregated <see cref="Money{TTarget}" /> total.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rates" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">
    /// No rate is available for one of the bag's currencies under <paramref name="options" />.
    /// </exception>
    public Money<TTarget> ConvertTo<TTarget>(IDatedRateProvider rates, DateOnly date, RateLookupOptions? options = null)
        where TTarget : ICurrency =>
        ConvertTo<TTarget>(rates, date, options, MoneyBagConversionRoundingPolicy.SumRawThenRound);

    /// <summary>
    /// Converts the entire bag to a single target currency through a dated provider, using the supplied rounding
    /// policy.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="rates">The dated provider that resolves each rate.</param>
    /// <param name="date">The valuation date supplied to every lookup.</param>
    /// <param name="options">The lookup rules supplied to every lookup.</param>
    /// <param name="policy">The rounding policy applied during aggregation.</param>
    /// <returns>The aggregated <see cref="Money{TTarget}" /> total.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rates" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="policy" /> is not a defined value.</exception>
    /// <exception cref="KeyNotFoundException">
    /// No rate is available for one of the bag's currencies under <paramref name="options" />.
    /// </exception>
    public Money<TTarget> ConvertTo<TTarget>(
        IDatedRateProvider rates,
        DateOnly date,
        RateLookupOptions? options,
        MoneyBagConversionRoundingPolicy policy)
        where TTarget : ICurrency
    {
        ThrowHelper.ThrowIfNull(rates);

        return ConvertCore<TTarget>(
            (from, to) => rates.GetRate(from, to, date, options).Rate.Rate,
            policy);
    }

    /// <summary>
    /// Converts the entire bag to a single target currency and returns the audit metadata describing which observation
    /// was used for each per-currency line.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="rates">The dated provider that resolves each rate.</param>
    /// <param name="date">The valuation date supplied to every lookup.</param>
    /// <param name="options">The lookup rules supplied to every lookup.</param>
    /// <param name="policy">The rounding policy applied during aggregation.</param>
    /// <returns>
    /// A <see cref="MoneyBagConversionAudit{TTarget}" /> containing the aggregated total and one
    /// <see cref="MoneyBagConversionLine" /> per source currency. Lines for balances already in
    /// <typeparamref name="TTarget" /> report a <see langword="null" /> rate (identity pass-through).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="rates" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="policy" /> is not a defined value.</exception>
    /// <exception cref="KeyNotFoundException">
    /// No rate is available for one of the bag's currencies under <paramref name="options" />.
    /// </exception>
    public MoneyBagConversionAudit<TTarget> ConvertToWithAudit<TTarget>(
        IDatedRateProvider rates,
        DateOnly date,
        RateLookupOptions? options,
        MoneyBagConversionRoundingPolicy policy = MoneyBagConversionRoundingPolicy.SumRawThenRound)
        where TTarget : ICurrency
    {
        ThrowHelper.ThrowIfNull(rates);

        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);

        CurrencyCode targetCode = CurrencyMetadata<TTarget>.Value.Code;
        string targetIso = CurrencyMetadata<TTarget>.Value.IsoCode;
        List<MoneyBagConversionLine> lines = new(_balances.Count);

        // The backing store is already kept in ISO-code lexicographic order, so per-line audit output is stable and
        // matches bag iteration.
        decimal rawTotal = 0m;
        Money<TTarget> roundedTotal = Money<TTarget>.Zero;
        foreach (KeyValuePair<CurrencyCode, decimal> entry in _balances)
        {
            string sourceIso = entry.Key.ToString();
            decimal raw;
            RateLookupResult? lookup;
            if (entry.Key == targetCode)
            {
                raw = entry.Value;
                lookup = null;
            }
            else
            {
                RateLookupResult resolved = rates.GetRate(sourceIso, targetIso, date, options);
                lookup = resolved;
                raw = entry.Value * resolved.Rate.Rate;
            }

            lines.Add(new MoneyBagConversionLine(sourceIso, entry.Value, lookup, raw));

            if (policy == MoneyBagConversionRoundingPolicy.SumRawThenRound)
                rawTotal += raw;
            else
                roundedTotal += new Money<TTarget>(raw);
        }

        Money<TTarget> total = policy == MoneyBagConversionRoundingPolicy.SumRawThenRound
            ? new Money<TTarget>(rawTotal)
            : roundedTotal;

        return new MoneyBagConversionAudit<TTarget>(total, lines);
    }
}
