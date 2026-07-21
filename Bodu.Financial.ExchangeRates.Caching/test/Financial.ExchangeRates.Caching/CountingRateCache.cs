// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IRateCache" /> test double that delegates to an in-memory cache while counting every read, and —
/// unlike the in-memory cache itself — deliberately does <b>not</b> implement the snapshot-read seam, so the caching
/// decorator's fallback path can be exercised and its read counts asserted.
/// </summary>
internal sealed class CountingRateCache
    : IRateCache
{
    /// <summary>The in-memory cache that stores the actual state.</summary>
    private readonly InMemoryRateCache _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountingRateCache" /> class.
    /// </summary>
    /// <param name="provider">The provider the cache is bound to.</param>
    public CountingRateCache(string provider)
    {
        _inner = new InMemoryRateCache(provider);
    }

    /// <summary>
    /// Gets the number of <see cref="GetRates" /> calls observed.
    /// </summary>
    /// <value>The read count.</value>
    public int GetRatesCount { get; private set; }

    /// <summary>
    /// Gets the number of <see cref="GetCoverage" /> calls observed.
    /// </summary>
    /// <value>The read count.</value>
    public int GetCoverageCount { get; private set; }

    /// <inheritdoc />
    public string Provider => _inner.Provider;

    /// <inheritdoc />
    public IReadOnlyList<CachedRate> GetRates(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        GetRatesCount++;
        return _inner.GetRates(pair, duration, asOf);
    }

    /// <inheritdoc />
    public void Store(CurrencyPair pair, IReadOnlyList<CachedRate> rates, TimeSpan duration, DateTimeOffset asOf) =>
        _inner.Store(pair, rates, duration, asOf);

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        GetCoverageCount++;
        return _inner.GetCoverage(pair, duration, asOf);
    }

    /// <inheritdoc />
    public void RecordCoverage(CurrencyPair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf) =>
        _inner.RecordCoverage(pair, start, end, duration, asOf);

    /// <inheritdoc />
    public RateCacheWriteStatus StoreFetchedRange(CurrencyPair pair, IReadOnlyList<CachedRate> rows, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf) =>
        _inner.StoreFetchedRange(pair, rows, start, end, duration, asOf);
}
