// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateEnumerableExtensions.ToBook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Extensions;

public static partial class ExchangeRateEnumerableExtensions
{
    /// <summary>
    /// Materializes a sequence of exchange-rate observations into an immutable <see cref="ExchangeRateBook" />, one
    /// series per (pair, provider) combination.
    /// </summary>
    /// <param name="rates">The observations to store, in any order.</param>
    /// <returns>An immutable book holding every distinct (pair, provider, date) observation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rates" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when an element carries an invalid (default) currency pair.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="FixedDatedExchangeRateProvider(IEnumerable{ExchangeRate})" />, this materializer accepts rates
    /// for the same pair from multiple providers — each (pair, provider) combination becomes its own series — so the
    /// results of aggregated or multi-source range lookups round-trip without error. When two observations share the
    /// same pair, provider, and date, the later element wins (upsert semantics, matching
    /// <see cref="ExchangeRateTableBuilder.Upsert" />), so re-materializing overlapping fetches is resilient rather
    /// than throwing.
    /// </para>
    /// <para>
    /// A rate resolved through inverse fallback (<see cref="ExchangeRate.IsInverted" />) is stored under its natively
    /// quoted pair with the originally observed rate, rather than baking the derived reciprocal into the book; a lookup
    /// on the resulting book still resolves the derived direction through the same inverse fallback. Each series'
    /// <see cref="ExchangeRateSeries.FetchedAtUtc" /> is set to the latest fetch instant seen among its rows, so
    /// provenance survives the round trip deterministically regardless of input order.
    /// </para>
    /// </remarks>
    public static ExchangeRateBook ToBook(this IEnumerable<ExchangeRate> rates)
    {
        ThrowHelper.ThrowIfNull(rates);

        ExchangeRateTableBuilder builder = new();
        foreach (ExchangeRate rate in rates)
        {
            // Normalize an inverse-resolved row back to its natively quoted direction, so the pre-rounded reciprocal
            // Rate is not stored as if the source had published it.
            ExchangeRatePair pair = rate.IsInverted ? rate.Pair.Inverse() : rate.Pair;
            var value = rate.IsInverted ? rate.ObservedRate : rate.Rate;

            ExchangeRateSeriesBuilder series = builder.GetOrAddSeries(pair, rate.Provider);
            series.Upsert(rate.Date, value);

            // Keep the latest fetch instant per series so the result is deterministic under input reordering.
            if (rate.FetchedAtUtc is { } fetchedAt && (series.FetchedAtUtc is null || fetchedAt > series.FetchedAtUtc))
                series.FetchedAtUtc = fetchedAt;
        }

        return builder.ToBook();
    }
}
