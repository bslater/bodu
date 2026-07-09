// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProviderTests.GetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that a fresh cached rate is returned by <see cref="CachingExchangeRateProvider.GetRate(string, string, DateOnly, RateLookupOptions?)" />
    /// without consulting the inner provider.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenCacheFresh_ShouldServeWithoutInner()
    {
        CountingDatedExchangeRateProvider inner = InnerWith();
        SeedCache(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

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
        CountingDatedExchangeRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        IReadOnlyList<CachedExchangeRate> cached = _cache.GetRates(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), Duration, _clock.GetUtcNow());
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
        CountingDatedExchangeRateProvider inner = InnerWith();
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);
        });
    }
}
