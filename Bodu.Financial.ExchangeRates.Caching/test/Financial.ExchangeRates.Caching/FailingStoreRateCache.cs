// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FailingStoreRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IRateCache" /> test double that persists nothing and reports every
/// <see cref="StoreFetchedRange" /> as <see cref="RateCacheWriteStatus.Failed" />, standing in for a backend
/// whose storage write was silently swallowed. Reads always return empty, modelling a store that retained nothing, so a
/// decorator over it must refetch rather than serve a false hit.
/// </summary>
internal sealed class FailingStoreRateCache
    : IRateCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailingStoreRateCache" /> class bound to a provider.
    /// </summary>
    /// <param name="provider">The provider identifier the cache reports.</param>
    public FailingStoreRateCache(string provider) => Provider = provider;

    /// <inheritdoc />
    public string Provider { get; }

    /// <summary>
    /// Gets the number of times <see cref="StoreFetchedRange" /> was invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int StoreFetchedRangeCallCount { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<CachedRate> GetRates(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf) =>
        Array.Empty<CachedRate>();

    /// <inheritdoc />
    public void Store(CurrencyPair pair, IReadOnlyList<CachedRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        // Intentionally no-op: the storage write is modelled as swallowed.
    }

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf) =>
        new();

    /// <inheritdoc />
    public void RecordCoverage(CurrencyPair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        // Intentionally no-op: the storage write is modelled as swallowed.
    }

    /// <inheritdoc />
    public RateCacheWriteStatus StoreFetchedRange(
        CurrencyPair pair,
        IReadOnlyList<CachedRate> rows,
        DateOnly start,
        DateOnly end,
        TimeSpan duration,
        DateTimeOffset asOf)
    {
        StoreFetchedRangeCallCount++;

        // Model a backend that swallowed the storage write: nothing is persisted and the failure is reported so the
        // decorator does not trust coverage that was never recorded.
        return RateCacheWriteStatus.Failed;
    }
}
