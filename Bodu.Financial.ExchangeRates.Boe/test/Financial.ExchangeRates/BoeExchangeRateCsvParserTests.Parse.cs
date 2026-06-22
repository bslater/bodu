// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateCsvParserTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

public partial class BoeExchangeRateCsvParserTests
{
    /// <summary>
    /// Verifies that parsing the sample response yields one observation per present rate cell.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Parse_WhenSampleResponse_ShouldYieldOneObservationPerCell()
    {
        BoeExchangeRateTable table = ParseSample();

        Assert.AreEqual(6, table.Observations.Count);
    }

    /// <summary>
    /// Verifies that a parsed cell carries the daily spot rate quoted against the pound on its date.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSampleResponse_ShouldReadDateCurrencyAndRate()
    {
        BoeExchangeRateTable table = ParseSample();

        BoeExchangeRateObservation usd = table.Observations
            .Single(o => o.Date == new DateOnly(2023, 1, 3) && o.CurrencyCode == "USD");

        Assert.AreEqual(1.2065m, usd.Rate);
    }

    /// <summary>
    /// Verifies that an empty rate cell is skipped while the rest of the row is read.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCellEmpty_ShouldSkipCell()
    {
        string csv =
            "\"DATE\",\"XUDLUSS\",\"XUDLJYS\"\n" +
            "\"03 Jan 2023\",\"\",\"159.10\"\n";

        BoeExchangeRateTable table = BoeExchangeRateCsvParser.Parse(csv, new BoeExchangeRateOptions());

        Assert.AreEqual(1, table.Observations.Count);
        Assert.AreEqual("JPY", table.Observations[0].CurrencyCode);
    }

    /// <summary>
    /// Verifies that a column whose series code is not configured is ignored.
    /// </summary>
    [TestMethod]
    public void Parse_WhenColumnNotConfigured_ShouldIgnoreColumn()
    {
        string csv =
            "\"DATE\",\"XUDLUSS\",\"XUDLZZZ\"\n" +
            "\"03 Jan 2023\",\"1.2065\",\"9.9999\"\n";

        BoeExchangeRateTable table = BoeExchangeRateCsvParser.Parse(csv, new BoeExchangeRateOptions());

        Assert.AreEqual(1, table.Observations.Count);
        Assert.AreEqual("USD", table.Observations[0].CurrencyCode);
    }

    /// <summary>
    /// Verifies that a response lacking the <c>DATE</c> header is reported as a format failure.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHeaderMissing_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = BoeExchangeRateCsvParser.Parse("<html><body>Service unavailable</body></html>", new BoeExchangeRateOptions());
        });
    }
}
