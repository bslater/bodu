// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingRateProviderTests.GetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class AggregatingRateProviderTests
{
    /// <summary>
    /// Verifies that the undated <see cref="AggregatingRateProvider.GetRate(string, string, RateLookupOptions?)" />
    /// resolves the most recent rate on or before the current UTC date, as a child's own undated lookup does, rather
    /// than requiring a rate on the current date itself.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenUndatedAndLatestRateIsBeforeToday_ShouldReturnMostRecentRate()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2024, 1, 6, 0, 0, 0, TimeSpan.Zero));
        var aggregate = new AggregatingRateProvider(
            new[] { Named("ECB", ("AUD", "USD", D1, 0.65m), ("AUD", "USD", new DateOnly(2024, 1, 5), 0.66m)) },
            timeProvider: clock);

        RateLookupResult result = aggregate.GetRate("AUD", "USD");

        Assert.AreEqual(new DateOnly(2024, 1, 5), result.Rate.Date);
        Assert.AreEqual(0.66m, result.Rate.Rate);
    }

    /// <summary>
    /// Verifies that the undated
    /// <see cref="AggregatingRateProvider.GetRateAsync(string, string, RateLookupOptions?, CancellationToken)" />
    /// resolves the most recent rate on or before the current UTC date.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task GetRateAsync_WhenUndatedAndLatestRateIsBeforeToday_ShouldReturnMostRecentRate()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2024, 1, 6, 0, 0, 0, TimeSpan.Zero));
        var aggregate = new AggregatingRateProvider(
            new[] { Named("ECB", ("AUD", "USD", D1, 0.65m), ("AUD", "USD", new DateOnly(2024, 1, 5), 0.66m)) },
            timeProvider: clock);

        RateLookupResult result = await aggregate.GetRateAsync("AUD", "USD");

        Assert.AreEqual(new DateOnly(2024, 1, 5), result.Rate.Date);
        Assert.AreEqual(0.66m, result.Rate.Rate);
    }
}
