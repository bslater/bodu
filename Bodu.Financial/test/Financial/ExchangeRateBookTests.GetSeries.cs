// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateBookTests.GetSeries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateBookTests
{

    /// <summary>
    /// Verifies that <see cref="ExchangeRateBook.GetSeries(ExchangeRatePair)" /> returns every series matching the
    /// pair regardless of provider.
    /// </summary>
    [TestMethod]
    public void GetSeries_WhenMultipleProvidersForSamePair_ShouldReturnAllProviders()
    {
        ExchangeRateSeries rba = BuildSeries(s_usdAud, "RBA", 1.5m);
        ExchangeRateSeries ecb = BuildSeries(s_usdAud, "ECB", 1.6m);
        ExchangeRateSeries other = BuildSeries(s_eurAud, "RBA", 1.7m);
        ExchangeRateBook book = new([rba, ecb, other]);

        var providers = book.GetSeries(s_usdAud).Select(s => s.Provider).ToHashSet();

        Assert.HasCount(2, providers);
        Assert.Contains("RBA", providers);
        Assert.Contains("ECB", providers);
    }
}
