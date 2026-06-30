// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PairWebExchangeRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Verifies the shared per-pair machinery provided by <see cref="PairWebExchangeRateProvider{TSeries}" />: coverage
/// tracking, idempotent loading, pair discovery, and the rate stamping carried out by the base.
/// </summary>
[TestClass]
public class PairWebExchangeRateProviderTests
{
    private static readonly DateOnly RangeStart = new(2023, 1, 1);
    private static readonly DateOnly RangeEnd = new(2023, 1, 31);
    private static readonly DateOnly Known = new(2023, 1, 3);

    /// <summary>
    /// Verifies that loading the same pair and window twice fetches from the source only once, because the second call
    /// finds the window already covered.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenWindowAlreadyCovered_ShouldFetchOnce()
    {
        StubPairSource source = new(new ExchangeRateObservation(Known, 0.6828m));
        TestPairWebExchangeRateProvider provider = new(source, new TestWebExchangeRateProviderOptions());

        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);
        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        Assert.AreEqual(1, source.CallCount);
    }

    /// <summary>
    /// Verifies that a loaded observation resolves through the lookup surface, stamped with the provider's identifier.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_ThenGetRate_ShouldResolveStampedRate()
    {
        StubPairSource source = new(new ExchangeRateObservation(Known, 0.6828m));
        TestPairWebExchangeRateProvider provider = new(source, new TestWebExchangeRateProviderOptions());

        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        ExchangeRateLookupResult result = provider.GetRate("AUD", "USD", Known, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(0.6828m, result.Rate.Rate);
        Assert.AreEqual("TEST", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that a loaded pair is surfaced through <see cref="PairWebExchangeRateProvider{TSeries}.GetAvailablePairs" />.
    /// </summary>
    [TestMethod]
    public async Task GetAvailablePairs_AfterLoad_ShouldContainSeries()
    {
        StubPairSource source = new(new ExchangeRateObservation(Known, 0.6828m));
        TestPairWebExchangeRateProvider provider = new(source, new TestWebExchangeRateProviderOptions());

        Assert.IsEmpty(provider.GetAvailablePairs());

        await provider.LoadPairAsync("AUD", "USD", RangeStart, RangeEnd);

        CollectionAssert.Contains(provider.GetAvailablePairs().ToList(), "AUD/USD");
    }

    /// <summary>
    /// Verifies that an inverted range throws an <see cref="ArgumentException" /> before any fetch is attempted.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenRangeInverted_ShouldThrowArgumentException()
    {
        StubPairSource source = new();
        TestPairWebExchangeRateProvider provider = new(source, new TestWebExchangeRateProviderOptions());

        _ = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            await provider.LoadPairAsync("AUD", "USD", RangeEnd, RangeStart);
        });

        Assert.AreEqual(0, source.CallCount);
    }

    /// <summary>
    /// Verifies that the provider surfaces the history availability declared on its options rather than the base
    /// unbounded default.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_ShouldForwardOptionsValue()
    {
        StubPairSource source = new();
        TestWebExchangeRateProviderOptions options = new()
        {
            HistoryAvailability = ExchangeRateHistoryAvailability.RollingDays(90),
        };
        TestPairWebExchangeRateProvider provider = new(source, options);

        Assert.AreEqual(ExchangeRateHistoryAvailabilityKind.Rolling, provider.HistoryAvailability.Kind);
        Assert.AreEqual(90, provider.HistoryAvailability.WindowDays);
    }
}
