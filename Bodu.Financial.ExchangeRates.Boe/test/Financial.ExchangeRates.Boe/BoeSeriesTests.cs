// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeSeriesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// Verifies the default catalogue and construction guards of <see cref="BoeSeries" />.
/// </summary>
[TestClass]
public class BoeSeriesTests
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

    /// <summary>
    /// Verifies that constructing a series with a <see langword="null" /> code throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSeriesCodeIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BoeSeries("USD", null!, "US dollar into Sterling");
        });
    }
}
