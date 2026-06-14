// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingDatedExchangeRateProviderTests.Composite.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingDatedExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that when several sources can satisfy a request, the one supplied first wins.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenMultipleSourcesHaveRate_ShouldPreferFirstSupplied()
    {
        CountingDatedExchangeRateProvider first = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CountingDatedExchangeRateProvider second = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.6m));
        CachingDatedExchangeRateProvider sut = CreateDecorator(Source("First", first), Source("Second", second));

        var found = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(0.5m, result.Rate.Rate);
        Assert.AreEqual(0, second.TotalCallCount);
    }

    /// <summary>
    /// Verifies that a request the first source cannot satisfy falls through to the next source.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenFirstSourceHasNoRate_ShouldFallThroughToNext()
    {
        CountingDatedExchangeRateProvider first = InnerWith();
        CountingDatedExchangeRateProvider second = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.6m));
        CachingDatedExchangeRateProvider sut = CreateDecorator(Source("First", first), Source("Second", second));

        var found = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(0.6m, result.Rate.Rate);
        Assert.AreEqual(1, first.TryGetRateCallCount);
        Assert.AreEqual(1, second.TryGetRateCallCount);
    }

    /// <summary>
    /// Verifies that a per-source expiry override is honoured: a source with a shorter expiry re-fetches once its cached
    /// rate ages past that override, even though the default expiry has not elapsed.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSourceHasShorterExpiry_ShouldRefetchAfterOverride()
    {
        CountingDatedExchangeRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingExchangeRateOptions options = new() { DefaultExpiry = TimeSpan.FromHours(24) };
        options.ProviderExpiry["Fast"] = TimeSpan.FromHours(1);
        CachingDatedExchangeRateProvider sut = new(new[] { Source("Fast", inner) }, _cache, options, _clock);

        _ = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact, out _);
        _clock.Advance(TimeSpan.FromHours(2));
        _ = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact, out _);

        Assert.AreEqual(2, inner.TryGetRateCallCount);
    }

    /// <summary>
    /// Verifies that each source caches under its own name, so a rate fetched from one source is not served on behalf of
    /// another.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSourcesShareAPair_ShouldCachePerSource()
    {
        CountingDatedExchangeRateProvider first = InnerWith();
        CountingDatedExchangeRateProvider second = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.6m));
        CachingDatedExchangeRateProvider sut = CreateDecorator(Source("First", first), Source("Second", second));

        _ = sut.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 3), ExchangeRateLookupOptions.Exact, out _);

        IReadOnlyList<CachedExchangeRate> firstCached = _cache.GetRates("First", new ExchangeRatePair("AUD", "USD"), Duration, _clock.GetUtcNow());
        IReadOnlyList<CachedExchangeRate> secondCached = _cache.GetRates("Second", new ExchangeRatePair("AUD", "USD"), Duration, _clock.GetUtcNow());

        Assert.AreEqual(0, firstCached.Count);
        Assert.AreEqual(1, secondCached.Count);
    }
}
