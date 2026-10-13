// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderTests.Publication.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateProviderTests
{
    /// <summary>The event id of the <c>ObservationIngested</c> diagnostic, logged once per observation while a load publishes.</summary>
    private const int ObservationIngestedEventId = 4105;

    /// <summary>
    /// Verifies that preloading a history feed whose older days quote a currency the ISO 4217 catalog no longer lists
    /// (the Turkish lira, <c>TRL</c>) skips those cells and publishes the rest of the feed.
    /// </summary>
    [TestMethod]
    public async Task PreloadAsync_WhenFeedQuotesRetiredCurrency_ShouldSkipItAndPublishTheRest()
    {
        EcbRateProviderOptions options = new() { EnableDiskCache = false };
        FixtureEcbRateTableSource source = new(options, EcbFixtures.RetiredCurrency);
        using EcbRateProvider provider = new(source, options);

        await provider.PreloadAsync();

        Assert.AreEqual(1.3621m, provider.GetRate("EUR", "USD", new DateOnly(2004, 12, 31)).Rate.Rate);
        Assert.AreEqual(1.8260m, provider.GetRate("EUR", "TRY", new DateOnly(2005, 1, 3)).Rate.Rate);
        Assert.HasCount(2, provider.GetAvailablePairs());
    }

    /// <summary>
    /// Verifies that a feed whose publication fails part-way is not recorded as loaded, so the next request downloads
    /// and publishes it again.
    /// </summary>
    [TestMethod]
    public async Task LoadFeedAsync_WhenPublishFails_ShouldLoadTheFeedAgainOnTheNextRequest()
    {
        EcbRateProviderOptions options = new() { EnableDiskCache = false };
        FixtureEcbRateTableSource source = new(options);
        FaultingLogger logger = new(ObservationIngestedEventId);
        using EcbRateProvider provider = new(source, options, logger);
        EcbRateFeed feed = new("hist", "eurofxref-hist.xml", null);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await provider.LoadFeedAsync(feed);
        });

        logger.IsFaulting = false;
        await provider.LoadFeedAsync(feed);

        Assert.AreEqual(2, source.GetTableCallCount);
        Assert.AreEqual(1.0545m, provider.GetRate("EUR", "USD", new DateOnly(2023, 1, 3)).Rate.Rate);
    }
}
