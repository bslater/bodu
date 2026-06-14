// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Contracts;

/// <summary>
/// Validates the <see cref="IExchangeRateCache" /> contract shared by every implementation: read-time freshness
/// filtering, write-time merge and prune, and resilience to missing data.
/// </summary>
/// <typeparam name="TCache">The concrete cache under test.</typeparam>
public abstract class ExchangeRateCacheContractTests<TCache>
    where TCache : IExchangeRateCache
{
    /// <summary>
    /// The provider identifier used by the contract tests.
    /// </summary>
    protected const string Provider = "Test";

    /// <summary>
    /// The currency pair used by the contract tests.
    /// </summary>
    protected static readonly ExchangeRatePair Pair = new("AUD", "USD");

    /// <summary>
    /// The duration used by the contract tests.
    /// </summary>
    protected static readonly TimeSpan Duration = TimeSpan.FromHours(24);

    /// <summary>
    /// Creates a fresh, empty cache instance for a single test.
    /// </summary>
    /// <returns>A new cache instance.</returns>
    protected abstract TCache CreateCache();

    /// <summary>
    /// Verifies that a read against an empty cache returns an empty result rather than throwing.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenNothingStored_ShouldReturnEmpty()
    {
        TCache cache = CreateCache();

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Provider, Pair, Duration, DateTimeOffset.UtcNow);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that a stored fresh rate is returned on read.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenStoredAndFresh_ShouldReturnRow()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Provider, Pair, Duration, now);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(0.5000m, result[0].Rate);
    }

    /// <summary>
    /// Verifies that a stored rate older than the duration is filtered out on read.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenStoredButStale_ShouldFilterOut()
    {
        TCache cache = CreateCache();
        var cachedAt = DateTimeOffset.UtcNow - TimeSpan.FromHours(48);
        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, cachedAt) }, Duration, cachedAt);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Provider, Pair, Duration, DateTimeOffset.UtcNow);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that freshness uses a strict less-than boundary: a rate cached exactly one duration ago is stale.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenAgeEqualsDuration_ShouldFilterOut()
    {
        TCache cache = CreateCache();
        var cachedAt = DateTimeOffset.UtcNow - Duration;
        var asOf = cachedAt + Duration;
        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, cachedAt) }, Duration, cachedAt);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Provider, Pair, Duration, asOf);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that storing the same date twice keeps the most recently cached value.
    /// </summary>
    [TestMethod]
    public void Store_WhenSameDateStoredTwice_ShouldKeepLatestCached()
    {
        TCache cache = CreateCache();
        var older = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
        var newer = DateTimeOffset.UtcNow;
        var date = new DateOnly(2023, 1, 3);

        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(date, 0.5000m, older) }, Duration, newer);
        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(date, 0.6000m, newer) }, Duration, newer);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Provider, Pair, Duration, newer);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(0.6000m, result[0].Rate);
    }

    /// <summary>
    /// Verifies that storing fresh rows merges with, rather than replaces, existing fresh rows for other dates.
    /// </summary>
    [TestMethod]
    public void Store_WhenDifferentDates_ShouldMergeAndOrderByDate()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;

        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 6), 0.5100m, now) }, Duration, now);
        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Provider, Pair, Duration, now);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 3), result[0].Date);
        Assert.AreEqual(new DateOnly(2023, 1, 6), result[1].Date);
    }

    /// <summary>
    /// Verifies that a write prunes previously stored rows that have since become stale.
    /// </summary>
    [TestMethod]
    public void Store_WhenExistingRowsStale_ShouldPruneOnWrite()
    {
        TCache cache = CreateCache();
        var stale = DateTimeOffset.UtcNow - TimeSpan.FromHours(48);
        var now = DateTimeOffset.UtcNow;

        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, stale) }, Duration, stale);
        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 6), 0.5100m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Provider, Pair, Duration, now);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(new DateOnly(2023, 1, 6), result[0].Date);
    }

    /// <summary>
    /// Verifies that storing an empty set leaves the cache unchanged.
    /// </summary>
    [TestMethod]
    public void Store_WhenEmpty_ShouldBeNoOp()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        cache.Store(Provider, Pair, Array.Empty<CachedExchangeRate>(), Duration, now);

        IReadOnlyList<CachedExchangeRate> result = cache.GetRates(Provider, Pair, Duration, now);
        Assert.AreEqual(1, result.Count);
    }

    /// <summary>
    /// Verifies that rates stored under one pair are not returned for a different pair.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenStoredUnderDifferentPair_ShouldNotLeak()
    {
        TCache cache = CreateCache();
        var now = DateTimeOffset.UtcNow;
        cache.Store(Provider, Pair, new[] { new CachedExchangeRate(new DateOnly(2023, 1, 3), 0.5000m, now) }, Duration, now);

        IReadOnlyList<CachedExchangeRate> other = cache.GetRates(Provider, new ExchangeRatePair("EUR", "USD"), Duration, now);

        Assert.AreEqual(0, other.Count);
    }
}
