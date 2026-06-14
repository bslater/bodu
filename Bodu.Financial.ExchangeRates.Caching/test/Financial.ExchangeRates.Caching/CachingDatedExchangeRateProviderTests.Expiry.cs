// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingDatedExchangeRateProviderTests.Expiry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingDatedExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that once a cached rate ages past the duration, the next request re-delegates to the inner provider.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenCachedRowExpires_ShouldRefetch()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingDatedExchangeRateProvider sut = CreateDecorator(inner);

        _ = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact, out _);
        _clock.Advance(Duration + TimeSpan.FromHours(1));
        var found = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact, out _);

        Assert.IsTrue(found);
        Assert.AreEqual(2, inner.TryGetRateCallCount);
    }

    /// <summary>
    /// Verifies that a cached rate still within the duration is served without re-delegating to the inner provider.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenWithinDuration_ShouldNotRefetch()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingDatedExchangeRateProvider sut = CreateDecorator(inner);

        _ = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact, out _);
        _clock.Advance(Duration - TimeSpan.FromHours(1));
        var found = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact, out _);

        Assert.IsTrue(found);
        Assert.AreEqual(1, inner.TryGetRateCallCount);
    }
}
