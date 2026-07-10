// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateTableBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides a mutable collection of <see cref="RateSeriesBuilder" /> instances keyed by currency pair and provider,
/// intended for assembling rate observations across many series before producing immutable snapshots.
/// </summary>
/// <remarks>
/// <para>
/// The table delegates per-series mutation to the underlying <see cref="RateSeriesBuilder" /> instances, so
/// single-observation edits, bulk import, and snapshot semantics behave identically to working with a builder directly.
/// The table itself adds only the multi-series indexing, lazy creation, and bulk snapshot operations.
/// </para>
/// <para>
/// Instances are not thread-safe; concurrent mutation requires external synchronisation.
/// </para>
/// <example>
/// The builder → book → provider chain is the offline-first pattern: pour any rate data you already hold (a file, a
/// database table, an archived API response) through the builder, freeze it, and serve it through the same contracts
/// the live web providers implement.
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial.Currencies;
/// using Bodu.Financial.ExchangeRates;
///
/// var builder = new RateTableBuilder();
///
/// builder.Upsert(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), "SampleData", new DateOnly(2024, 3, 15), 0.6580m);
/// builder.Upsert(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.EUR), "SampleData", new DateOnly(2024, 3, 15), 0.6196m);
///
/// // Freeze every series into an immutable book, then wrap it in a dated provider.
/// var rates = new FixedDatedRateProvider(builder.ToBook());
///
/// RateLookupResult usd = rates.GetRate("AUD", "USD", new DateOnly(2024, 3, 15));
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class RateTableBuilder
{
    /// <summary>The series builders, keyed by (pair, provider).</summary>
    private readonly Dictionary<RateSeriesKey, RateSeriesBuilder> _series;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateTableBuilder" /> class with no series.
    /// </summary>
    public RateTableBuilder()
    {
        _series = new Dictionary<RateSeriesKey, RateSeriesBuilder>();
    }

    /// <summary>
    /// Gets the number of series currently tracked.
    /// </summary>
    /// <value>A non-negative series count.</value>
    public int Count => _series.Count;

    /// <summary>
    /// Gets the set of keys currently tracked.
    /// </summary>
    /// <value>An enumeration of <see cref="RateSeriesKey" /> values in dictionary order.</value>
    public IEnumerable<RateSeriesKey> Keys => _series.Keys;

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
    public RateSeriesBuilder GetOrAddSeries(CurrencyPair pair, string provider)
    {
        FinancialThrowHelper.ThrowIfInvalidCurrencyPair(pair);
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);

        var key = new RateSeriesKey(pair, provider);
        if (!_series.TryGetValue(key, out RateSeriesBuilder? builder))
        {
            builder = new RateSeriesBuilder(pair, provider);
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
    /// <param name="fetchedAtUtc">
    /// The UTC instant at which the load contributing this observation downloaded its source data, or
    /// <see langword="null" /> to leave the series' fetch instant unchanged.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="provider" /> is empty or white-space.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    /// <remarks>
    /// When supplied, <paramref name="fetchedAtUtc" /> is recorded at the series grain — a property of the load, not of
    /// the individual observation — and is stamped onto every <see cref="ExchangeRate" /> the series materializes. A
    /// later upsert carrying a fresh instant overwrites it; passing <see langword="null" /> leaves any previously
    /// recorded instant in place.
    /// </remarks>
    public void Upsert(CurrencyPair pair, string provider, DateOnly date, decimal rate, DateTimeOffset? fetchedAtUtc = null)
    {
        RateSeriesBuilder builder = GetOrAddSeries(pair, provider);
        builder.Upsert(date, rate);
        if (fetchedAtUtc is { } f)
            builder.FetchedAtUtc = f;
    }

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
    public bool Remove(CurrencyPair pair, string provider)
    {
        FinancialThrowHelper.ThrowIfInvalidCurrencyPair(pair);
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);

        return _series.Remove(new RateSeriesKey(pair, provider));
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
    public bool ContainsSeries(CurrencyPair pair, string provider)
    {
        FinancialThrowHelper.ThrowIfInvalidCurrencyPair(pair);
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);

        return _series.ContainsKey(new RateSeriesKey(pair, provider));
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
    public bool TryGetBuilder(CurrencyPair pair, string provider, out RateSeriesBuilder? builder)
    {
        FinancialThrowHelper.ThrowIfInvalidCurrencyPair(pair);
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);

        return _series.TryGetValue(new RateSeriesKey(pair, provider), out builder);
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
    public bool TryGetSeries(CurrencyPair pair, string provider, out RateSeries? series)
    {
        FinancialThrowHelper.ThrowIfInvalidCurrencyPair(pair);
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);

        if (_series.TryGetValue(new RateSeriesKey(pair, provider), out RateSeriesBuilder? builder) && !builder.IsEmpty)
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
    /// A list of <see cref="RateSeries" /> instances, one per non-empty builder. Empty builders are skipped because an
    /// immutable series must contain at least one observation.
    /// </returns>
    public IReadOnlyList<RateSeries> ToSeries()
    {
        var snapshots = new List<RateSeries>(_series.Count);
        foreach (RateSeriesBuilder builder in _series.Values)
        {
            if (!builder.IsEmpty)
                snapshots.Add(builder.ToSeries());
        }

        return snapshots;
    }

    /// <summary>
    /// Produces an immutable <see cref="RateBook" /> snapshot containing one series per non-empty builder.
    /// </summary>
    /// <returns>
    /// A new <see cref="RateBook" /> indexed by (pair, provider). Empty builders are skipped because an immutable
    /// series must contain at least one observation.
    /// </returns>
    /// <remarks>
    /// The returned book preserves multi-provider entries for the same pair, making it the recommended hand-off path
    /// when feeding rates into a provider facade.
    /// </remarks>
    public RateBook ToBook() => new(ToSeries());
}
