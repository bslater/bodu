// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderTests.Refresh.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateProviderTests
{
    /// <summary>
    /// Verifies that a range ending today, loaded before the Bank of England published today's rate, is downloaded
    /// again once the refresh interval has passed, so the rate published later the same day is served.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenTodayWasNotYetPublished_ShouldLoadAgainAfterTheRefreshInterval()
    {
        DateOnly today = new(2023, 1, 4);
        MutableTimeProvider time = new(new DateTimeOffset(2023, 1, 4, 9, 0, 0, TimeSpan.Zero));
        BoeRateProviderOptions options = new() { EnableDiskCache = false, RefreshInterval = TimeSpan.FromHours(1) };
        FixtureBoeRateTableSource source = new(options) { PublishedThrough = new DateOnly(2023, 1, 3) };
        using BoeRateProvider provider = new(source, options, timeProvider: time);

        await provider.LoadRangeAsync(new DateOnly(2023, 1, 3), today);
        source.PublishedThrough = today;
        time.Advance(TimeSpan.FromHours(2));
        await provider.LoadRangeAsync(new DateOnly(2023, 1, 3), today);

        Assert.AreEqual(2, source.GetTableCallCount);
        Assert.AreEqual(1.2050m, provider.GetRate("GBP", "USD", today, RateLookupOptions.Exact).Rate.Rate);
    }
}
