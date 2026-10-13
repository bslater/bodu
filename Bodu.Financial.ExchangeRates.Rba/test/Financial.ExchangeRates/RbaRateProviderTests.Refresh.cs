// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderTests.Refresh.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderTests
{
    /// <summary>
    /// Verifies that the open-ended current era, loaded before the RBA published today's rates, is downloaded again,
    /// for a request that reaches today, once the current-era refresh interval has passed.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenTodayWasNotYetPublished_ShouldLoadAgainAfterTheRefreshInterval()
    {
        DateOnly today = new(2023, 1, 4);
        MutableTimeProvider time = new(new DateTimeOffset(2023, 1, 4, 9, 0, 0, TimeSpan.Zero));
        RbaRateProviderOptions options = new() { EnableDiskCache = false, CurrentEraRefreshInterval = TimeSpan.FromHours(1) };
        FixtureRbaRateTableSource source = new(options) { PublishedThrough = new DateOnly(2023, 1, 3) };
        using RbaRateProvider provider = new(source, options, timeProvider: time);

        await provider.LoadRangeAsync(today, today);
        source.PublishedThrough = today;
        time.Advance(TimeSpan.FromHours(2));
        await provider.LoadRangeAsync(today, today);

        Assert.AreEqual(2, source.GetTableCallCount);
        Assert.IsTrue(provider.TryGetRate("AUD", "USD", today, RateLookupOptions.Exact, out _));
    }
}
