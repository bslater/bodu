// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.ExpiryJitter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Caching;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the deterministic expiry jitter of the caching decorator: disabled by default (bit-identical behaviour),
/// shortening-only when enabled, keyed stably per provider and pair, and identical across provider instances.
/// </summary>
public sealed partial class CachingRateProviderTests
{
    /// <summary>The pair used by the jitter tests.</summary>
    private static readonly CurrencyPair JitterPair = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>
    /// Computes the same jitter key the decorator derives for a pair.
    /// </summary>
    /// <param name="pair">The pair.</param>
    /// <returns>The provider-and-pair jitter key.</returns>
    private static string JitterKey(CurrencyPair pair) => $"{Provider}:{pair.From}{pair.To}";

    /// <summary>
    /// Verifies that with the default zero jitter a row exactly at the freshness boundary behaves exactly as the
    /// configured duration dictates: fresh just inside, stale at the boundary.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenJitterDisabled_ShouldUseConfiguredExpiryExactly()
    {
        SeedCache(JitterPair, (new DateOnly(2023, 1, 3), 0.5m));
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = CreateDecorator(inner);

        _clock.Advance(Duration - TimeSpan.FromSeconds(1));
        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);
        Assert.AreEqual(0, inner.TotalCallCount, "just inside the configured expiry must still be a hit");

        _clock.Advance(TimeSpan.FromSeconds(1));
        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);
        Assert.AreEqual(1, inner.TotalCallCount, "exactly at the configured expiry must be a miss");
    }

    /// <summary>
    /// Verifies that enabled jitter shortens the pair's effective expiry deterministically: a row older than the
    /// jittered expiry — but still younger than the configured expiry — misses and refetches.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenJitterEnabled_ShouldExpireAtTheJitteredInstantNotTheConfiguredOne()
    {
        _options.ExpiryJitter = 0.5;
        TimeSpan effective = CacheFreshness.WithJitter(Duration, JitterKey(JitterPair), 0.5);
        Assert.IsTrue(effective < Duration, "the jitter must shorten the effective expiry for this key");

        SeedCache(JitterPair, (new DateOnly(2023, 1, 3), 0.5m));
        CountingDatedRateProvider inner = InnerWith(("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));
        CachingRateProvider sut = CreateDecorator(inner);

        _clock.Advance(effective - TimeSpan.FromSeconds(1));
        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);
        Assert.AreEqual(0, inner.TotalCallCount, "just inside the jittered expiry must still be a hit");

        _clock.Advance(TimeSpan.FromSeconds(2));
        _ = sut.GetRate("AUD", "USD", new DateOnly(2023, 1, 3), RateLookupOptions.Exact);
        Assert.AreEqual(1, inner.TotalCallCount, "past the jittered expiry — though inside the configured one — must be a miss");
    }

    /// <summary>
    /// Verifies that the jitter never extends the configured expiry and is stable per key across repeated evaluation.
    /// </summary>
    [TestMethod]
    public void WithJitter_WhenEvaluatedRepeatedly_ShouldBeStableAndNeverExtend()
    {
        TimeSpan first = CacheFreshness.WithJitter(Duration, JitterKey(JitterPair), 0.25);
        TimeSpan second = CacheFreshness.WithJitter(Duration, JitterKey(JitterPair), 0.25);

        Assert.AreEqual(first, second, "the jitter must be deterministic for a key");
        Assert.IsTrue(first <= Duration, "the jitter must only ever shorten the duration");
        Assert.IsTrue(first > TimeSpan.Zero);
    }

    /// <summary>
    /// Verifies that different pairs derive different effective expiries, which is the desynchronization the jitter
    /// exists to provide.
    /// </summary>
    [TestMethod]
    public void WithJitter_WhenKeysDiffer_ShouldSpreadEffectiveExpiries()
    {
        var other = new CurrencyPair(CurrencyCode.EUR, CurrencyCode.USD);

        TimeSpan a = CacheFreshness.WithJitter(Duration, JitterKey(JitterPair), 0.5);
        TimeSpan b = CacheFreshness.WithJitter(Duration, JitterKey(other), 0.5);

        Assert.AreNotEqual(a, b, "distinct keys must yield distinct effective expiries");
    }
}
