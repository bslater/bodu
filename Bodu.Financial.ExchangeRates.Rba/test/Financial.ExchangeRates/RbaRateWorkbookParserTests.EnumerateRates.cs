// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateWorkbookParserTests.EnumerateRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateWorkbookParserTests
{
    /// <summary>
    /// Verifies that enumerated rates reproduce the rates the sample workbook holds on its first and last rows, the
    /// ones the tests and documentation name.
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

    /// <summary>
    /// Verifies that every cell of the 2010-2013 workbook that stores a double one unit in the last place from its rate
    /// is read as the rate itself, among them the parser's documented example, the rand on 4 January 2010 stored as
    /// <c>6.6754999999999995</c> for <c>6.6755</c>, and the riyal on 10 February 2011 stored as
    /// <c>3.7781000000000002</c> for <c>3.7781</c>.
    /// </summary>
    [TestMethod]
    public void EnumerateRates_WhenStoredOneUlpFromRate_ShouldRecoverRate()
    {
        const string FileName = "2010-2013.xls";
        RbaTestSheet sheet = RbaFixtures.GetSheet(FileName);
        Dictionary<(CurrencyCode To, DateOnly Date), decimal> expected = GetWorkbookCells(sheet, static cell => cell.StoredValue != (double)cell.Value);

        var rates = ParseFixture(FileName)
            .EnumerateRates()
            .ToDictionary(rate => (rate.To, rate.Date), rate => rate.Rate);

        Assert.AreEqual(6.6755m, rates[(CurrencyCode.ZAR, new DateOnly(2010, 1, 4))]);
        Assert.AreEqual(3.7781m, rates[(CurrencyCode.SAR, new DateOnly(2011, 2, 10))]);
        Assert.IsGreaterThan(2, expected.Count);
        foreach (((CurrencyCode To, DateOnly Date) key, decimal rate) in expected)
            Assert.AreEqual(rate, rates[key], $"{key.To} {key.Date:yyyy-MM-dd}");
    }

    /// <summary>
    /// Verifies that a cell holding placeholder text, as the RBA writes <c>Closed</c>, <c>CLOSED</c>, <c>--</c> and
    /// <c>N/A</c> on a day without a rate, yields no observation, in every era's workbook.
    /// </summary>
    [TestMethod]
    public void EnumerateRates_WhenCellHoldsPlaceholderText_ShouldNotEmitObservation()
    {
        List<(CurrencyCode To, DateOnly Date)> placeholders = [];
        List<(CurrencyCode To, DateOnly Date)> observations = [];
        foreach (RbaEraWorkbook era in RbaEraWorkbook.Default)
        {
            placeholders.AddRange(GetWorkbookCells(RbaFixtures.GetSheet(era.FileName), static cell => cell.Kind == RbaTestCellKind.Text).Keys);
            observations.AddRange(ParseFixture(era.FileName).EnumerateRates().Select(static rate => (rate.To, rate.Date)));
        }

        Assert.IsNotEmpty(placeholders);
        Assert.IsEmpty(placeholders.Intersect(observations));
    }

    /// <summary>
    /// Verifies that a cell holding a zero rate, which the 1991-1994 workbook plants, yields no observation, since a
    /// rate of zero cannot be quoted.
    /// </summary>
    [TestMethod]
    public void EnumerateRates_WhenRateIsZero_ShouldNotEmitObservation()
    {
        const string FileName = "1991-1994.xls";
        Dictionary<(CurrencyCode To, DateOnly Date), decimal> zeros = GetWorkbookCells(RbaFixtures.GetSheet(FileName), static cell => cell.Kind == RbaTestCellKind.Number && cell.Value == 0m);

        var observations = ParseFixture(FileName).EnumerateRates().Select(static rate => (rate.To, rate.Date)).ToList();

        Assert.IsNotEmpty(zeros);
        Assert.IsEmpty(zeros.Keys.Intersect(observations));
    }

    /// <summary>
    /// Verifies that a currency column holding no rate in an era, blank or without any record, is still read as a
    /// series but yields no observation, in every era's workbook.
    /// </summary>
    [TestMethod]
    public void EnumerateRates_WhenColumnHoldsNoRate_ShouldEmitNoObservationForIt()
    {
        int emptyColumns = 0;
        foreach (RbaEraWorkbook era in RbaEraWorkbook.Default)
        {
            RbaRateTable table = ParseFixture(era.FileName);
            HashSet<CurrencyCode> observed = [.. table.EnumerateRates().Select(static rate => rate.To)];
            foreach (RbaTestColumn column in RbaFixtures.GetSheet(era.FileName).Columns)
            {
                if (!CurrencyInfo.TryGetCurrencyCode(column.CurrencyCode ?? string.Empty, out CurrencyCode code) || column.Cells.Any(static cell => cell.Kind == RbaTestCellKind.Number && cell.Value > 0m))
                    continue;

                emptyColumns++;
                Assert.Contains(series => series.CurrencyCode == column.CurrencyCode, table.Series, $"{era.Label} {code} is read as a series");
                Assert.DoesNotContain(code, observed, $"{era.Label} {code} yields no observation");
            }
        }

        Assert.IsGreaterThan(0, emptyColumns);
    }

    /// <summary>
    /// Returns the cells of a workbook's currency columns that match a condition, keyed by the currency the provider
    /// serves them under and their date, with the rate each holds.
    /// </summary>
    /// <param name="sheet">The content of the workbook.</param>
    /// <param name="match">The condition a cell must meet.</param>
    /// <returns>The matching cells of every column whose currency the exchange-rate types model.</returns>
    private static Dictionary<(CurrencyCode To, DateOnly Date), decimal> GetWorkbookCells(RbaTestSheet sheet, Func<RbaTestCell, bool> match)
    {
        Dictionary<(CurrencyCode To, DateOnly Date), decimal> cells = [];
        foreach (RbaTestColumn column in sheet.Columns)
        {
            if (!CurrencyInfo.TryGetCurrencyCode(column.CurrencyCode ?? string.Empty, out CurrencyCode code))
                continue;

            for (int row = 0; row < sheet.Dates.Count; row++)
            {
                if (match(column.Cells[row]))
                    cells.Add((code, sheet.Dates[row]), column.Cells[row].Value);
            }
        }

        return cells;
    }
}
