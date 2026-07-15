// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderTests.LookupResolution.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that a lookup served from cached rows applies the full date-resolution contract — previous/next/nearest
/// selection, tie handling, the tolerance clamp, same-currency identity, and the direct-before-inverse preference —
/// identically to a lookup resolved by a live provider.
/// </summary>
public sealed partial class CachingRateProviderTests
{
    /// <summary>The requested pair used by the resolution tests.</summary>
    private static readonly CurrencyPair AudUsd = new(CurrencyCode.AUD, CurrencyCode.USD);

    /// <summary>
    /// Verifies that a previous-on-or-before lookup serves the closest earlier cached observation with its offset.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenServedFromCacheWithPreviousResolution_ShouldSelectEarlierObservation()
    {
        SeedCache(AudUsd, (new DateOnly(2023, 1, 3), 0.51m), (new DateOnly(2023, 1, 6), 0.52m));
        CachingRateProvider decorator = CreateDecorator(InnerWith());

        bool served = decorator.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 5), RateLookupOptions.PreviousWithin(3), out RateLookupResult result);

        Assert.IsTrue(served);
        Assert.AreEqual(new DateOnly(2023, 1, 3), result.Rate.Date);
        Assert.AreEqual(0.51m, result.Rate.Rate);
        Assert.AreEqual(2, result.OffsetDays);
    }

    /// <summary>
    /// Verifies that a next-on-or-after lookup serves the closest later cached observation with its offset.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenServedFromCacheWithNextResolution_ShouldSelectLaterObservation()
    {
        SeedCache(AudUsd, (new DateOnly(2023, 1, 3), 0.51m), (new DateOnly(2023, 1, 6), 0.52m));
        CachingRateProvider decorator = CreateDecorator(InnerWith());

        bool served = decorator.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 5), RateLookupOptions.NextWithin(3), out RateLookupResult result);

        Assert.IsTrue(served);
        Assert.AreEqual(new DateOnly(2023, 1, 6), result.Rate.Date);
        Assert.AreEqual(0.52m, result.Rate.Rate);
        Assert.AreEqual(1, result.OffsetDays);
    }

    /// <summary>
    /// Verifies that a plain nearest lookup fails deterministically on an equidistant tie rather than picking a side.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenServedFromCacheWithNearestAndTie_ShouldMiss()
    {
        SeedCache(AudUsd, (new DateOnly(2023, 1, 3), 0.51m), (new DateOnly(2023, 1, 7), 0.52m));
        CachingRateProvider decorator = CreateDecorator(InnerWith());
        var nearest = new RateLookupOptions(RateDateResolution.Nearest, toleranceDays: 5);

        Assert.IsFalse(decorator.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 5), nearest, out _));
    }

    /// <summary>
    /// Verifies that a nearest-prefer-previous lookup resolves an equidistant tie to the earlier cached observation.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenServedFromCacheWithNearestPreferPreviousAndTie_ShouldSelectEarlier()
    {
        SeedCache(AudUsd, (new DateOnly(2023, 1, 3), 0.51m), (new DateOnly(2023, 1, 7), 0.52m));
        CachingRateProvider decorator = CreateDecorator(InnerWith());

        bool served = decorator.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 5), RateLookupOptions.NearestWithin(5), out RateLookupResult result);

        Assert.IsTrue(served);
        Assert.AreEqual(new DateOnly(2023, 1, 3), result.Rate.Date);
    }

    /// <summary>
    /// Verifies that a nearest-prefer-next lookup resolves an equidistant tie to the later cached observation.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenServedFromCacheWithNearestPreferNextAndTie_ShouldSelectLater()
    {
        SeedCache(AudUsd, (new DateOnly(2023, 1, 3), 0.51m), (new DateOnly(2023, 1, 7), 0.52m));
        CachingRateProvider decorator = CreateDecorator(InnerWith());
        var preferNext = new RateLookupOptions(RateDateResolution.NearestPreferNext, toleranceDays: 5);

        bool served = decorator.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 5), preferNext, out RateLookupResult result);

        Assert.IsTrue(served);
        Assert.AreEqual(new DateOnly(2023, 1, 7), result.Rate.Date);
    }

    /// <summary>
    /// Verifies the strict tolerance clamp: an offset equal to the tolerance serves, one day beyond it misses.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenServedFromCacheAtToleranceBoundary_ShouldServeOnlyWithinTolerance()
    {
        SeedCache(AudUsd, (new DateOnly(2023, 1, 3), 0.51m));
        CachingRateProvider decorator = CreateDecorator(InnerWith());

        Assert.IsTrue(decorator.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 5), RateLookupOptions.PreviousWithin(2), out _));
        Assert.IsFalse(decorator.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 5), RateLookupOptions.PreviousWithin(1), out _));
    }

    /// <summary>
    /// Verifies that a same-currency lookup over a pair with cached rows short-circuits to the synthetic identity rate.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenServedFromCacheForSameCurrency_ShouldReturnIdentityRate()
    {
        var audAud = new CurrencyPair(CurrencyCode.AUD, CurrencyCode.AUD);
        SeedCache(audAud, (new DateOnly(2023, 1, 5), 0.9m));
        CachingRateProvider decorator = CreateDecorator(InnerWith());

        bool served = decorator.TryGetRate("AUD", "AUD", new DateOnly(2023, 1, 5), RateLookupOptions.Exact, out RateLookupResult result);

        Assert.IsTrue(served);
        Assert.AreEqual(1m, result.Rate.Rate);
        Assert.AreEqual(FixedDatedRateProvider.IdentityProviderName, result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that the direct pair's cached row is preferred unconditionally when both directions are cached.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenBothDirectionsCached_ShouldPreferDirectRow()
    {
        SeedCache(AudUsd, (new DateOnly(2023, 1, 5), 0.5m));
        SeedCache(AudUsd.Inverse(), (new DateOnly(2023, 1, 5), 4.0m));
        CachingRateProvider decorator = CreateDecorator(InnerWith());

        bool served = decorator.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 5), RateLookupOptions.Exact, out RateLookupResult result);

        Assert.IsTrue(served);
        Assert.AreEqual(0.5m, result.Rate.Rate);
        Assert.IsFalse(result.Rate.IsInverted);
    }

    /// <summary>
    /// Verifies that an inverse-only cached pair serves the requested direction with the reciprocal multiplier and the
    /// inverted flag.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenOnlyInverseCached_ShouldServeReciprocal()
    {
        SeedCache(AudUsd.Inverse(), (new DateOnly(2023, 1, 5), 2.0m));
        CachingRateProvider decorator = CreateDecorator(InnerWith());

        bool served = decorator.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 5), RateLookupOptions.Exact, out RateLookupResult result);

        Assert.IsTrue(served);
        Assert.AreEqual(0.5m, result.Rate.Rate);
        Assert.IsTrue(result.Rate.IsInverted);
        Assert.AreEqual(CurrencyCode.AUD, result.Rate.From);
        Assert.AreEqual(CurrencyCode.USD, result.Rate.To);
    }

    /// <summary>
    /// Verifies that invalid lookup options are rejected when a cached serve is attempted, preserving the validation the
    /// snapshot provider previously performed.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenServedFromCacheWithInvalidOptions_ShouldThrowArgumentException()
    {
        SeedCache(AudUsd, (new DateOnly(2023, 1, 5), 0.5m));
        CachingRateProvider decorator = CreateDecorator(InnerWith());
        var invalid = new RateLookupOptions(RateDateResolution.Exact, toleranceDays: 3);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = decorator.TryGetRate("AUD", "USD", new DateOnly(2023, 1, 5), invalid, out _);
        });
    }
}
