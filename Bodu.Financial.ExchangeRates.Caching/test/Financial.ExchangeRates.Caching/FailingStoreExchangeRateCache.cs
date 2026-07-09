// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FailingStoreExchangeRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IExchangeRateCache" /> test double that persists nothing and reports every
/// <see cref="StoreFetchedRange" /> as <see cref="ExchangeRateCacheWriteStatus.Failed" />, standing in for a backend
/// whose storage write was silently swallowed. Reads always return empty, modelling a store that retained nothing, so a
/// decorator over it must refetch rather than serve a false hit.
/// </summary>
internal sealed class FailingStoreExchangeRateCache
    : IExchangeRateCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailingStoreExchangeRateCache" /> class bound to a provider.
    /// </summary>
    /// <param name="provider">The provider identifier the cache reports.</param>
    public FailingStoreExchangeRateCache(string provider) => Provider = provider;

    /// <inheritdoc />
    public string Provider { get; }

    /// <summary>
    /// Gets the number of times <see cref="StoreFetchedRange" /> was invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int StoreFetchedRangeCallCount { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf) =>
        Array.Empty<CachedExchangeRate>();

    /// <inheritdoc />
    public void Store(CurrencyPair pair, IReadOnlyList<CachedExchangeRate> rates, TimeSpan duration, DateTimeOffset asOf)
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
    public ExchangeRateCacheWriteStatus StoreFetchedRange(
        CurrencyPair pair,
        IReadOnlyList<CachedExchangeRate> rows,
        DateOnly start,
        DateOnly end,
        TimeSpan duration,
        DateTimeOffset asOf)
    {
        StoreFetchedRangeCallCount++;

        // Model a backend that swallowed the storage write: nothing is persisted and the failure is reported so the
        // decorator does not trust coverage that was never recorded.
        return ExchangeRateCacheWriteStatus.Failed;
    }
}
