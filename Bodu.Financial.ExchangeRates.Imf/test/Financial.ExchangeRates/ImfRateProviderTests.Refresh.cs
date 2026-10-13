// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfRateProviderTests.Refresh.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Time;

namespace Bodu.Financial.ExchangeRates;

public partial class ImfRateProviderTests
{
    /// <summary>
    /// Verifies that the current month's report, loaded before the IMF published today's rates, is downloaded again,
    /// for a request that reaches today, once the refresh interval has passed.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenTodayWasNotYetPublished_ShouldLoadAgainAfterTheRefreshInterval()
    {
        DateOnly today = new(2026, 4, 2);
        MutableTimeProvider time = new(new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero));
        ImfRateProviderOptions options = new() { EnableDiskCache = false, RefreshInterval = TimeSpan.FromHours(1) };
        FixtureImfRateTableSource source = new(options) { PublishedThrough = new DateOnly(2026, 4, 1) };
        using ImfRateProvider provider = new(source, options, timeProvider: time);

        await provider.LoadRangeAsync(today, today);
        source.PublishedThrough = today;
        time.Advance(TimeSpan.FromHours(2));
        await provider.LoadRangeAsync(today, today);

        Assert.AreEqual(2, source.GetTableCallCount);
        Assert.AreEqual(158.66m, provider.GetRate("USD", "JPY", today, RateLookupOptions.Exact).Rate.Rate);
    }

    /// <summary>
    /// Verifies that a month loaded after it closed is never downloaded again, however long ago it was loaded.
    /// </summary>
    [TestMethod]
    public async Task LoadMonthAsync_WhenMonthWasClosedWhenLoaded_ShouldNotLoadItAgain()
    {
        MutableTimeProvider time = new(new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero));
        ImfRateProviderOptions options = new() { EnableDiskCache = false, RefreshInterval = TimeSpan.FromHours(1) };
        FixtureImfRateTableSource source = new(options);
        using ImfRateProvider provider = new(source, options, timeProvider: time);

        await provider.LoadMonthAsync(new ImfReportMonth(2026, 4));
        time.Advance(TimeSpan.FromDays(30));
        await provider.LoadMonthAsync(new ImfReportMonth(2026, 4));

        Assert.AreEqual(1, source.GetTableCallCount);
    }
}
