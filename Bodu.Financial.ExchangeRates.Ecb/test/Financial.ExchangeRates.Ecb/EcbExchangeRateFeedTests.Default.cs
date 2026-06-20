// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateFeedTests.Default.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

public partial class EcbExchangeRateFeedTests
{
    /// <summary>
    /// Verifies that the default catalogue is ordered from the narrowest look-back to the full history.
    /// </summary>
    [TestMethod]
    public void Default_ShouldBeOrderedNarrowestToWidest()
    {
        IReadOnlyList<EcbExchangeRateFeed> feeds = EcbExchangeRateFeed.Default;

        Assert.AreEqual(2, feeds.Count);
        Assert.AreEqual(90, feeds[0].LookbackDays);
        Assert.IsTrue(feeds[1].IsFullHistory);
    }
}
