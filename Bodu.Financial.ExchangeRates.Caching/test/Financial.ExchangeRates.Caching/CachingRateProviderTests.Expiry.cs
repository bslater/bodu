// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.Expiry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingRateProviderTests
{
    /// <summary>
    /// Verifies that once a cached rate ages past the duration, the next request re-delegates to the inner provider.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenCachedRowExpires_ShouldRefetch()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = CreateDecorator(inner);

        _ = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _);
        _clock.Advance(Duration + TimeSpan.FromHours(1));
        bool found = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _);

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
        CachingRateProvider sut = CreateDecorator(inner);

        _ = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _);
        _clock.Advance(Duration - TimeSpan.FromHours(1));
        bool found = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _);

        Assert.IsTrue(found);
        Assert.AreEqual(1, inner.TryGetRateCallCount);
    }

    /// <summary>
    /// Verifies that a per-provider expiry override is honoured: the provider re-fetches once its cached rate ages past
    /// the override, even though the default expiry has not elapsed.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenProviderHasShorterExpiry_ShouldRefetchAfterOverride()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingRateOptions options = new() { DefaultExpiry = TimeSpan.FromHours(24) };
        options.ProviderExpiry[Provider] = TimeSpan.FromHours(1);
        CachingRateProvider sut = new(inner, _cache, options, _clock);

        _ = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _);
        _clock.Advance(TimeSpan.FromHours(2));
        _ = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact, out _);

        Assert.AreEqual(2, inner.TryGetRateCallCount);
    }
}
