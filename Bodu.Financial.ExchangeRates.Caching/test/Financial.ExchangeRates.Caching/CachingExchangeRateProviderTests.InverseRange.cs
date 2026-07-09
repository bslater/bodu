// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingExchangeRateProviderTests.InverseRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies that a range lookup that misses the direct pair's coverage is served from a complete inverse-pair coverage
/// by reciprocating each rate, matching the single-date surface, and that the behaviour is gated by the provider's
/// default lookup options.
/// </summary>
public sealed partial class CachingExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that when only the inverse pair has been fetched, a range request for the direct pair is served by
    /// inverting the cached inverse rows, without consulting the inner provider.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenOnlyInversePairCovered_ShouldServeInvertedWithoutFetch()
    {
        CurrencyPair inverse = new(CurrencyCode.USD, CurrencyCode.AUD);
        CountingDatedExchangeRateProvider inner = InnerWith();

        // Seed only the inverse pair's rows and a full coverage window; the direct AUD/USD pair has nothing cached.
        SeedCache(inverse, (new DateOnly(2023, 1, 3), 2.0m), (new DateOnly(2023, 1, 4), 2.5m));
        SeedCoverage(inverse, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        IReadOnlyList<ExchangeRate> rates = [.. await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 4))];

        // The request was satisfied from the inverse coverage, so the inner provider was never consulted.
        Assert.AreEqual(0, inner.GetRatesAsyncCallCount);
        Assert.HasCount(2, rates);

        Assert.AreEqual(CurrencyCode.AUD, rates[0].From);
        Assert.AreEqual(CurrencyCode.USD, rates[0].To);
        Assert.IsTrue(rates[0].IsInverted);

        Assert.AreEqual(new DateOnly(2023, 1, 3), rates[0].Date);
        Assert.AreEqual(0.5m, rates[0].Rate);   // 1 / 2.0
        Assert.AreEqual(new DateOnly(2023, 1, 4), rates[1].Date);
        Assert.AreEqual(0.4m, rates[1].Rate);   // 1 / 2.5
    }

    /// <summary>
    /// Verifies that the synchronous range surface also serves an inverse-covered window by inversion, mirroring the
    /// asynchronous surface.
    /// </summary>
    [TestMethod]
    public void GetRates_WhenOnlyInversePairCovered_ShouldServeInverted()
    {
        CurrencyPair inverse = new(CurrencyCode.USD, CurrencyCode.AUD);
        CountingDatedExchangeRateProvider inner = InnerWith();

        SeedCache(inverse, (new DateOnly(2023, 1, 3), 4.0m));
        SeedCoverage(inverse, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3));
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        RateRangeResult rates = sut.GetRates("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3));

        Assert.AreEqual(0, inner.GetRatesAsyncCallCount);
        Assert.AreEqual(1, rates.Count);
        Assert.AreEqual(0.25m, rates[0].Rate);   // 1 / 4.0
        Assert.IsTrue(rates[0].IsInverted);
    }

    /// <summary>
    /// Verifies that with inverse serving disabled on the provider's default lookup options, a direct-pair range miss
    /// refetches from the inner provider rather than inverting the cached inverse rows.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenInverseServingDisabled_ShouldNotUseInverseCoverage()
    {
        CurrencyPair inverse = new(CurrencyCode.USD, CurrencyCode.AUD);
        CountingDatedExchangeRateProvider inner = InnerWith(
            ("AUD", "USD", new DateOnly(2023, 1, 3), 0.5m));

        SeedCache(inverse, (new DateOnly(2023, 1, 3), 2.0m));
        SeedCoverage(inverse, new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3));

        // Disable inverse range serving through the provider's default lookup options.
        _options.DefaultLookupOptions = new RateLookupOptions(RateDateResolution.Exact, allowInverse: false);
        CachingExchangeRateProvider sut = CreateDecorator(inner);

        IReadOnlyList<ExchangeRate> rates = [.. await sut.GetRatesAsync("AUD", "USD", new DateOnly(2023, 1, 3), new DateOnly(2023, 1, 3))];

        // Inverse serving is off, so the direct miss refetches from the inner rather than inverting cached USD/AUD rows.
        Assert.AreEqual(1, inner.GetRatesAsyncCallCount);
        Assert.HasCount(1, rates);
        Assert.AreEqual(0.5m, rates[0].Rate);
        Assert.IsFalse(rates[0].IsInverted);
    }
}
