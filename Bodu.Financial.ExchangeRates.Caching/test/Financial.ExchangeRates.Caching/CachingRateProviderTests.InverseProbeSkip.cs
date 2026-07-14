// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.InverseProbeSkip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies <see cref="CachingRateOptions.SkipInverseRangeProbeWhenDirectCovered" />: disabled (the default) pins the
/// historical always-probe behaviour, and enabled skips the inverse read whenever the direct pair holds any fresh
/// coverage while still probing — and serving — the inverse when the direct pair is completely empty.
/// </summary>
public sealed partial class CachingRateProviderTests
{
    /// <summary>The pair used by the inverse-probe tests.</summary>
    private static readonly CurrencyPair ProbePair = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>The requested window, deliberately outside any seeded partial coverage.</summary>
    private static readonly DateOnly ProbeStart = new(2023, 1, 3);

    /// <summary>The inclusive end of the requested window.</summary>
    private static readonly DateOnly ProbeEnd = new(2023, 1, 6);

    /// <summary>
    /// Verifies that with the option off (the default), a direct-coverage miss still probes the inverse pair —
    /// two snapshot reads — pinning the historical behaviour.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenSkipDisabledAndDirectMiss_ShouldProbeInverse()
    {
        var cache = new CountingSnapshotRateCache(Provider);
        cache.RecordCoverage(ProbePair, new DateOnly(2023, 2, 1), new DateOnly(2023, 2, 5), Duration, _clock.GetUtcNow());
        var sut = new CachingRateProvider(InnerWith(("AUD", "USD", ProbeStart, 0.5m)), cache, _options, _clock);

        _ = await sut.GetRatesAsync("AUD", "USD", ProbeStart, ProbeEnd);

        Assert.AreEqual(2, cache.SnapshotCount, "a direct miss must probe direct then inverse when the skip is off");
    }

    /// <summary>
    /// Verifies that with the option on, fresh direct coverage suppresses the inverse probe: one snapshot read, then
    /// the refetch proceeds.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenSkipEnabledAndDirectHasPartialCoverage_ShouldNotProbeInverse()
    {
        var cache = new CountingSnapshotRateCache(Provider);
        cache.RecordCoverage(ProbePair, new DateOnly(2023, 2, 1), new DateOnly(2023, 2, 5), Duration, _clock.GetUtcNow());
        _options.SkipInverseRangeProbeWhenDirectCovered = true;
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", ProbeStart, 0.5m));
        var sut = new CachingRateProvider(inner, cache, _options, _clock);

        IReadOnlyList<ExchangeRate> rates = await sut.GetRatesAsync("AUD", "USD", ProbeStart, ProbeEnd);

        Assert.AreEqual(1, cache.SnapshotCount, "fresh direct coverage must suppress the inverse probe");
        Assert.AreEqual(1, inner.GetRatesAsyncCallCount, "the miss must refetch from the inner provider");
        Assert.HasCount(1, rates);
    }

    /// <summary>
    /// Verifies that with the option on, a completely empty direct pair still probes the inverse, and a complete
    /// inverse coverage serves the window without an inner fetch.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenSkipEnabledAndDirectEmpty_ShouldStillServeFromInverse()
    {
        var cache = new CountingSnapshotRateCache(Provider);
        CurrencyPair inversePair = ProbePair.Inverse();
        cache.Store(inversePair, new[] { new CachedRate(ProbeStart, 2.0m, _clock.GetUtcNow()) }, Duration, _clock.GetUtcNow());
        cache.RecordCoverage(inversePair, ProbeStart, ProbeEnd, Duration, _clock.GetUtcNow());
        _options.SkipInverseRangeProbeWhenDirectCovered = true;
        CountingDatedRateProvider inner = InnerWith();
        var sut = new CachingRateProvider(inner, cache, _options, _clock);

        IReadOnlyList<ExchangeRate> rates = await sut.GetRatesAsync("AUD", "USD", ProbeStart, ProbeEnd);

        Assert.AreEqual(2, cache.SnapshotCount, "an empty direct pair must still probe the inverse");
        Assert.AreEqual(0, inner.GetRatesAsyncCallCount, "the covered inverse window must serve without an inner fetch");
        Assert.HasCount(1, rates);
        Assert.AreEqual(0.5m, rates[0].Rate, "the inverse rate must be reciprocated into the requested direction");
    }

    /// <summary>
    /// Verifies the non-snapshot fallback path: with the option on and partial direct coverage on a plain cache, the
    /// inverse coverage probe is skipped.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenSkipEnabledOnPlainCacheWithPartialCoverage_ShouldNotProbeInverseCoverage()
    {
        var cache = new CountingRateCache(Provider);
        cache.RecordCoverage(ProbePair, new DateOnly(2023, 2, 1), new DateOnly(2023, 2, 5), Duration, _clock.GetUtcNow());
        _options.SkipInverseRangeProbeWhenDirectCovered = true;
        var sut = new CachingRateProvider(InnerWith(("AUD", "USD", ProbeStart, 0.5m)), cache, _options, _clock);

        _ = await sut.GetRatesAsync("AUD", "USD", ProbeStart, ProbeEnd);

        Assert.AreEqual(1, cache.GetCoverageCount, "the inverse coverage probe must be skipped on the plain-cache path");
    }
}
