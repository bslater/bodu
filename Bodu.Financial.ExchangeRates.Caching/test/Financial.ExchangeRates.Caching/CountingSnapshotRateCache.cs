// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingSnapshotRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IRateCache" /> test double that delegates to an in-memory cache and implements the snapshot-read seam,
/// counting snapshot and standard reads so the decorator's single-read range path can be asserted.
/// </summary>
internal sealed class CountingSnapshotRateCache
    : IRateCache
    , IRateCacheSnapshotReader
{
    /// <summary>The in-memory cache that stores the actual state.</summary>
    private readonly InMemoryRateCache _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountingSnapshotRateCache" /> class.
    /// </summary>
    /// <param name="provider">The provider the cache is bound to.</param>
    public CountingSnapshotRateCache(string provider)
    {
        _inner = new InMemoryRateCache(provider);
    }

    /// <summary>
    /// Gets the number of <see cref="IRateCacheSnapshotReader.ReadSnapshot" /> calls observed.
    /// </summary>
    /// <value>The snapshot-read count.</value>
    public int SnapshotCount { get; private set; }

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

    /// <inheritdoc />
    RateCacheSnapshot IRateCacheSnapshotReader.ReadSnapshot(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        SnapshotCount++;
        return ((IRateCacheSnapshotReader)_inner).ReadSnapshot(pair, duration, asOf);
    }
}
