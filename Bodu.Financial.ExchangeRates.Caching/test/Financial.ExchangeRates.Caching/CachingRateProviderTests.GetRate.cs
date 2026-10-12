// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.GetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingRateProviderTests
{
    /// <summary>
    /// Verifies that a fresh cached rate is returned by <see cref="CachingRateProvider.GetRate(string, string, DateOnly, RateLookupOptions?)" />
    /// without consulting the inner provider.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCacheFresh_ShouldServeWithoutInner()
    {
        CountingDatedRateProvider inner = InnerWith();
        SeedCache(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = CreateDecorator(inner);

        RateLookupResult result = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(0.5m, result.Rate.Rate);
        Assert.AreEqual(0, inner.TotalCallCount);
    }

    /// <summary>
    /// Verifies that a cache miss delegates to the inner provider and stores the resolved rate.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetRate_WhenCacheMiss_ShouldDelegateAndStore()
    {
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = CreateDecorator(inner);

        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        IReadOnlyList<CachedRate> cached = _cache.GetRates(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), Duration, _clock.GetUtcNow());
        Assert.HasCount(1, cached);
        Assert.AreEqual(0.5m, cached[0].Rate);
        Assert.AreEqual(1, inner.TotalCallCount);
    }

    /// <summary>
    /// Verifies that the inner provider's <see cref="KeyNotFoundException" /> propagates when no rate is available
    /// anywhere.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenHardMiss_ShouldPropagateKeyNotFoundException()
    {
        CountingDatedRateProvider inner = InnerWith();
        CachingRateProvider sut = CreateDecorator(inner);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);
        });
    }

    /// <summary>
    /// Verifies that the undated <see cref="CachingRateProvider.GetRate(string, string, RateLookupOptions?)" />
    /// resolves the most recent rate on or before the current UTC date, as the wrapped provider's undated lookup does,
    /// rather than requiring a rate on the current date itself.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenUndatedAndLatestRateIsBeforeToday_ShouldReturnMostRecentRate()
    {
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 5), 0.50m), ("AUD", "USD", new DateOnly(2023, 1, 6), 0.51m));
        CachingRateProvider sut = CreateDecorator(inner);

        RateLookupResult result = sut.GetRate("AUD", "USD", options: null);

        Assert.AreEqual(new DateOnly(2023, 1, 6), result.Rate.Date);
        Assert.AreEqual(0.51m, result.Rate.Rate);
    }
}
