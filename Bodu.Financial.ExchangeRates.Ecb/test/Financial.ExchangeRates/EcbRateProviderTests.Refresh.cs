// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderTests.Refresh.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateProviderTests
{
    /// <summary>
    /// Verifies that a feed loaded before the ECB published today's reference rates is downloaded again, for a request
    /// that reaches today, once the refresh interval has passed.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenTodayWasNotYetPublished_ShouldLoadAgainAfterTheRefreshInterval()
    {
        DateOnly today = new(2023, 1, 4);
        MutableTimeProvider time = new(new DateTimeOffset(2023, 1, 4, 9, 0, 0, TimeSpan.Zero));
        EcbRateProviderOptions options = new() { EnableDiskCache = false, RefreshInterval = TimeSpan.FromHours(1) };
        FixtureEcbRateTableSource source = new(options) { PublishedThrough = new DateOnly(2023, 1, 3) };
        using EcbRateProvider provider = new(source, options, timeProvider: time);

        await provider.LoadRangeAsync(today, today);
        source.PublishedThrough = today;
        time.Advance(TimeSpan.FromHours(2));
        await provider.LoadRangeAsync(today, today);

        Assert.AreEqual(2, source.GetTableCallCount);
        Assert.AreEqual(1.0600m, provider.GetRate("EUR", "USD", today, RateLookupOptions.Exact).Rate.Rate);
    }
}
