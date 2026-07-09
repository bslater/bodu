// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateCsvParserTests.GetSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateCsvParserTests
{
    /// <summary>
    /// Verifies that the distinct quote currencies are surfaced as GBP-based series with their codes.
    /// </summary>
    [TestMethod]
    public void GetSeriesInfo_WhenSampleResponse_ShouldReturnGbpPairsWithCodes()
    {
        BoeRateTable table = ParseSample();

        BoeSeriesInfo usd = table.GetSeriesInfo().Single(s => s.QuoteIsoCode == "USD");

        Assert.AreEqual(new CurrencyPair(CurrencyCode.GBP, CurrencyCode.USD), usd.Pair);
        Assert.AreEqual("XUDLUSS", usd.SeriesCode);
    }
}
