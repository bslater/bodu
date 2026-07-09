// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateBookTests.GetSeries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateBookTests
{

    /// <summary>
    /// Verifies that <see cref="RateBook.GetSeries(CurrencyPair)" /> returns every series matching the
    /// pair regardless of provider.
    /// </summary>
    [TestMethod]
    public void GetSeries_WhenMultipleProvidersForSamePair_ShouldReturnAllProviders()
    {
        RateSeries rba = BuildSeries(s_usdAud, "RBA", 1.5m);
        RateSeries ecb = BuildSeries(s_usdAud, "ECB", 1.6m);
        RateSeries other = BuildSeries(s_eurAud, "RBA", 1.7m);
        RateBook book = new([rba, ecb, other]);

        var providers = book.GetSeries(s_usdAud).Select(s => s.Provider).ToHashSet();

        Assert.HasCount(2, providers);
        Assert.Contains("RBA", providers);
        Assert.Contains("ECB", providers);
    }
}
