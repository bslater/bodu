// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HistoryAwareCountingProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IDatedRateProvider" /> test double that advertises a settable <see cref="RateHistoryAvailability" />
/// through <see cref="IHistoricalRateProvider" /> while delegating to a fixed in-memory book and counting every
/// lookup, so tests can assert both what the decorators forward and when they avoided calling a source outside its
/// advertised history.
/// </summary>
internal sealed class HistoryAwareCountingProvider
    : IDatedRateProvider, IHistoricalRateProvider
{
    /// <summary>The fixed provider backing the counted lookups.</summary>
    private readonly FixedDatedRateProvider _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryAwareCountingProvider" /> class.
    /// </summary>
    /// <param name="availability">The history depth the double advertises.</param>
    /// <param name="rates">The observations the inner provider resolves from.</param>
    public HistoryAwareCountingProvider(RateHistoryAvailability availability, IEnumerable<ExchangeRate> rates)
    {
        HistoryAvailability = availability;
        _inner = new FixedDatedRateProvider(rates);
    }

    /// <summary>
    /// Gets or sets the history depth the double advertises.
    /// </summary>
    /// <value>The advertised availability, mutable so a test can change the declaration mid-scenario.</value>
    public RateHistoryAvailability HistoryAvailability { get; set; }

    /// <summary>
    /// Gets the number of times a single-date lookup (<c>GetRate</c>, <c>TryGetRate</c>, or <c>GetRateAsync</c>) has
    /// been invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int SingleDateCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times a range lookup (<c>GetRates</c> or <c>GetRatesAsync</c>) has been invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int RangeCallCount { get; private set; }

    /// <summary>
    /// Gets the start date the most recent range lookup was invoked with, so tests can observe the decorator's clamp.
    /// </summary>
    /// <value>The captured start date, or <see langword="null" /> before any range lookup.</value>
    public DateOnly? LastRangeStart { get; private set; }

    /// <summary>
    /// Gets the end date the most recent range lookup was invoked with.
    /// </summary>
    /// <value>The captured end date, or <see langword="null" /> before any range lookup.</value>
    public DateOnly? LastRangeEnd { get; private set; }

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null)
    {
        SingleDateCallCount++;
        return _inner.GetRate(fromIsoCode, toIsoCode, options);
    }

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null)
    {
        SingleDateCallCount++;
        return _inner.GetRate(fromIsoCode, toIsoCode, date, options);
    }

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, out RateLookupResult result)
    {
        SingleDateCallCount++;
        return _inner.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);
    }

    /// <inheritdoc />
    public RateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        RangeCallCount++;
        LastRangeStart = startDate;
        LastRangeEnd = endDate;
        return _inner.GetRates(fromIsoCode, toIsoCode, startDate, endDate);
    }

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        SingleDateCallCount++;
        return _inner.GetRateAsync(fromIsoCode, toIsoCode, options, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        SingleDateCallCount++;
        return _inner.GetRateAsync(fromIsoCode, toIsoCode, date, options, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RateRangeResult> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        RangeCallCount++;
        LastRangeStart = startDate;
        LastRangeEnd = endDate;
        return _inner.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken);
    }
}
