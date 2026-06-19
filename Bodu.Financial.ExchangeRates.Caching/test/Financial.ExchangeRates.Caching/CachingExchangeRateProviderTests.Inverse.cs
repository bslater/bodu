// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProviderTests.Inverse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that a request for the inverse pair is served from the cached direct rows when inversion is allowed,
    /// without consulting the inner provider.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenInverseCachedAndAllowed_ShouldServeFromCache()
    {
        CountingDatedExchangeRateProvider inner = InnerWith();
        SeedCache(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        bool found = sut.TryGetRate("USD", "AUD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.IsTrue(result.Rate.IsInverted);
        Assert.AreEqual(2m, result.Rate.Rate);
        Assert.AreEqual(0, inner.TotalCallCount);
    }

    /// <summary>
    /// Verifies that inverse cached rows are not consulted when inversion is disallowed, so the request falls through to
    /// the inner provider.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenInverseCachedButDisallowed_ShouldDelegateToInner()
    {
        CountingDatedExchangeRateProvider inner = InnerWith();
        SeedCache(new ExchangeRatePair(CurrencyCode.AUD, CurrencyCode.USD), (new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        var options = new ExchangeRateLookupOptions(ExchangeRateDateResolution.Exact, allowInverse: false);
        bool found = sut.TryGetRate("USD", "AUD", new DateOnly(2023, 1, 3), options, out _);

        Assert.IsFalse(found);
        Assert.AreEqual(1, inner.TryGetRateCallCount);
    }
}
