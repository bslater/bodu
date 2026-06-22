// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateFeedTests.EarliestDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class EcbExchangeRateFeedTests
{
    /// <summary>
    /// Verifies that the full-history feed reports the euro reference-rate epoch as its earliest date.
    /// </summary>
    [TestMethod]
    public void EarliestDate_WhenFullHistory_ShouldReturnEpoch()
    {
        Assert.AreEqual(new DateOnly(1999, 1, 4), EcbExchangeRateFeed.Full.EarliestDate(s_asOf));
    }

    /// <summary>
    /// Verifies that a look-back feed shifts its earliest date relative to the reference date.
    /// </summary>
    [TestMethod]
    public void EarliestDate_WhenLookback_ShouldShiftFromReferenceDate()
    {
        Assert.AreEqual(s_asOf.AddDays(-90), EcbExchangeRateFeed.Last90Days.EarliestDate(s_asOf));
    }
}
