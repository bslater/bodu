// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.Metrics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Diagnostics;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the hit and miss counters the caching decorator emits through the
/// <c>Bodu.Financial.ExchangeRates.Caching</c> meter. Every test uses a provider name unique to itself and filters on
/// it, because the meter is process-wide and tests run in parallel.
/// </summary>
public sealed partial class CachingRateProviderTests
{
    /// <summary>
    /// Builds a decorator over a uniquely named provider so metric assertions cannot observe other tests' counts.
    /// </summary>
    /// <param name="provider">The unique provider name.</param>
    /// <param name="inner">The inner source.</param>
    /// <returns>The decorator.</returns>
    private CachingRateProvider CreateMeteredDecorator(string provider, IDatedRateProvider inner) =>
        new(inner, new InMemoryRateCache(provider), _options, _clock);

    /// <summary>
    /// Verifies that a single-date miss then hit increment the miss and hit counters with the provider and
    /// single-lookup tags.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenMissThenHit_ShouldIncrementMissAndHitCounters()
    {
        string provider = $"metrics-{Guid.NewGuid():N}";
        using var collector = new TestMeterCollector(CachingMeter.MeterName);
        CachingRateProvider sut = CreateMeteredDecorator(provider, InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m)));

        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);
        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(1, collector.Sum("bodu.financial.rate_cache.misses", "provider", provider), "the first lookup must count one miss");
        Assert.AreEqual(1, collector.Sum("bodu.financial.rate_cache.hits", "provider", provider), "the second lookup must count one hit");
    }

    /// <summary>
    /// Verifies that a range refetch then covered serve increment the miss and hit counters with the range-lookup tag.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenRefetchThenCovered_ShouldIncrementRangeCounters()
    {
        string provider = $"metrics-{Guid.NewGuid():N}";
        using var collector = new TestMeterCollector(CachingMeter.MeterName);
        CachingRateProvider sut = CreateMeteredDecorator(provider, InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m)));

        _ = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4));
        _ = await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4));

        Assert.AreEqual(1, collector.Sum("bodu.financial.rate_cache.misses", "provider", provider));
        Assert.AreEqual(1, collector.Sum("bodu.financial.rate_cache.hits", "provider", provider));

        // The lookup-shape tag distinguishes the range surface from the single-date one.
        Assert.IsTrue(
            collector.Measurements("bodu.financial.rate_cache.hits")
                .Where(m => m.Tags.Any(t => t.Key == "provider" && Equals(t.Value, provider)))
                .All(m => m.Tags.Any(t => t.Key == "lookup" && Equals(t.Value, "range"))),
            "range serves must carry the range lookup tag");
    }

    /// <summary>
    /// Verifies that with no listener attached the instrumented paths still serve correctly — the counters are
    /// observability only.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenNoMeterListener_ShouldServeNormally()
    {
        string provider = $"metrics-{Guid.NewGuid():N}";
        CachingRateProvider sut = CreateMeteredDecorator(provider, InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m)));

        RateLookupResult result = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);

        Assert.AreEqual(0.5m, result.Rate.Rate);
    }
}
