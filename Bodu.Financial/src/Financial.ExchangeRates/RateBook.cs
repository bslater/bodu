// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateBook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Frozen;
using System.Globalization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides an immutable, read-heavy collection of <see cref="RateSeries" /> instances keyed by
/// <see cref="RateSeriesKey" /> (pair + provider), forming the immutable bridge between mutable build-side types (<see cref="RateTableBuilder" />)
/// and dated lookup providers ( <see cref="FixedDatedRateProvider" /> and friends).
/// </summary>
/// <remarks>
/// <para>
/// The book stores at most one series per (pair, provider) combination, allowing the same currency pair to be
/// represented by multiple providers within a single immutable store. Providers built on top of the book apply an
/// explicit provider-selection policy at lookup time.
/// </para>
/// <para>
/// Instances are safe to share across threads after construction because all read paths only touch the underlying
/// frozen dictionary.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial;
///
/// var series = new RateSeries(
///     new CurrencyPair(CurrencyCode.USD, CurrencyCode.EUR),
///     "ECB",
///     new[] { (new DateOnly(2024, 3, 1), 0.92m), (new DateOnly(2024, 3, 4), 0.93m) });
///
/// var book = new RateBook(new[] { series });
///
/// // Look the series back up by pair and provider.
/// if (book.TryGetSeries(series.Pair, "ECB", out RateSeries? found))
/// {
///     int count = found!.Count;   // 2
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class RateBook
{
    /// <summary>The per-pair-and-provider series store, frozen after construction for fast read-only access.</summary>
    private readonly FrozenDictionary<RateSeriesKey, RateSeries> _series;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateBook" /> class from the supplied series.
    /// </summary>
    /// <param name="series">The series to include in the book.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="series" /> is <see langword="null" />, or if any element of <paramref name="series" />
    /// is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if two series share the same pair and provider.</exception>
    public RateBook(IEnumerable<RateSeries> series)
    {
        ThrowHelper.ThrowIfNull(series);

        Dictionary<RateSeriesKey, RateSeries> buffer = new();
        foreach (RateSeries entry in series)
        {
            if (entry is null)
                throw new ArgumentNullException(nameof(series));

            RateSeriesKey key = new(entry.Pair, entry.Provider);
            if (!buffer.TryAdd(key, entry))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        FinancialResourceStrings.Arg_Invalid_RateBookDuplicateKey,
                        entry.Pair.From.ToString(),
                        entry.Pair.To.ToString(),
                        entry.Provider),
                    nameof(series));
            }
        }

        _series = buffer.ToFrozenDictionary();
    }

    /// <summary>
    /// Gets the number of (pair, provider) series held by the book.
    /// </summary>
    /// <value>A non-negative series count.</value>
    public int Count => _series.Count;

    /// <summary>
    /// Gets the set of keys currently held.
    /// </summary>
    /// <value>The keys exposed by the underlying frozen dictionary.</value>
    public IReadOnlyCollection<RateSeriesKey> Keys => _series.Keys;

    /// <summary>
    /// Attempts to retrieve the series for the supplied pair and provider.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="series">
    /// When this method returns <see langword="true" />, the matching series; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> if a matching series exists; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="pair" /> is invalid or <paramref name="provider" /> is empty / white-space.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public bool TryGetSeries(CurrencyPair pair, string provider, out RateSeries? series)
    {
        FinancialThrowHelper.ThrowIfInvalidCurrencyPair(pair);
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);

        return _series.TryGetValue(new RateSeriesKey(pair, provider), out series);
    }

    /// <summary>
    /// Enumerates every series whose currency pair equals <paramref name="pair" />, regardless of provider.
    /// </summary>
    /// <param name="pair">The currency pair to filter by.</param>
    /// <returns>A lazy sequence of <see cref="RateSeries" /> instances ordered by their hash slot.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="pair" /> is invalid.</exception>
    public IEnumerable<RateSeries> GetSeries(CurrencyPair pair)
    {
        FinancialThrowHelper.ThrowIfInvalidCurrencyPair(pair);

        foreach (KeyValuePair<RateSeriesKey, RateSeries> entry in _series)
        {
            if (entry.Key.Pair.Equals(pair))
                yield return entry.Value;
        }
    }

    /// <summary>
    /// Enumerates every series in the book.
    /// </summary>
    /// <returns>A lazy sequence of <see cref="RateSeries" /> instances.</returns>
    public IEnumerable<RateSeries> EnumerateSeries() => _series.Values;

    /// <summary>
    /// Creates a mutable <see cref="RateTableBuilder" /> seeded with a copy of every series in this book, including
    /// each series' fetch instant.
    /// </summary>
    /// <returns>A new builder whose contents start equal to this book.</returns>
    /// <remarks>
    /// This is the editing counterpart of <see cref="RateTableBuilder.ToBook" />: edits to the returned builder never
    /// affect this book, and calling <c>ToBook()</c> on the builder completes the round trip. It mirrors the per-series
    /// <see cref="RateSeries.ToBuilder" />.
    /// </remarks>
    public RateTableBuilder ToBuilder()
    {
        RateTableBuilder builder = new();

        foreach (RateSeries series in _series.Values)
        {
            RateSeriesBuilder seriesBuilder = builder.GetOrAddSeries(series.Pair, series.Provider);
            seriesBuilder.AddRange(series.GetObservations());
            seriesBuilder.FetchedAtUtc = series.FetchedAtUtc;
        }

        return builder;
    }
}
