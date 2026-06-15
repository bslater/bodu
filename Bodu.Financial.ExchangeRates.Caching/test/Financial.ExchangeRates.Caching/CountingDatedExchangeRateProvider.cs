// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingDatedExchangeRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IDatedExchangeRateProvider" /> test double that delegates to a fixed in-memory book and counts how many
/// times each lookup method is invoked, so tests can assert when the caching decorator avoided the inner provider.
/// </summary>
internal sealed class CountingDatedExchangeRateProvider
    : IDatedExchangeRateProvider
{
    /// <summary>
    /// The fixed provider backing the counted lookups.
    /// </summary>
    private readonly FixedDatedExchangeRateProvider _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountingDatedExchangeRateProvider" /> class.
    /// </summary>
    /// <param name="rates">The observations the inner provider resolves from.</param>
    public CountingDatedExchangeRateProvider(IEnumerable<ExchangeRate> rates) =>
        _inner = new FixedDatedExchangeRateProvider(rates);

    /// <summary>
    /// Gets the number of times <see cref="GetRate" /> has been invoked.
    /// </summary>
    /// <returns>The invocation count.</returns>
    public int GetRateCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="TryGetRate" /> has been invoked.
    /// </summary>
    /// <returns>The invocation count.</returns>
    public int TryGetRateCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="GetRatesAsync" /> has been invoked.
    /// </summary>
    /// <returns>The invocation count.</returns>
    public int GetRatesAsyncCallCount { get; private set; }

    /// <summary>
    /// Gets the total number of inner lookups across the single-date methods.
    /// </summary>
    /// <returns>The combined invocation count.</returns>
    public int TotalCallCount => GetRateCallCount + TryGetRateCallCount;

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, ExchangeRateLookupOptions? options = null)
    {
        GetRateCallCount++;
        return _inner.GetRate(fromIsoCode, toIsoCode, options);
    }

    /// <inheritdoc />
    public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null)
    {
        GetRateCallCount++;
        return _inner.GetRate(fromIsoCode, toIsoCode, date, options);
    }

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, out ExchangeRateLookupResult result)
    {
        TryGetRateCallCount++;
        return _inner.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);
    }

    /// <inheritdoc />
    public IEnumerable<ExchangeRate> GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        GetRatesAsyncCallCount++;
        return _inner.GetRates(fromIsoCode, toIsoCode, startDate, endDate);
    }

    /// <inheritdoc />
    public ValueTask<ExchangeRateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, ExchangeRateLookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        GetRateCallCount++;
        return _inner.GetRateAsync(fromIsoCode, toIsoCode, options, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<ExchangeRateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        GetRateCallCount++;
        return _inner.GetRateAsync(fromIsoCode, toIsoCode, date, options, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<ExchangeRate>> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        GetRatesAsyncCallCount++;
        return _inner.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken);
    }
}
