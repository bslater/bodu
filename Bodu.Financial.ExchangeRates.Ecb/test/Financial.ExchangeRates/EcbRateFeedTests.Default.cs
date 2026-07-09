// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateFeedTests.Default.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateFeedTests
{
    /// <summary>
    /// Verifies that the default catalogue is ordered from the narrowest look-back to the full history.
    /// </summary>
    [TestMethod]
    public void Default_ShouldBeOrderedNarrowestToWidest()
    {
        IReadOnlyList<EcbRateFeed> feeds = EcbRateFeed.Default;

        Assert.HasCount(2, feeds);
        Assert.AreEqual(90, feeds[0].LookbackDays);
        Assert.IsTrue(feeds[1].IsFullHistory);
    }
}
