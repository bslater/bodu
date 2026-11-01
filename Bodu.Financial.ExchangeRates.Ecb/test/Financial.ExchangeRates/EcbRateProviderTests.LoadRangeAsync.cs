// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderTests.LoadRangeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateProviderTests
{
    /// <summary>
    /// Verifies that a range whose end precedes its start is rejected with <see cref="ArgumentException" /> naming
    /// the end date, before any feed is requested.
    /// </summary>
    [TestMethod]
    public void LoadRangeAsync_WhenEndDatePrecedesStartDate_ShouldThrowArgumentException()
    {
        (EcbRateProvider provider, FixtureEcbRateTableSource source) = Create(allowSync: false);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = provider.LoadRangeAsync(new DateOnly(2023, 1, 31), new DateOnly(2023, 1, 1));
        });

        Assert.AreEqual("endDate", ex.ParamName);
        Assert.AreEqual(0, source.GetTableCallCount);
    }

    /// <summary>
    /// Verifies that a range starting before the euro reference-rate epoch, which no feed covers, loads the
    /// full-history feed, the widest in the catalog.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenNoFeedReachesTheStart_ShouldLoadTheFullHistoryFeed()
    {
        (EcbRateProvider provider, FixtureEcbRateTableSource source) = Create(allowSync: false);

        await provider.LoadRangeAsync(EcbRateFeed.Epoch.AddDays(-7), EcbRateFeed.Epoch);

        CollectionAssert.AreEqual(new[] { EcbRateFeed.Full.Name }, source.RequestedFeeds.ToArray());
    }

    /// <summary>
    /// Verifies that when no feed reaches the start of the range and the catalog holds no full-history feed, the
    /// last configured feed, the widest, is loaded.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenNoFeedReachesTheStartAndNoneIsFullHistory_ShouldLoadTheLastConfiguredFeed()
    {
        EcbRateProviderOptions options = new()
        {
            Feeds = [EcbRateFeed.Daily, EcbRateFeed.Last90Days],
            EnableDiskCache = false,
        };
        FixtureEcbRateTableSource source = new(options);
        MutableTimeProvider time = new(new DateTimeOffset(2023, 4, 1, 12, 0, 0, TimeSpan.Zero));
        using EcbRateProvider provider = new(source, options, logger: null, time);

        await provider.LoadRangeAsync(new DateOnly(2022, 1, 3), new DateOnly(2022, 1, 4));

        CollectionAssert.AreEqual(new[] { EcbRateFeed.Last90Days.Name }, source.RequestedFeeds.ToArray());
    }
}
