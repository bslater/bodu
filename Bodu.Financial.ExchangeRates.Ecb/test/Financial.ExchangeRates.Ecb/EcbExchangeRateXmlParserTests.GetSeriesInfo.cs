// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateXmlParserTests.GetSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Financial.ExchangeRates.Ecb;

public partial class EcbExchangeRateXmlParserTests
{
    /// <summary>
    /// Verifies that the distinct quote currencies are surfaced as EUR-based series.
    /// </summary>
    [TestMethod]
    public void GetSeriesInfo_WhenSampleFeed_ShouldReturnDistinctEurPairs()
    {
        EcbExchangeRateTable table = ParseSample();

        var pairs = table.GetSeriesInfo().Select(s => s.Pair).ToList();

        Assert.AreEqual(3, pairs.Count);
        CollectionAssert.Contains(pairs, new ExchangeRatePair("EUR", "USD"));
        CollectionAssert.Contains(pairs, new ExchangeRatePair("EUR", "JPY"));
        CollectionAssert.Contains(pairs, new ExchangeRatePair("EUR", "GBP"));
    }
}
