// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Stores the ordered set of dated exchange-rate observations for a single provider and currency pair, optimised for
/// read-heavy lookup with allocation-free <c>O(log n)</c> resolution under any <see cref="RateDateResolution" />
/// policy.
/// </summary>
/// <remarks>
/// <para>
/// The collection is immutable after construction. Internally it stores observations in a shared
/// <see cref="RateSeriesStorage" /> backed by two parallel sorted arrays — an <see cref="int" /> array of day numbers (<see cref="DateOnly.DayNumber" />)
/// and a <see cref="decimal" /> array of rates — so that the binary search at lookup time touches only the compact date
/// array. Compared with <see cref="System.Collections.Generic.SortedDictionary{TKey, TValue}" /> this gives
/// substantially better cache locality, no per-node allocation, and predictable hot-path performance for the multi-year
/// daily series typical of FX data.
/// </para>
/// <para>
/// Instances are safe to share across threads after construction because all read paths only touch read-only arrays.
/// Use <see cref="RateSeriesBuilder" /> to construct or edit observations imperatively before producing a snapshot via
/// <see cref="RateSeriesBuilder.ToSeries" />.
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
/// // Resolve a date that has no exact observation, falling back to the previous one.
/// bool ok = series.TryGetRate(
///     new DateOnly(2024, 3, 3),
///     RateLookupOptions.PreviousWithin(5),
///     out DateOnly resolvedDate,   // 2024-03-01
///     out decimal rate);           // 0.92
///]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("{Pair.From,nq}/{Pair.To,nq} ({Provider,nq}) Count={Count}")]
public sealed class RateSeries
{
    /// <summary>The shared immutable storage holding the validated, sorted, unique day-number / rate arrays.</summary>
    private readonly RateSeriesStorage _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateSeries" /> class from a sequence of date/rate tuples.
    /// </summary>
    /// <param name="pair">The currency pair this series describes.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="rates">
    /// The observations to include. Must contain at least one entry and must not contain duplicate dates.
    /// </param>
    /// <param name="fetchedAtUtc">
    /// The UTC instant at which the load that produced this series downloaded its source data, or
    /// <see langword="null" /> when not tracked.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> or <paramref name="rates" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="provider" /> is empty or white-space, if <paramref name="rates" /> is empty, or if it
    /// contains duplicate dates.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if any rate in <paramref name="rates" /> is zero or negative.
    /// </exception>
    public RateSeries(
        CurrencyPair pair,
        string provider,
        IEnumerable<(DateOnly Date, decimal Rate)> rates,
        DateTimeOffset? fetchedAtUtc = null)
    {
        FinancialThrowHelper.ThrowIfInvalidCurrencyPair(pair);
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);
        ThrowHelper.ThrowIfNull(rates);

        Pair = pair;
        Provider = provider;
        FetchedAtUtc = fetchedAtUtc;
        _storage = RateSeriesStorage.Create(rates, nameof(rates));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateSeries" /> class from a sequence of
    /// <see cref="RateObservation" /> records.
    /// </summary>
    /// <param name="pair">The currency pair this series describes.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="observations">
    /// The observations to include. Must contain at least one entry and must not contain duplicate dates.
    /// </param>
    /// <param name="fetchedAtUtc">
    /// The UTC instant at which the load that produced this series downloaded its source data, or
    /// <see langword="null" /> when not tracked.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> or <paramref name="observations" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="provider" /> is empty or white-space, if <paramref name="observations" /> is empty, or
    /// if it contains duplicate dates.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if any rate in <paramref name="observations" /> is zero or negative.
    /// </exception>
    public RateSeries(
        CurrencyPair pair,
        string provider,
        IEnumerable<RateObservation> observations,
        DateTimeOffset? fetchedAtUtc = null)
    {
        FinancialThrowHelper.ThrowIfInvalidCurrencyPair(pair);
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);
        ThrowHelper.ThrowIfNull(observations);

        Pair = pair;
        Provider = provider;
        FetchedAtUtc = fetchedAtUtc;
        _storage = RateSeriesStorage.Create(observations, nameof(observations));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateSeries" /> class from pre-validated storage. Used by the
    /// builder snapshot path to avoid revalidation.
    /// </summary>
    /// <param name="pair">The currency pair this series describes.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <param name="storage">The storage instance the new series should adopt.</param>
    /// <param name="fetchedAtUtc">
    /// The UTC instant at which the load that produced this series downloaded its source data, or
    /// <see langword="null" /> when not tracked.
    /// </param>
    internal RateSeries(CurrencyPair pair, string provider, RateSeriesStorage storage, DateTimeOffset? fetchedAtUtc = null)
    {
        Pair = pair;
        Provider = provider;
        FetchedAtUtc = fetchedAtUtc;
        _storage = storage;
    }

    /// <summary>
    /// Gets the currency pair this series describes.
    /// </summary>
    /// <value>The series pair.</value>
    public CurrencyPair Pair { get; }

    /// <summary>
    /// Gets the identifier of the source that published the rates in this series.
    /// </summary>
    /// <value>A non-empty provider identifier.</value>
    public string Provider { get; }

    /// <summary>
    /// Gets the UTC instant at which the load that produced this series downloaded its source data, or
    /// <see langword="null" /> when not tracked.
    /// </summary>
    /// <value>The fetch instant when known; otherwise <see langword="null" />.</value>
    /// <remarks>
    /// The fetch instant is recorded at the series grain and stamped onto every <see cref="ExchangeRate" /> the series
    /// materializes, so downstream audit consumers can attribute a served rate to the moment its backing data was
    /// fetched. It is provenance metadata only and does not affect resolution.
    /// </remarks>
    public DateTimeOffset? FetchedAtUtc { get; }

    /// <summary>
    /// Gets the number of observations stored in this series.
    /// </summary>
    /// <value>A non-negative count equal to the number of rate entries supplied at construction.</value>
    public int Count => _storage.Count;

    /// <summary>
    /// Attempts to resolve a rate for <paramref name="requestedDate" /> under <paramref name="options" />.
    /// </summary>
    /// <param name="requestedDate">The calendar date the caller is asking about.</param>
    /// <param name="options">The lookup rules to apply.</param>
    /// <param name="resolvedDate">
    /// When this method returns <see langword="true" />, the observation date selected as the answer; otherwise
    /// <see langword="default" />.
    /// </param>
    /// <param name="rate">
    /// When this method returns <see langword="true" />, the rate observed on <paramref name="resolvedDate" />;
    /// otherwise <see langword="default" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if a rate was resolved within tolerance; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The resolution algorithm performs a single <see cref="Array.BinarySearch{T}(T[], T)" /> over the day-number
    /// array. On an exact hit the corresponding rate is returned immediately. On a miss the previous and next candidate
    /// indices are derived from the bitwise complement of the returned index, then selected per
    /// <paramref name="options" />. For <see cref="RateDateResolution.Nearest" /> with a tie (the requested date lies
    /// exactly midway between two observations), this method returns <see langword="false" /> so callers receive a
    /// deterministic failure rather than an arbitrary pick.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetRate(
        DateOnly requestedDate,
        RateLookupOptions? options,
        out DateOnly resolvedDate,
        out decimal rate)
    {
        options ??= RateLookupOptions.Exact;
        options.Validate();

        return _storage.TryGetRate(requestedDate, options, out resolvedDate, out rate);
    }

    /// <summary>
    /// Enumerates the observations in strictly ascending date order.
    /// </summary>
    /// <returns>A lazy sequence of <see cref="RateObservation" /> values.</returns>
    public IEnumerable<RateObservation> GetObservations() => _storage.Enumerate();

    /// <summary>
    /// Returns a new series with the observation for <paramref name="date" /> upserted to <paramref name="rate" />.
    /// </summary>
    /// <param name="date">The observation date to insert or update.</param>
    /// <param name="rate">The rate to record on <paramref name="date" />.</param>
    /// <returns>A new <see cref="RateSeries" /> reflecting the updated observation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="rate" /> is zero or negative.
    /// </exception>
    /// <remarks>
    /// Each call materialises a complete new immutable series, so this method is intended for occasional,
    /// functional-style single updates. For bulk import or repeated mutation, accumulate observations in an
    /// <see cref="RateSeriesBuilder" /> and build the series once rather than calling <see cref="WithRate" /> in a
    /// loop.
    /// </remarks>
    public RateSeries WithRate(DateOnly date, decimal rate)
    {
        ThrowHelper.ThrowIfZeroOrNegative(rate);

        if (_storage.TryGetExactRate(date, out decimal existing) && existing == rate)
            return this;

        RateSeriesBuilder builder = ToBuilder();
        builder.Upsert(date, rate);
        return builder.ToSeries();
    }

    /// <summary>
    /// Returns a new series with the observation for <paramref name="date" /> removed if present.
    /// </summary>
    /// <param name="date">The observation date to remove.</param>
    /// <returns>A new <see cref="RateSeries" /> reflecting the removal.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if removing the observation would leave the series empty.
    /// </exception>
    public RateSeries WithoutRate(DateOnly date)
    {
        if (!_storage.Contains(date))
            return this;

        RateSeriesBuilder builder = ToBuilder();
        builder.Remove(date);
        return builder.ToSeries();
    }

    /// <summary>
    /// Creates a mutable builder pre-populated from this series.
    /// </summary>
    /// <returns>A new <see cref="RateSeriesBuilder" /> seeded with the current observations.</returns>
    public RateSeriesBuilder ToBuilder() => new(this);

    /// <summary>
    /// Provides internal access to the underlying storage so the builder can seed its buffer without copying through a
    /// public enumeration.
    /// </summary>
    /// <returns>The series' immutable storage.</returns>
    internal RateSeriesStorage GetStorage() => _storage;
}
