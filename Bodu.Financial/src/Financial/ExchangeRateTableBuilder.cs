// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTableBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Provides a mutable collection of <see cref="ExchangeRateSeriesBuilder" /> instances keyed by currency pair and
/// provider, intended for assembling rate observations across many series before producing immutable snapshots.
/// </summary>
/// <remarks>
/// <para>
/// The table delegates per-series mutation to the underlying <see cref="ExchangeRateSeriesBuilder" /> instances, so
/// single-observation edits, bulk import, and snapshot semantics behave identically to working with a builder directly.
/// The table itself adds only the multi-series indexing, lazy creation, and bulk snapshot operations.
/// </para>
/// <para>
/// Instances are not thread-safe; concurrent mutation requires external synchronisation.
/// </para>
/// </remarks>
public sealed class ExchangeRateTableBuilder
{
    /// <summary>
    /// The series builders, keyed by (pair, provider).
    /// </summary>
    private readonly Dictionary<ExchangeRateSeriesKey, ExchangeRateSeriesBuilder> _series;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateTableBuilder" /> class with no series.
    /// </summary>
    public ExchangeRateTableBuilder()
    {
        _series = new Dictionary<ExchangeRateSeriesKey, ExchangeRateSeriesBuilder>();
    }

    /// <summary>
    /// Gets the number of series currently tracked.
    /// </summary>
    /// <returns>A non-negative series count.</returns>
    public int Count => _series.Count;

    /// <summary>
    /// Gets the set of keys currently tracked.
    /// </summary>
    /// <returns>An enumeration of <see cref="ExchangeRateSeriesKey" /> values in dictionary order.</returns>
    public IEnumerable<ExchangeRateSeriesKey> Keys => _series.Keys;

    /// <summary>
    /// Returns the builder for the supplied pair and provider, creating an empty one if it does not yet exist.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <returns>The existing or freshly created builder.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="provider" /> is empty or white-space.</exception>
    public ExchangeRateSeriesBuilder GetOrAddSeries(ExchangeRatePair pair, string provider)
    {
        FinancialThrowHelper.ThrowIfInvalidExchangeRatePair(pair);
        FinancialThrowHelper.ThrowIfNullOrWhiteSpaceProvider(provider);

        var key = new ExchangeRateSeriesKey(pair, provider);
        if (!_series.TryGetValue(key, out ExchangeRateSeriesBuilder? builder))
        {
            builder = new ExchangeRateSeriesBuilder(pair, provider);
            _series[key] = builder;
        }

        return builder;
    }

    /// <summary>
    /// Upserts an observation into the series identified by <paramref name="pair" /> and <paramref name="provider" />,
    /// creating the series if it does not yet exist.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="date">The observation date.</param>
    /// <param name="rate">The rate to record.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="provider" /> is empty or white-space.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    public void Upsert(ExchangeRatePair pair, string provider, DateOnly date, decimal rate) =>
        GetOrAddSeries(pair, provider).Upsert(date, rate);

    /// <summary>
    /// Removes the entire series for the supplied pair and provider.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <returns><see langword="true" /> if a series was removed; <see langword="false" /> if none existed.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="provider" /> is empty or white-space.</exception>
    public bool Remove(ExchangeRatePair pair, string provider)
    {
        FinancialThrowHelper.ThrowIfInvalidExchangeRatePair(pair);
        FinancialThrowHelper.ThrowIfNullOrWhiteSpaceProvider(provider);

        return _series.Remove(new ExchangeRateSeriesKey(pair, provider));
    }

    /// <summary>
    /// Reports whether the table tracks a series for the supplied pair and provider.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <returns><see langword="true" /> if a series exists; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="provider" /> is empty or white-space.</exception>
    public bool ContainsSeries(ExchangeRatePair pair, string provider)
    {
        FinancialThrowHelper.ThrowIfInvalidExchangeRatePair(pair);
        FinancialThrowHelper.ThrowIfNullOrWhiteSpaceProvider(provider);

        return _series.ContainsKey(new ExchangeRateSeriesKey(pair, provider));
    }

    /// <summary>
    /// Attempts to retrieve the builder for the supplied pair and provider without creating one.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="builder">
    /// When this method returns <see langword="true" />, the existing builder; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> if the builder was found; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="provider" /> is empty or white-space.</exception>
    public bool TryGetBuilder(ExchangeRatePair pair, string provider, out ExchangeRateSeriesBuilder? builder)
    {
        FinancialThrowHelper.ThrowIfInvalidExchangeRatePair(pair);
        FinancialThrowHelper.ThrowIfNullOrWhiteSpaceProvider(provider);

        return _series.TryGetValue(new ExchangeRateSeriesKey(pair, provider), out builder);
    }

    /// <summary>
    /// Attempts to retrieve an immutable snapshot of the series for the supplied pair and provider.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="series">
    /// When this method returns <see langword="true" />, the snapshot; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if a non-empty series snapshot was produced; <see langword="false" /> if no series
    /// existed or it held no observations.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="provider" /> is empty or white-space.</exception>
    public bool TryGetSeries(ExchangeRatePair pair, string provider, out ExchangeRateSeries? series)
    {
        FinancialThrowHelper.ThrowIfInvalidExchangeRatePair(pair);
        FinancialThrowHelper.ThrowIfNullOrWhiteSpaceProvider(provider);

        if (_series.TryGetValue(new ExchangeRateSeriesKey(pair, provider), out ExchangeRateSeriesBuilder? builder) && !builder.IsEmpty)
        {
            series = builder.ToSeries();
            return true;
        }

        series = null;
        return false;
    }

    /// <summary>
    /// Produces immutable snapshots for every non-empty series in the table.
    /// </summary>
    /// <returns>
    /// A list of <see cref="ExchangeRateSeries" /> instances, one per non-empty builder. Empty builders are skipped
    /// because an immutable series must contain at least one observation.
    /// </returns>
    public IReadOnlyList<ExchangeRateSeries> ToSeries()
    {
        var snapshots = new List<ExchangeRateSeries>(_series.Count);
        foreach (ExchangeRateSeriesBuilder builder in _series.Values)
        {
            if (!builder.IsEmpty)
                snapshots.Add(builder.ToSeries());
        }

        return snapshots;
    }

    /// <summary>
    /// Produces an immutable <see cref="ExchangeRateBook" /> snapshot containing one series per non-empty builder.
    /// </summary>
    /// <returns>
    /// A new <see cref="ExchangeRateBook" /> indexed by (pair, provider). Empty builders are skipped because an
    /// immutable series must contain at least one observation.
    /// </returns>
    /// <remarks>
    /// The returned book preserves multi-provider entries for the same pair, making it the recommended hand-off path
    /// when feeding rates into a provider facade.
    /// </remarks>
    public ExchangeRateBook ToBook() => new(ToSeries());
}
