// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBag.DatedConversion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    public Money<TTarget> ConvertTo<TTarget>(IDatedExchangeRateProvider rates, DateOnly date, ExchangeRateLookupOptions? options = null)
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
        IDatedExchangeRateProvider rates,
        DateOnly date,
        ExchangeRateLookupOptions? options,
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
        IDatedExchangeRateProvider rates,
        DateOnly date,
        ExchangeRateLookupOptions? options,
        MoneyBagConversionRoundingPolicy policy = MoneyBagConversionRoundingPolicy.SumRawThenRound)
        where TTarget : ICurrency
    {
        ThrowHelper.ThrowIfNull(rates);

        FinancialThrowHelper.ThrowIfMoneyBagRoundingPolicyUndefined(policy);

        string targetIso = CurrencyMetadata<TTarget>.Value.IsoCode;
        List<MoneyBagConversionLine> lines = new(_balances.Count);

        // Enumerate in ISO-lexicographic order so per-line audit output is stable and matches bag iteration.
        IEnumerable<KeyValuePair<string, decimal>> ordered =
            _balances.OrderBy(p => p.Key, StringComparer.Ordinal);

        decimal rawTotal = 0m;
        Money<TTarget> roundedTotal = Money<TTarget>.Zero;
        foreach (KeyValuePair<string, decimal> entry in ordered)
        {
            decimal raw;
            ExchangeRateLookupResult? lookup;
            if (string.Equals(entry.Key, targetIso, StringComparison.Ordinal))
            {
                raw = entry.Value;
                lookup = null;
            }
            else
            {
                ExchangeRateLookupResult resolved = rates.GetRate(entry.Key, targetIso, date, options);
                lookup = resolved;
                raw = entry.Value * resolved.Rate.Rate;
            }

            lines.Add(new MoneyBagConversionLine(entry.Key, entry.Value, lookup, raw));

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

/// <summary>
/// Captures the per-line audit metadata produced by
/// <see cref="MoneyBag.ConvertToWithAudit{TTarget}(IDatedExchangeRateProvider, DateOnly, ExchangeRateLookupOptions?, MoneyBagConversionRoundingPolicy)" />
/// for a single source currency in the bag.
/// </summary>
/// <param name="SourceIsoCode">The source-currency ISO code for this line.</param>
/// <param name="SourceAmount">The bag's raw decimal balance for that source.</param>
/// <param name="Rate">
/// The exchange-rate lookup result used for the conversion, or <see langword="null" /> when the source already matches
/// the target currency (identity pass-through).
/// </param>
/// <param name="RawConvertedAmount">
/// The unrounded contribution to the aggregated total — <c>SourceAmount × Rate</c> for cross-currency lines or
/// <c>SourceAmount</c> for the identity pass-through.
/// </param>
public readonly record struct MoneyBagConversionLine(
    string SourceIsoCode,
    decimal SourceAmount,
    ExchangeRateLookupResult? Rate,
    decimal RawConvertedAmount);

/// <summary>
/// Bundles the aggregated total produced by a dated bag conversion with the full per-line audit trail.
/// </summary>
/// <typeparam name="TTarget">The destination currency type.</typeparam>
/// <param name="Total">The aggregated total in the destination currency.</param>
/// <param name="Lines">One line per source currency in the bag, in ISO-lexicographic order.</param>
public readonly record struct MoneyBagConversionAudit<TTarget>(
    Money<TTarget> Total,
    IReadOnlyList<MoneyBagConversionLine> Lines)
    where TTarget : ICurrency;
