// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedRateProviderExtensions.ToFixedProviderAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Extensions;

public static partial class DatedRateProviderExtensions
{
    /// <summary>
    /// Fetches the inclusive date window for each requested pair and materializes the results into an immutable
    /// <see cref="FixedDatedRateProvider" />.
    /// </summary>
    /// <param name="provider">The provider to fetch from.</param>
    /// <param name="pairs">The currency pairs to materialize; duplicates are fetched once.</param>
    /// <param name="startDate">The inclusive first date of the window.</param>
    /// <param name="endDate">The inclusive last date of the window.</param>
    /// <param name="cancellationToken">A token to observe while fetching.</param>
    /// <returns>
    /// A task producing an immutable provider over every rate the source returned for the requested pairs and window. A
    /// pair the source has no data for simply contributes no rows.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> or <paramref name="pairs" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="pairs" /> is empty or contains an invalid (default) pair, or when
    /// <paramref name="endDate" /> is earlier than <paramref name="startDate" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Pairs are fetched sequentially through
    /// <see cref="IDatedRateProvider.GetRatesAsync(string, string, DateOnly, DateOnly, CancellationToken)" /> —
    /// the shipped web providers already coalesce and rate-limit their downloads, so parallel fan-out buys little and
    /// risks hammering a feed. Exceptions a source raises for an unserved pair propagate unchanged.
    /// </para>
    /// <para>
    /// The result is fully decoupled from <paramref name="provider" />: later fetches, cache expiry, or disposal of the
    /// source never affect it. Rows are materialized through
    /// <see cref="ExchangeRateEnumerableExtensions.ToBook(IEnumerable{ExchangeRate})" />, so rates for the same pair
    /// from multiple sources are kept as separate series (the returned provider consults them in first-seen order),
    /// inverse-resolved rows are stored under their natively quoted direction, and each series preserves its latest
    /// fetch instant. Callers needing a specific provider-selection policy can rewrap via
    /// <c>result.Book.ToFixedProvider(priority)</c>.
    /// </para>
    /// </remarks>
    public static async Task<FixedDatedRateProvider> ToFixedProviderAsync(
        this IDatedRateProvider provider,
        IEnumerable<CurrencyPair> pairs,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(pairs);
        if (endDate < startDate)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_ExchangeRateRangeInverted, nameof(endDate));

        List<CurrencyPair> distinctPairs = new();
        HashSet<CurrencyPair> seenPairs = new();
        foreach (CurrencyPair pair in pairs)
        {
            FinancialThrowHelper.ThrowIfInvalidCurrencyPair(pair, nameof(pairs));
            if (seenPairs.Add(pair))
                distinctPairs.Add(pair);
        }

        if (distinctPairs.Count == 0)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_CurrencyPairsEmpty, nameof(pairs));

        List<ExchangeRate> rows = new();
        List<string> providerOrder = new();
        HashSet<string> seenProviders = new(StringComparer.Ordinal);

        foreach (CurrencyPair pair in distinctPairs)
        {
            RateRangeResult range = await provider
                .GetRatesAsync(pair.From.ToString(), pair.To.ToString(), startDate, endDate, cancellationToken)
                .ConfigureAwait(false);

            foreach (ExchangeRate row in range)
            {
                rows.Add(row);
                if (seenProviders.Add(row.Provider))
                    providerOrder.Add(row.Provider);
            }
        }

        RateBook book = rows.ToBook();

        // The priority ctor tolerates multi-provider pairs; the book-only ctor is reserved for the no-rows case,
        // where there is no provider to prioritize (an empty book is legal and simply misses every lookup).
        return providerOrder.Count > 0
            ? new FixedDatedRateProvider(book, providerOrder)
            : new FixedDatedRateProvider(book);
    }
}
