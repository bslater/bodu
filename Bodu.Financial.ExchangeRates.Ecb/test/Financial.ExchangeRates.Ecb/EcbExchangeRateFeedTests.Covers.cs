// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateFeedTests.Covers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

public partial class EcbExchangeRateFeedTests
{
    /// <summary>
    /// Verifies that a look-back feed covers a date inside its window but not one before it.
    /// </summary>
    [TestMethod]
    public void Covers_WhenLookback_ShouldRespectWindow()
    {
        Assert.IsTrue(EcbExchangeRateFeed.Last90Days.Covers(s_asOf.AddDays(-30), s_asOf));
        Assert.IsFalse(EcbExchangeRateFeed.Last90Days.Covers(s_asOf.AddDays(-120), s_asOf));
    }
}
