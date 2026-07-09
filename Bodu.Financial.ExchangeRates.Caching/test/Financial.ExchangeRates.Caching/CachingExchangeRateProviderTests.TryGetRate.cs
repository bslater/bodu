// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProviderTests.TryGetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that a cache miss delegates to the inner provider and returns the resolved rate.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenCacheMiss_ShouldDelegateToInnerAndReturnTrue()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        bool found = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out RateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(0.5m, result.Rate.Rate);
        Assert.AreEqual(1, inner.TryGetRateCallCount);
    }

    /// <summary>
    /// Verifies that a resolved rate is stored so a repeat request is served from the cache without consulting the inner
    /// provider a second time.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenCalledTwice_ShouldServeSecondFromCache()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        _ = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _);
        bool found = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out RateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(0.5m, result.Rate.Rate);
        Assert.AreEqual(1, inner.TryGetRateCallCount);
    }

    /// <summary>
    /// Verifies that a fresh cached rate is served with no call to the inner provider.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenCacheFresh_ShouldServeWithoutInner()
    {
        CountingDatedExchangeRateProvider inner = InnerWith();
        SeedCache(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        bool found = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out RateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(0.5m, result.Rate.Rate);
        Assert.AreEqual(0, inner.TotalCallCount);
    }

    /// <summary>
    /// Verifies that a request resolvable by neither the cache nor the inner provider returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenNoRateAnywhere_ShouldReturnFalse()
    {
        CountingDatedExchangeRateProvider inner = InnerWith();
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        bool found = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out RateLookupResult result);

        Assert.IsFalse(found);
        Assert.AreEqual(default, result);
    }

    /// <summary>
    /// Verifies that a non-exact date-resolution policy is honoured against cached rows, serving the most recent rate on
    /// or before the requested date without consulting the inner provider.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenPreviousOnOrBeforeResolution_ShouldServeFromCache()
    {
        CountingDatedExchangeRateProvider inner = InnerWith();
        SeedCache(new CurrencyPair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        bool found = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 5), RateLookupOptions.PreviousWithin(7), out RateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(new DateOnly(2023, 1, 3), result.Rate.Date);
        Assert.AreEqual(0, inner.TotalCallCount);
    }
}
