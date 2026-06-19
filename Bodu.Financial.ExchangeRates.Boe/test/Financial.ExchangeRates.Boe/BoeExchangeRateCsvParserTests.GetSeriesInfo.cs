// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateCsvParserTests.GetSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Financial.ExchangeRates.Boe;

public partial class BoeExchangeRateCsvParserTests
{
    /// <summary>
    /// Verifies that the distinct quote currencies are surfaced as GBP-based series with their codes.
    /// </summary>
    [TestMethod]
    public void GetSeriesInfo_WhenSampleResponse_ShouldReturnGbpPairsWithCodes()
    {
        BoeExchangeRateTable table = ParseSample();

        BoeSeriesInfo usd = table.GetSeriesInfo().Single(s => s.QuoteIsoCode == "USD");

        Assert.AreEqual(new ExchangeRatePair("GBP", "USD"), usd.Pair);
        Assert.AreEqual("XUDLUSS", usd.SeriesCode);
    }
}
