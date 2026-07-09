// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingDatedExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IDatedRateProvider" /> test double that delegates to a fixed in-memory book and counts how
/// many times each lookup method is invoked, so tests can assert when the caching decorator avoided the inner provider.
/// </summary>
internal sealed class CountingDatedExchangeRateProvider
    : IDatedRateProvider
{
    /// <summary>The fixed provider backing the counted lookups.</summary>
    private readonly FixedDatedRateProvider _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountingDatedExchangeRateProvider" /> class.
    /// </summary>
    /// <param name="rates">The observations the inner provider resolves from.</param>
    public CountingDatedExchangeRateProvider(IEnumerable<ExchangeRate> rates) =>
        _inner = new FixedDatedRateProvider(rates);

    /// <summary>
    /// Gets the number of times <see cref="GetRate" /> has been invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int GetRateCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="TryGetRate" /> has been invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int TryGetRateCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="GetRatesAsync" /> has been invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int GetRatesAsyncCallCount { get; private set; }

    /// <summary>
    /// Gets the total number of inner lookups across the single-date methods.
    /// </summary>
    /// <value>The combined invocation count.</value>
    public int TotalCallCount => GetRateCallCount + TryGetRateCallCount;

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null)
    {
        GetRateCallCount++;
        return _inner.GetRate(fromIsoCode, toIsoCode, options);
    }

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null)
    {
        GetRateCallCount++;
        return _inner.GetRate(fromIsoCode, toIsoCode, date, options);
    }

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, out RateLookupResult result)
    {
        TryGetRateCallCount++;
        return _inner.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);
    }

    /// <inheritdoc />
    public RateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        GetRatesAsyncCallCount++;
        return _inner.GetRates(fromIsoCode, toIsoCode, startDate, endDate);
    }

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        GetRateCallCount++;
        return _inner.GetRateAsync(fromIsoCode, toIsoCode, options, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        GetRateCallCount++;
        return _inner.GetRateAsync(fromIsoCode, toIsoCode, date, options, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RateRangeResult> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        GetRatesAsyncCallCount++;
        return _inner.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken);
    }
}
