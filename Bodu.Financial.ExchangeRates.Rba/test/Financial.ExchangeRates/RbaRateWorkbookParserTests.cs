// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateWorkbookParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Formats.Excel;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="RbaRateWorkbookParser" /> interprets the RBA workbook layout correctly.
/// </summary>
[TestClass]
public partial class RbaRateWorkbookParserTests
{
    /// <summary>
    /// Parses the embedded sample workbook with default options.
    /// </summary>
    /// <returns>The parsed table.</returns>
    private static RbaRateTable ParseSample()
    {
        using MemoryStream stream = RbaFixtures.OpenStream(RbaFixtures.Sample);
        using var workbook = ExcelBinaryWorkbook.OpenRead(stream, leaveOpen: true);
        return RbaRateWorkbookParser.Parse(workbook, new RbaRateProviderOptions());
    }

    /// <summary>
    /// Builds the workbook a synthetic sheet describes and parses it with default options.
    /// </summary>
    /// <param name="sheet">The content of the workbook.</param>
    /// <returns>The parsed table.</returns>
    private static RbaRateTable Parse(RbaTestSheet sheet)
    {
        using MemoryStream stream = new(RbaTestWorkbook.Build(sheet), writable: false);
        using var workbook = ExcelBinaryWorkbook.OpenRead(stream, leaveOpen: true);
        return RbaRateWorkbookParser.Parse(workbook, new RbaRateProviderOptions());
    }

    /// <summary>
    /// Creates the content of a small F11.1 workbook: a US dollar column and a trade-weighted index column over three
    /// days, with every header row.
    /// </summary>
    /// <returns>The content of the workbook.</returns>
    private static RbaTestSheet CreateSheet()
    {
        DateOnly published = new(2024, 1, 4);

        return new RbaTestSheet
        {
            Banner = "F11.1  EXCHANGE RATES ",
            Dates = [new DateOnly(2024, 1, 2), new DateOnly(2024, 1, 3), published],
            Columns =
            [
                new RbaTestColumn
                {
                    Title = "A$1=USD",
                    Description = "AUD/USD Exchange Rate; see notes for further detail.",
                    Units = "USD",
                    Source = "WM/Reuters",
                    PublicationDate = published,
                    SeriesId = "FXRUSD",
                    Cells = [RbaTestCell.FromNumber(0.6812m), RbaTestCell.FromNumber(0.6735m), RbaTestCell.FromNumber(0.6704m)],
                },
                new RbaTestColumn
                {
                    Title = "Trade-weighted Index May 1970 = 100",
                    Description = "Australian Dollar Trade-weighted Index",
                    Units = "Index",
                    Source = "RBA",
                    PublicationDate = published,
                    SeriesId = "FXRTWI",
                    Decimals = 1,
                    Cells = [RbaTestCell.FromNumber(61.3m), RbaTestCell.FromNumber(61.1m), RbaTestCell.FromNumber(60.9m)],
                },
            ],
        };
    }

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
    /// Verifies that enumerated rates reproduce known published values for the first and last data rows.
    /// </summary>
    [TestMethod]
    public void EnumerateRates_WhenSampleWorkbook_ShouldReproduceKnownAnchors()
    {
        var rates = ParseSample()
            .EnumerateRates()
            .ToDictionary(r => (r.To, r.Date), r => r.Rate);

        Assert.AreEqual(0.6828m, rates[(CurrencyCode.USD, new DateOnly(2023, 1, 3))]);
        Assert.AreEqual(0.7029m, rates[(CurrencyCode.USD, new DateOnly(2026, 6, 12))]);
        Assert.AreEqual(112.71m, rates[(CurrencyCode.JPY, new DateOnly(2026, 6, 12))]);
    }

    /// <summary>
    /// Verifies that blank cells produce no observation: on the first data row the USD rate is present while the UAE
    /// dirham column, which is blank on that row, yields no observation.
    /// </summary>
    [TestMethod]
    public void EnumerateRates_WhenCellIsBlank_ShouldNotEmitObservation()
    {
        var firstRow = new DateOnly(2023, 1, 3);
        var rates = ParseSample().EnumerateRates().ToList();

        Assert.Contains(r => r.To == CurrencyCode.USD && r.Date == firstRow, rates);
        Assert.DoesNotContain(r => r.To == CurrencyCode.AED && r.Date == firstRow, rates);
    }
}
