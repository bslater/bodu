// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProviderTests.GetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that a fresh cached rate is returned by <see cref="CachingExchangeRateProvider.GetRate(string, string, DateOnly, ExchangeRateLookupOptions?)" />
    /// without consulting the inner provider.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCacheFresh_ShouldServeWithoutInner()
    {
        CountingDatedExchangeRateProvider inner = InnerWith();
        SeedCache(new ExchangeRatePair("AUD", "USD"), (new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        ExchangeRateLookupResult result = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

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
        CountingDatedExchangeRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);

        IReadOnlyList<CachedExchangeRate> cached = _cache.GetRates(new ExchangeRatePair("AUD", "USD"), Duration, _clock.GetUtcNow());
        Assert.AreEqual(1, cached.Count);
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
        CountingDatedExchangeRateProvider inner = InnerWith();
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact);
        });
    }
}
