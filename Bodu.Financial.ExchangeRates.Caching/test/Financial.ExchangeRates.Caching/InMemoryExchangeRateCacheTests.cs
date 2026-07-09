// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryExchangeRateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the in-memory cache's expiry (via the shared freshness mechanism) and non-persistence.
/// </summary>
[TestClass]
public sealed class InMemoryExchangeRateCacheTests
{
    /// <summary>
    /// The currency pair used by the tests.
    /// </summary>
    private static readonly CurrencyPair Pair = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>
    /// Verifies that a stored rate is served while fresh and filtered out once it ages past the duration.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetRates_ShouldServeWhileFreshAndExpireAfterDuration()
    {
        InMemoryExchangeRateCache cache = new("RBA");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        cache.Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2024, 1, 3), 0.5m, now) }, TimeSpan.FromHours(1), now);

        IReadOnlyList<CachedExchangeRate> fresh = cache.GetRates(Pair, TimeSpan.FromHours(1), now);
        IReadOnlyList<CachedExchangeRate> expired = cache.GetRates(Pair, TimeSpan.FromHours(1), now + TimeSpan.FromHours(2));

        Assert.HasCount(1, fresh);
        Assert.IsEmpty(expired);
    }

    /// <summary>
    /// Verifies that the cache holds nothing across instances, confirming it is not persisted.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenReadByNewInstance_ShouldNotSeePreviousData()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        new InMemoryExchangeRateCache("RBA").Store(Pair, new[] { new CachedExchangeRate(new DateOnly(2024, 1, 3), 0.5m, now) }, TimeSpan.FromHours(1), now);

        InMemoryExchangeRateCache other = new("RBA");

        Assert.IsEmpty(other.GetRates(Pair, TimeSpan.FromHours(1), now));
    }
}
