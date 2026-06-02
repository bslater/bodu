// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateBook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Frozen;
using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Provides an immutable, read-heavy collection of <see cref="ExchangeRateSeries" /> instances keyed by
/// <see cref="ExchangeRateSeriesKey" /> (pair + provider), forming the immutable bridge between mutable build-side
/// types (<see cref="ExchangeRateTableBuilder" />) and dated lookup providers
/// (<see cref="FixedDatedExchangeRateProvider" /> and friends).
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
/// </remarks>
public sealed class ExchangeRateBook
{
    /// <summary>
    /// The per-pair-and-provider series store, frozen after construction for fast read-only access.
    /// </summary>
    private readonly FrozenDictionary<ExchangeRateSeriesKey, ExchangeRateSeries> _series;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateBook" /> class from the supplied series.
    /// </summary>
    /// <param name="series">The series to include in the book.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="series" /> is <see langword="null" />, or if any element of <paramref name="series" />
    /// is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if two series share the same pair and provider.</exception>
    public ExchangeRateBook(IEnumerable<ExchangeRateSeries> series)
    {
        ThrowHelper.ThrowIfNull(series);

        Dictionary<ExchangeRateSeriesKey, ExchangeRateSeries> buffer = new();
        foreach (ExchangeRateSeries entry in series)
        {
            if (entry is null)
                throw new ArgumentNullException(nameof(series));

            ExchangeRateSeriesKey key = new(entry.Pair, entry.Provider);
            if (!buffer.TryAdd(key, entry))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        FinancialResourceStrings.Arg_Invalid_ExchangeRateBookDuplicateKey,
                        entry.Pair.FromIsoCode,
                        entry.Pair.ToIsoCode,
                        entry.Provider),
                    nameof(series));
            }
        }

        _series = buffer.ToFrozenDictionary();
    }

    /// <summary>
    /// Gets the number of (pair, provider) series held by the book.
    /// </summary>
    /// <returns>A non-negative series count.</returns>
    public int Count => _series.Count;

    /// <summary>
    /// Gets the set of keys currently held.
    /// </summary>
    /// <returns>The keys exposed by the underlying frozen dictionary.</returns>
    public IReadOnlyCollection<ExchangeRateSeriesKey> Keys => _series.Keys;

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
    public bool TryGetSeries(ExchangeRatePair pair, string provider, out ExchangeRateSeries? series)
    {
        FinancialThrowHelper.ThrowIfInvalidExchangeRatePair(pair);
        FinancialThrowHelper.ThrowIfNullOrWhiteSpaceProvider(provider);

        return _series.TryGetValue(new ExchangeRateSeriesKey(pair, provider), out series);
    }

    /// <summary>
    /// Enumerates every series whose currency pair equals <paramref name="pair" />, regardless of provider.
    /// </summary>
    /// <param name="pair">The currency pair to filter by.</param>
    /// <returns>A lazy sequence of <see cref="ExchangeRateSeries" /> instances ordered by their hash slot.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="pair" /> is invalid.</exception>
    public IEnumerable<ExchangeRateSeries> GetSeries(ExchangeRatePair pair)
    {
        FinancialThrowHelper.ThrowIfInvalidExchangeRatePair(pair);

        foreach (KeyValuePair<ExchangeRateSeriesKey, ExchangeRateSeries> entry in _series)
        {
            if (entry.Key.Pair.Equals(pair))
                yield return entry.Value;
        }
    }

    /// <summary>
    /// Enumerates every series in the book.
    /// </summary>
    /// <returns>A lazy sequence of <see cref="ExchangeRateSeries" /> instances.</returns>
    public IEnumerable<ExchangeRateSeries> EnumerateSeries() => _series.Values;
}
