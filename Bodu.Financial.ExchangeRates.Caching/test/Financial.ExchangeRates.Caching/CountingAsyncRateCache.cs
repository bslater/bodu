// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingAsyncRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IRateCache" /> test double that also exposes the internal <see cref="IRateCacheAsync" /> seam,
/// delegating to an in-memory cache while counting synchronous and asynchronous calls separately, so the decorator's
/// per-surface routing can be asserted.
/// </summary>
internal sealed class CountingAsyncRateCache
    : IRateCache
    , IRateCacheAsync
{
    /// <summary>The in-memory cache that stores the actual state.</summary>
    private readonly InMemoryRateCache _inner;

    /// <summary>The inner cache's async seam.</summary>
    private readonly IRateCacheAsync _innerAsync;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountingAsyncRateCache" /> class.
    /// </summary>
    /// <param name="provider">The provider the cache is bound to.</param>
    public CountingAsyncRateCache(string provider)
    {
        _inner = new InMemoryRateCache(provider);
        _innerAsync = _inner;
    }

    /// <summary>
    /// Gets the number of synchronous read or write calls observed.
    /// </summary>
    /// <value>The synchronous call count.</value>
    public int SyncCalls { get; private set; }

    /// <summary>
    /// Gets the number of asynchronous seam calls observed.
    /// </summary>
    /// <value>The asynchronous call count.</value>
    public int AsyncCalls { get; private set; }

    /// <summary>
    /// Gets the last cancellation token passed through the asynchronous seam.
    /// </summary>
    /// <value>The last token observed.</value>
    public CancellationToken LastToken { get; private set; }

    /// <inheritdoc />
    public string Provider => _inner.Provider;

    /// <inheritdoc />
    public IReadOnlyList<CachedRate> GetRates(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        SyncCalls++;
        return _inner.GetRates(pair, duration, asOf);
    }

    /// <inheritdoc />
    public void Store(CurrencyPair pair, IReadOnlyList<CachedRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        SyncCalls++;
        _inner.Store(pair, rates, duration, asOf);
    }

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        SyncCalls++;
        return _inner.GetCoverage(pair, duration, asOf);
    }

    /// <inheritdoc />
    public void RecordCoverage(CurrencyPair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        SyncCalls++;
        _inner.RecordCoverage(pair, start, end, duration, asOf);
    }

    /// <inheritdoc />
    public RateCacheWriteStatus StoreFetchedRange(CurrencyPair pair, IReadOnlyList<CachedRate> rows, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        SyncCalls++;
        return _inner.StoreFetchedRange(pair, rows, start, end, duration, asOf);
    }

    /// <inheritdoc />
    ValueTask<IReadOnlyList<CachedRate>> IRateCacheAsync.GetRatesAsync(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        AsyncCalls++;
        LastToken = cancellationToken;
        return _innerAsync.GetRatesAsync(pair, duration, asOf, cancellationToken);
    }

    /// <inheritdoc />
    ValueTask<RateCacheSnapshot> IRateCacheAsync.ReadSnapshotAsync(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        AsyncCalls++;
        LastToken = cancellationToken;
        return _innerAsync.ReadSnapshotAsync(pair, duration, asOf, cancellationToken);
    }

    /// <inheritdoc />
    ValueTask IRateCacheAsync.StoreAsync(CurrencyPair pair, IReadOnlyList<CachedRate> rates, TimeSpan duration, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        AsyncCalls++;
        LastToken = cancellationToken;
        return _innerAsync.StoreAsync(pair, rates, duration, asOf, cancellationToken);
    }

    /// <inheritdoc />
    ValueTask<RateCacheWriteStatus> IRateCacheAsync.StoreFetchedRangeAsync(CurrencyPair pair, IReadOnlyList<CachedRate> rows, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        AsyncCalls++;
        LastToken = cancellationToken;
        return _innerAsync.StoreFetchedRangeAsync(pair, rows, start, end, duration, asOf, cancellationToken);
    }
}
