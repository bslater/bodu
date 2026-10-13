// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PairWebRateProviderTests.Refresh.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Time;

namespace Bodu.Financial.ExchangeRates;

public partial class PairWebRateProviderTests
{
    /// <summary>The day the refresh tests treat as today.</summary>
    private static readonly DateOnly RefreshToday = new(2023, 1, 4);

    /// <summary>The day before <see cref="RefreshToday" />, which the source has already published.</summary>
    private static readonly DateOnly RefreshYesterday = new(2023, 1, 3);

    /// <summary>The instant at which the refresh tests make their first request, early on <see cref="RefreshToday" />.</summary>
    private static readonly DateTimeOffset RefreshMorning = new(2023, 1, 4, 8, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that a window ending today, loaded before the source published today's rate, is fetched again for
    /// today once the refresh interval has passed, so the rate published later the same day is served.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenTodayWasNotYetPublished_ShouldFetchTodayAgainAfterTheRefreshInterval()
    {
        var time = new MutableTimeProvider(RefreshMorning);
        var source = new StubPairSource(
            new RateObservation(RefreshYesterday, 0.6828m),
            new RateObservation(RefreshToday, 0.6900m))
        {
            PublishedThrough = RefreshYesterday,
        };
        using var provider = new TestPairWebRateProvider(source, new TestWebRateProviderOptions(), timeProvider: time);

        await provider.LoadPairAsync("AUD", "USD", RefreshYesterday, RefreshToday);
        source.PublishedThrough = RefreshToday;
        time.Advance(TimeSpan.FromHours(13));
        await provider.LoadPairAsync("AUD", "USD", RefreshYesterday, RefreshToday);

        Assert.AreEqual(2, source.CallCount);
        Assert.AreEqual((RefreshToday, RefreshToday), source.Requests[^1]);
        Assert.AreEqual(0.6900m, provider.GetRate("AUD", "USD", RefreshToday, RateLookupOptions.Exact).Rate.Rate);
    }

    /// <summary>
    /// Verifies that the undated lookup, made before the source published today's rate, serves yesterday's rate and,
    /// once the refresh interval has passed, fetches today again and serves the rate published since.
    /// </summary>
    [TestMethod]
    public async Task GetRateAsync_WhenTodayWasNotYetPublished_ShouldServeTodaysRateAfterTheRefreshInterval()
    {
        var time = new MutableTimeProvider(RefreshMorning);
        var source = new StubPairSource(
            new RateObservation(RefreshYesterday, 0.6828m),
            new RateObservation(RefreshToday, 0.6900m))
        {
            PublishedThrough = RefreshYesterday,
        };
        using var provider = new TestPairWebRateProvider(source, new TestWebRateProviderOptions(), timeProvider: time);

        RateLookupResult before = await provider.GetRateAsync("AUD", "USD");
        source.PublishedThrough = RefreshToday;
        time.Advance(TimeSpan.FromHours(13));
        RateLookupResult after = await provider.GetRateAsync("AUD", "USD");

        Assert.AreEqual(0.6828m, before.Rate.Rate);
        Assert.AreEqual(0.6900m, after.Rate.Rate);
    }
}
