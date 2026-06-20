// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedExchangeRateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
using Bodu.Financial.Currencies;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// Verifies the distributed-cache-specific behaviour of <see cref="DistributedExchangeRateCache" /> beyond the shared
/// cache contract: the stable key format, state sharing across instances over one backing store, storage-failure
/// resilience, and lossless round-tripping through the JSON blob.
/// </summary>
[TestClass]
public sealed partial class DistributedExchangeRateCacheTests
{
    /// <summary>
    /// The provider the caches under test are bound to.
    /// </summary>
    private const string Provider = "Test";

    /// <summary>
    /// The currency pair used by the tests.
    /// </summary>
    private static readonly ExchangeRatePair Pair = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>
    /// The freshness duration used by the tests.
    /// </summary>
    private static readonly TimeSpan Duration = TimeSpan.FromHours(24);

    /// <summary>
    /// Verifies that a rate stored through the cache is served back on read, confirming the happy path end to end over
    /// an in-memory distributed cache.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void DistributedExchangeRateCache_WhenStoredAndReadBack_ShouldServeRate()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DistributedExchangeRateCache cache = CreateCache();

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);
        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(0.5000m, rows[0].Rate);
    }

    /// <summary>
    /// Verifies that the bound provider is reported from the options.
    /// </summary>
    [TestMethod]
    public void Provider_ShouldReportBoundProvider()
    {
        DistributedExchangeRateCache cache = CreateCache();

        Assert.AreEqual(Provider, cache.Provider);
    }

    /// <summary>
    /// Creates a fresh in-memory <see cref="IDistributedCache" />.
    /// </summary>
    /// <returns>A new, empty in-memory distributed cache.</returns>
    private static MemoryDistributedCache CreateBackingStore() =>
        new(Options.Create(new MemoryDistributedCacheOptions()));

    /// <summary>
    /// Creates a cache bound to <see cref="Provider" /> over a fresh in-memory distributed cache.
    /// </summary>
    /// <returns>A new cache over its own backing store.</returns>
    private static DistributedExchangeRateCache CreateCache() =>
        CreateCache(CreateBackingStore());

    /// <summary>
    /// Creates a cache bound to <see cref="Provider" /> over the supplied distributed cache.
    /// </summary>
    /// <param name="backingStore">The distributed cache the cache reads from and writes to.</param>
    /// <returns>A new cache over <paramref name="backingStore" />.</returns>
    private static DistributedExchangeRateCache CreateCache(IDistributedCache backingStore) =>
        new(backingStore, new DistributedExchangeRateCacheOptions { Provider = Provider });
}
