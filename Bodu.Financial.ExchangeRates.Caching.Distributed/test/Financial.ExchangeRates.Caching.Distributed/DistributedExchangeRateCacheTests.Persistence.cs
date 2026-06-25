// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedExchangeRateCacheTests.Persistence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Microsoft.Extensions.Caching.Distributed;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

/// <summary>
/// Verifies that <see cref="DistributedExchangeRateCache" /> persists its state to the backing
/// <see cref="IDistributedCache" /> under a stable key, so the state is shared by any cache instance pointing at the
/// same backing store.
/// </summary>
public sealed partial class DistributedExchangeRateCacheTests
{
    /// <summary>
    /// Verifies that rates stored through one cache instance are readable through a second instance over the same
    /// backing distributed cache, confirming the state lives in the store and not in the instance.
    /// </summary>
    [TestMethod]
    public void Store_WhenSecondInstanceSharesBackingStore_ShouldReadPersistedRates()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        MemoryDistributedCache backingStore = CreateBackingStore();

        DistributedExchangeRateCache writer = CreateCache(backingStore);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        DistributedExchangeRateCache reader = CreateCache(backingStore);
        IReadOnlyList<CachedExchangeRate> rows = reader.GetRates(Pair, Duration, now);

        Assert.HasCount(1, rows);
        Assert.AreEqual(new DateOnly(2023, 1, 3), rows[0].Date);
        Assert.AreEqual(0.5000m, rows[0].Rate);
    }

    /// <summary>
    /// Verifies that coverage recorded through one cache instance is readable through a second instance over the same
    /// backing distributed cache.
    /// </summary>
    [TestMethod]
    public void RecordCoverage_WhenSecondInstanceSharesBackingStore_ShouldReadPersistedCoverage()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        MemoryDistributedCache backingStore = CreateBackingStore();

        DistributedExchangeRateCache writer = CreateCache(backingStore);
        writer.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);

        DistributedExchangeRateCache reader = CreateCache(backingStore);
        DateRangeCoverage coverage = reader.GetCoverage(Pair, Duration, now);

        Assert.IsTrue(coverage.Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10)));
    }

    /// <summary>
    /// Verifies that both the rate and coverage halves persist together so a second instance sees the full state.
    /// </summary>
    [TestMethod]
    public void StoreAndRecordCoverage_WhenSecondInstanceSharesBackingStore_ShouldReadBothHalves()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        MemoryDistributedCache backingStore = CreateBackingStore();

        DistributedExchangeRateCache writer = CreateCache(backingStore);
        writer.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);
        writer.RecordCoverage(Pair, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10), Duration, now);

        DistributedExchangeRateCache reader = CreateCache(backingStore);

        Assert.HasCount(1, reader.GetRates(Pair, Duration, now));
        Assert.IsTrue(reader.GetCoverage(Pair, Duration, now).Contains(new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 10)));
    }

    /// <summary>
    /// Verifies that the cache writes its blob under the stable, collision-free key <c>{provider}:{from}{to}</c> when no
    /// prefix is configured, so the key layout is predictable and shared across instances.
    /// </summary>
    [TestMethod]
    public void Store_WhenNoPrefixConfigured_ShouldWriteUnderProviderColonPairKey()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        MemoryDistributedCache backingStore = CreateBackingStore();
        var cache = new DistributedExchangeRateCache(backingStore, new DistributedExchangeRateCacheOptions { Provider = Provider });

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        // The expected key is "{provider}:{from}{to}" with no prefix; read the raw blob straight from the backing store.
        Assert.IsNotNull(backingStore.Get("Test:AUDUSD"));
    }

    /// <summary>
    /// Verifies that a configured key prefix is prepended verbatim to the cache key, so the entry is namespaced within a
    /// shared distributed store.
    /// </summary>
    [TestMethod]
    public void Store_WhenPrefixConfigured_ShouldWriteUnderPrefixedKey()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        MemoryDistributedCache backingStore = CreateBackingStore();
        var cache = new DistributedExchangeRateCache(backingStore, new DistributedExchangeRateCacheOptions { Provider = Provider, KeyPrefix = "fx:" });

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        Assert.IsNotNull(backingStore.Get("fx:Test:AUDUSD"));
        Assert.IsNull(backingStore.Get("Test:AUDUSD"));
    }

    /// <summary>
    /// Verifies that distinct currency pairs map to distinct keys, so rates for one pair never overwrite or leak into
    /// another within the same backing store.
    /// </summary>
    [TestMethod]
    public void Store_WhenDifferentPairs_ShouldUseDistinctKeys()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        MemoryDistributedCache backingStore = CreateBackingStore();
        DistributedExchangeRateCache cache = CreateCache(backingStore);
        var other = new ExchangeRatePair(CurrencyCode.EUR, CurrencyCode.USD);

        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);
        cache.Store(other, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 1.1000m, now) }, Duration, now);

        Assert.IsNotNull(backingStore.Get("Test:AUDUSD"));
        Assert.IsNotNull(backingStore.Get("Test:EURUSD"));
        Assert.AreEqual(0.5000m, cache.GetRates(Pair, Duration, now)[0].Rate);
        Assert.AreEqual(1.1000m, cache.GetRates(other, Duration, now)[0].Rate);
    }

    /// <summary>
    /// Verifies that a hand-written pre-C blob — one whose rate object has no <c>observedAtUtc</c> property — reads back
    /// with a <see langword="null" /> <see cref="CachedExchangeRate.ObservedAtUtc" /> and without error, confirming
    /// backward compatibility with blobs written before the upstream fetch instant was tracked.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenBlobHasNoObservedAtUtc_ShouldReadRowWithNullObservedAtUtc()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        MemoryDistributedCache backingStore = CreateBackingStore();
        DistributedExchangeRateCache cache = CreateCache(backingStore);

        // A hand-written legacy blob in the Web (camelCase) shape, with a rate object that predates observedAtUtc.
        string legacyJson =
            "{\"rates\":[{\"date\":\"2023-01-03\",\"rate\":\"0.5000\",\"cachedAtUtc\":\""
            + now.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
            + "\"}],\"coverage\":[]}";
        backingStore.Set("Test:AUDUSD", System.Text.Encoding.UTF8.GetBytes(legacyJson));

        IReadOnlyList<CachedExchangeRate> rows = cache.GetRates(Pair, Duration, now);

        Assert.HasCount(1, rows);
        Assert.AreEqual(0.5000m, rows[0].Rate);
        Assert.IsNull(rows[0].ObservedAtUtc);
    }
}
