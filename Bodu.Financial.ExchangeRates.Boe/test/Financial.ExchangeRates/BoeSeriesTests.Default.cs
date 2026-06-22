// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeSeriesTests.Default.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Financial.ExchangeRates;

public partial class BoeSeriesTests
{
    /// <summary>
    /// Verifies that the default catalogue maps the US dollar to its IADB series code.
    /// </summary>
    [TestMethod]
    public void Default_ShouldMapUsdToXudluss()
    {
        BoeSeries usd = BoeSeries.Default.Single(s => s.QuoteIsoCode == "USD");

        Assert.AreEqual("XUDLUSS", usd.SeriesCode);
    }

    /// <summary>
    /// Verifies that the default catalogue is non-empty and free of duplicate currencies.
    /// </summary>
    [TestMethod]
    public void Default_ShouldHaveDistinctCurrencies()
    {
        var currencies = BoeSeries.Default.Select(s => s.QuoteIsoCode).ToList();

        Assert.IsTrue(currencies.Count > 0);
        Assert.AreEqual(currencies.Count, currencies.Distinct().Count());
    }
}
