// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderTests.HistoryAvailability.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateProviderTests
{
    /// <summary>
    /// Verifies that with the default feed catalogue — which includes the full-history feed — the provider advertises
    /// a fixed floor at the euro reference-rate epoch, 4 January 1999.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenDefaultFeeds_ShouldReportSinceEpoch()
    {
        (EcbRateProvider provider, _) = Create();

        Assert.AreEqual(RateHistoryAvailabilityKind.Since, provider.HistoryAvailability.Kind);
        Assert.AreEqual(EcbRateFeed.Epoch, provider.HistoryAvailability.EarliestDate);
        Assert.AreEqual(new DateOnly(1999, 1, 4), provider.HistoryAvailability.EarliestDate);
    }

    /// <summary>
    /// Verifies that when the feed catalogue is restricted to rolling feeds, the provider advertises a rolling window
    /// matching the deepest configured feed.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenOnlyRollingFeeds_ShouldReportDeepestRollingWindow()
    {
        EcbRateProviderOptions options = new()
        {
            Feeds = [EcbRateFeed.Daily, EcbRateFeed.Last90Days],
            EnableDiskCache = false,
        };
        EcbRateProvider provider = new(new FixtureEcbRateTableSource(options), options);

        Assert.AreEqual(RateHistoryAvailabilityKind.Rolling, provider.HistoryAvailability.Kind);
        Assert.AreEqual(90, provider.HistoryAvailability.WindowDays);
    }
}
