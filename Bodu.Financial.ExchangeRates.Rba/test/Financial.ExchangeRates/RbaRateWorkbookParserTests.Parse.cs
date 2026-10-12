// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateWorkbookParserTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateWorkbookParserTests
{
    /// <summary>
    /// Verifies that the parser discovers the AUD/USD series with its RBA series identifier.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Parse_WhenSampleWorkbook_ShouldDiscoverAudUsdSeries()
    {
        RbaRateTable table = ParseSample();

        RbaRateSeries usd = table.Series.Single(s => s.CurrencyCode == "USD");

        Assert.AreEqual("FXRUSD", usd.SeriesId);
    }

    /// <summary>
    /// Verifies that every discovered series resolves to a three-letter currency code, excluding the trade-weighted
    /// index column.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSampleWorkbook_ShouldExcludeTradeWeightedIndexColumn()
    {
        RbaRateTable table = ParseSample();

        Assert.IsTrue(table.Series.All(s => s.CurrencyCode.Length == 3));
        Assert.DoesNotContain(s => s.SeriesId == "FXRTWI", table.Series);
    }

    /// <summary>
    /// Verifies that the RBA "SDR" units label is aliased to the ISO 4217 code "XDR".
    /// </summary>
    [TestMethod]
    public void Parse_WhenSampleWorkbook_ShouldAliasSdrToXdr()
    {
        RbaRateTable table = ParseSample();

        RbaRateSeries xdr = table.Series.Single(s => s.CurrencyCode == "XDR");

        Assert.AreEqual("FXRSDR", xdr.SeriesId);
    }

    /// <summary>
    /// Verifies that the rows span the expected first and last observation dates.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSampleWorkbook_ShouldExposeRowsAcrossExpectedDateRange()
    {
        RbaRateTable table = ParseSample();

        Assert.AreEqual(new DateOnly(2023, 1, 3), table.Rows[0].Date);
        Assert.AreEqual(new DateOnly(2026, 6, 12), table.Rows[^1].Date);
    }

    /// <summary>
    /// Verifies that a data sheet whose name differs from <c>Data</c> only in case, here <c>data</c>, is read as a
    /// <c>Data</c> sheet is, rather than failing with <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDataSheetNameDiffersInCase_ShouldReadSheet()
    {
        RbaTestSheet sheet = CreateSheet() with { Name = "data" };

        RbaRateTable table = Parse(sheet);

        RbaRateSeries usd = table.Series.Single();
        Assert.AreEqual("USD", usd.CurrencyCode);
        Assert.AreEqual("FXRUSD", usd.SeriesId);
        CollectionAssert.AreEqual(sheet.Dates.ToArray(), table.Rows.Select(row => row.Date).ToArray());
        Assert.AreEqual(0.6812m, table.Rows[0].Values[0]);
    }
}
