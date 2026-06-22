// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateFeedTests.ForDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbExchangeRateFeedTests
{
    /// <summary>
    /// Verifies that a recent date resolves to the narrowest covering feed.
    /// </summary>
    [TestMethod]
    public void ForDate_WhenRecentDate_ShouldSelectNarrowestFeed()
    {
        var feed = EcbExchangeRateFeed.ForDate(s_asOf.AddDays(-10), EcbExchangeRateFeed.Default, s_asOf);

        Assert.AreSame(EcbExchangeRateFeed.Last90Days, feed);
    }

    /// <summary>
    /// Verifies that an older date falls through to the full-history feed.
    /// </summary>
    [TestMethod]
    public void ForDate_WhenOlderDate_ShouldSelectFullHistory()
    {
        var feed = EcbExchangeRateFeed.ForDate(new DateOnly(2010, 5, 1), EcbExchangeRateFeed.Default, s_asOf);

        Assert.AreSame(EcbExchangeRateFeed.Full, feed);
    }

    /// <summary>
    /// Verifies that a date before the euro reference-rate epoch is not covered by any default feed.
    /// </summary>
    [TestMethod]
    public void ForDate_WhenBeforeEpoch_ShouldReturnNull()
    {
        var feed = EcbExchangeRateFeed.ForDate(new DateOnly(1998, 1, 1), EcbExchangeRateFeed.Default, s_asOf);

        Assert.IsNull(feed);
    }
}
