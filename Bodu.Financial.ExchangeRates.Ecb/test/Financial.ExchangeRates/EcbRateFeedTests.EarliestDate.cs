// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateFeedTests.EarliestDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateFeedTests
{
    /// <summary>
    /// Verifies that the full-history feed reports the euro reference-rate epoch as its earliest date.
    /// </summary>
    [TestMethod]
    public void EarliestDate_WhenFullHistory_ShouldReturnEpoch()
    {
        Assert.AreEqual(new DateOnly(1999, 1, 4), EcbRateFeed.Full.EarliestDate(s_asOf));
    }

    /// <summary>
    /// Verifies that a look-back feed shifts its earliest date relative to the reference date.
    /// </summary>
    [TestMethod]
    public void EarliestDate_WhenLookback_ShouldShiftFromReferenceDate()
    {
        Assert.AreEqual(s_asOf.AddDays(-90), EcbRateFeed.Last90Days.EarliestDate(s_asOf));
    }
}
