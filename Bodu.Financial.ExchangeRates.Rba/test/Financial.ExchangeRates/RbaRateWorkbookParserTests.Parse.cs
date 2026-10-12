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

    /// <summary>
    /// Verifies that a workbook with no sheet named <c>Data</c>, in any case, is rejected with
    /// <see cref="ExchangeRateFormatException" /> naming the missing sheet.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDataSheetMissing_ShouldThrowExchangeRateFormatException()
    {
        RbaTestSheet sheet = CreateSheet() with { Name = "Rates" };

        var ex = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = Parse(sheet);
        });

        Assert.AreEqual(RbaResourceStrings.Format_Invalid_RbaDataSheetMissing, ex.Message);
    }

    /// <summary>
    /// Verifies that a data sheet with neither a <c>Units</c> row nor a <c>Title</c> row, from which a column's
    /// currency could be read, is rejected with <see cref="ExchangeRateFormatException" /> naming the header rows.
    /// </summary>
    [TestMethod]
    public void Parse_WhenUnitsAndTitleRowsMissing_ShouldThrowExchangeRateFormatException()
    {
        RbaTestSheet sheet = CreateSheet() with { HeaderRows = RbaTestHeaderRows.All & ~(RbaTestHeaderRows.Units | RbaTestHeaderRows.Title) };

        var ex = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = Parse(sheet);
        });

        Assert.AreEqual(RbaResourceStrings.Format_Invalid_RbaHeaderRows, ex.Message);
    }

    /// <summary>
    /// Verifies that a data sheet whose header rows are followed by no dated row is rejected with
    /// <see cref="ExchangeRateFormatException" /> naming the missing data rows.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNoDataRows_ShouldThrowExchangeRateFormatException()
    {
        RbaTestSheet template = CreateSheet();
        RbaTestSheet sheet = template with
        {
            Dates = [],
            Columns = [.. template.Columns.Select(static column => column with { Cells = [] })],
        };

        var ex = Assert.ThrowsExactly<ExchangeRateFormatException>(() =>
        {
            _ = Parse(sheet);
        });

        Assert.AreEqual(RbaResourceStrings.Format_Invalid_RbaNoDataRows, ex.Message);
    }

    /// <summary>
    /// Verifies that without a <c>Units</c> row a column's currency is read from its title, <c>A$1=USD</c> giving the
    /// US dollar, while a title without the <c>A$1=</c> prefix, the trade-weighted index's, gives no currency.
    /// </summary>
    [TestMethod]
    public void Parse_WhenUnitsRowMissing_ShouldResolveCurrencyFromTitle()
    {
        RbaTestSheet sheet = CreateSheet() with { HeaderRows = RbaTestHeaderRows.All & ~RbaTestHeaderRows.Units };

        RbaRateTable table = Parse(sheet);

        RbaRateSeries usd = table.Series.Single();
        Assert.AreEqual("USD", usd.CurrencyCode);
        Assert.AreEqual("FXRUSD", usd.SeriesId);
        Assert.AreEqual(string.Empty, usd.Units);
    }

    /// <summary>
    /// Verifies that a column quoting the Australian dollar itself, the base of every RBA rate, is not taken as a
    /// currency series.
    /// </summary>
    [TestMethod]
    public void Parse_WhenColumnIsAud_ShouldExcludeColumn()
    {
        RbaTestSheet template = CreateSheet();
        RbaTestColumn aud = template.Columns[0] with { Title = "A$1=AUD", Description = "AUD/AUD Exchange Rate", Units = "AUD", SeriesId = "FXRAUD" };
        RbaTestSheet sheet = template with { Columns = [.. template.Columns, aud] };

        RbaRateTable table = Parse(sheet);

        CollectionAssert.AreEqual(new[] { "USD" }, table.Series.Select(static series => series.CurrencyCode).ToArray());
    }

    /// <summary>
    /// Verifies that without a <c>Series ID</c> row each series is read with an empty series identifier.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSeriesIdRowMissing_ShouldReadEmptySeriesId()
    {
        RbaTestSheet sheet = CreateSheet() with { HeaderRows = RbaTestHeaderRows.All & ~RbaTestHeaderRows.SeriesId };

        RbaRateTable table = Parse(sheet);

        Assert.AreEqual(string.Empty, table.Series.Single().SeriesId);
    }

    /// <summary>
    /// Verifies that without a <c>Description</c> row each series is read with an empty description.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDescriptionRowMissing_ShouldReadEmptyDescription()
    {
        RbaTestSheet sheet = CreateSheet() with { HeaderRows = RbaTestHeaderRows.All & ~RbaTestHeaderRows.Description };

        RbaRateTable table = Parse(sheet);

        Assert.AreEqual(string.Empty, table.Series.Single().Description);
    }

    /// <summary>
    /// Verifies that a legacy column whose <c>Series ID</c> cell is blank, as the 1983-1998 workbooks leave those of
    /// their 11 legacy currencies, is read with an empty series identifier beside the identifiers of the other columns.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSeriesIdCellIsBlank_ShouldReadEmptySeriesId()
    {
        const string FileName = "1983-1986.xls";
        RbaTestSheet sheet = RbaFixtures.GetSheet(FileName);

        RbaRateTable table = ParseFixture(FileName);

        CollectionAssert.AreEqual(
            sheet.Columns.Where(static column => column.CurrencyCode is not null).Select(static column => column.SeriesId ?? string.Empty).ToArray(),
            table.Series.Select(static series => series.SeriesId).ToArray());
    }

    /// <summary>
    /// Verifies that title and description cells written as formulas, as the 2010-2013 workbook writes them, are read
    /// as the text their cached results hold.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTitlesAreFormulas_ShouldReadCachedText()
    {
        const string FileName = "2010-2013.xls";
        RbaTestSheet sheet = RbaFixtures.GetSheet(FileName);

        RbaRateTable table = ParseFixture(FileName);

        Assert.IsTrue(sheet.HeaderFormulas);
        CollectionAssert.AreEqual(
            sheet.Columns.Where(static column => column.CurrencyCode is not null).Select(static column => (column.Title, column.Description)).ToArray(),
            table.Series.Select(static series => (series.Title, series.Description)).ToArray());
    }

    /// <summary>
    /// Verifies that a dated row whose date serial is not a finite number, here NaN and positive infinity, is skipped,
    /// and the rows around it are read.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDateSerialIsNotFinite_ShouldSkipRow()
    {
        RbaTestSheet template = CreateSheet();
        RbaTestSheet sheet = template with { DateSerials = [double.NaN, RbaTestSheet.ToSerial(template.Dates[1]), double.PositiveInfinity] };

        RbaRateTable table = Parse(sheet);

        RbaRateRow row = table.Rows.Single();
        Assert.AreEqual(template.Dates[1], row.Date);
        Assert.AreEqual(0.6735m, row.Values[0]);
    }
}
