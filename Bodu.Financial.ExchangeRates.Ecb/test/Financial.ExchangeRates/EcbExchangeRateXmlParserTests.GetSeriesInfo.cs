// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateXmlParserTests.GetSeriesInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

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

        Assert.HasCount(3, pairs);
        CollectionAssert.Contains(pairs, new ExchangeRatePair(CurrencyCode.EUR, CurrencyCode.USD));
        CollectionAssert.Contains(pairs, new ExchangeRatePair(CurrencyCode.EUR, CurrencyCode.JPY));
        CollectionAssert.Contains(pairs, new ExchangeRatePair(CurrencyCode.EUR, CurrencyCode.GBP));
    }
}
