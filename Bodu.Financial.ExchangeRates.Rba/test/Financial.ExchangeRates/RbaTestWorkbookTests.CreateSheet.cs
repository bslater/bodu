// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaTestWorkbookTests.CreateSheet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaTestWorkbookTests
{
    /// <summary>
    /// Verifies that the open-ended era's workbook runs from 3 January 2023 to 12 June 2026 and holds the rates the
    /// tests and documentation name: US dollar 0.6828 on its first day, and US dollar 0.7029 and yen 112.71 on its
    /// last.
    /// </summary>
    [TestMethod]
    public void CreateSheet_WhenOpenEra_ShouldHoldNamedRates()
    {
        RbaTestSheet sheet = RbaTestWorkbook.CreateSheet(GetEra("2023-current"));
        RbaTestColumn usd = sheet.Columns.Single(static column => column.Units == "USD");
        RbaTestColumn jpy = sheet.Columns.Single(static column => column.Units == "JPY");

        Assert.AreEqual(new DateOnly(2023, 1, 3), sheet.Dates[0]);
        Assert.AreEqual(new DateOnly(2026, 6, 12), sheet.Dates[^1]);
        Assert.AreEqual(RbaTestCell.FromNumber(0.6828m), usd.Cells[0]);
        Assert.AreEqual(RbaTestCell.FromNumber(0.7029m), usd.Cells[^1]);
        Assert.AreEqual(RbaTestCell.FromNumber(112.71m), jpy.Cells[^1]);
    }

    /// <summary>
    /// Verifies that across the eras the workbooks plant every cell and column the parser must cope with: each
    /// placeholder text, zero rates, doubles a unit in the last place from their rate, empty currency columns, columns
    /// that start late, end early, or stop and resume, the trade-weighted index and SDR columns in every era, and
    /// legacy columns with no series identifier.
    /// </summary>
    [TestMethod]
    public void CreateSheet_WhenAllEras_ShouldPlantEveryFeature()
    {
        RbaTestSheet[] sheets = [.. RbaEraWorkbook.Default.Select(RbaTestWorkbook.CreateSheet)];
        RbaTestColumn[] currencies = [.. sheets.SelectMany(static sheet => sheet.Columns).Where(static column => column.CurrencyCode is not null)];
        RbaTestCell[] cells = [.. sheets.SelectMany(static sheet => sheet.Columns).SelectMany(static column => column.Cells)];

        CollectionAssert.IsSubsetOf(
            new[] { "Closed", "CLOSED", RbaTestWorkbook.MissingValuePlaceholder, RbaTestWorkbook.UnavailablePlaceholder },
            cells.Where(static cell => cell.Kind == RbaTestCellKind.Text).Select(static cell => cell.Text).Distinct().ToArray());
        Assert.IsTrue(cells.Any(static cell => cell.Kind == RbaTestCellKind.Number && cell.Value == 0m), "zero rate");
        Assert.IsTrue(cells.Any(static cell => cell.Kind == RbaTestCellKind.Number && cell.StoredValue != (double)cell.Value), "double a unit in the last place away");
        Assert.IsTrue(currencies.Any(static column => !column.Cells.Any(IsRate)), "empty currency column");
        Assert.IsTrue(currencies.Any(static column => column.Cells.Any(IsRate) && FirstRate(column) > 20), "late-starting column");
        Assert.IsTrue(currencies.Any(static column => column.Cells.Any(IsRate) && LastRate(column) < column.Cells.Count - 20), "early-ending column");
        Assert.IsTrue(currencies.Any(static column => LongestPause(column) > 20), "column that stops and resumes");
        Assert.IsTrue(currencies.Any(static column => column.SeriesId is null), "legacy column with no series identifier");
        Assert.IsTrue(
            sheets.All(static sheet => sheet.Columns.Any(static column => column.Units == RbaTestWorkbook.IndexUnits && column.CurrencyCode is null)),
            "trade-weighted index column in every era");
        Assert.IsTrue(
            sheets.All(static sheet => sheet.Columns.Any(static column => column.Units == "SDR" && column.CurrencyCode == "XDR")),
            "SDR column in every era");
    }

    /// <summary>
    /// Reports whether a cell holds a rate, a positive number.
    /// </summary>
    /// <param name="cell">The cell.</param>
    /// <returns><see langword="true" /> when <paramref name="cell" /> is a number greater than zero.</returns>
    private static bool IsRate(RbaTestCell cell) =>
        cell.Kind == RbaTestCellKind.Number && cell.Value > 0m;

    /// <summary>
    /// Returns the index of a column's first rate.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns>The zero-based row index of the first rate, or -1 when the column has none.</returns>
    private static int FirstRate(RbaTestColumn column) =>
        column.Cells.ToList().FindIndex(IsRate);

    /// <summary>
    /// Returns the index of a column's last rate.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns>The zero-based row index of the last rate, or -1 when the column has none.</returns>
    private static int LastRate(RbaTestColumn column) =>
        column.Cells.ToList().FindLastIndex(IsRate);

    /// <summary>
    /// Returns the longest run of rows without a rate between two of a column's rates.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns>The length of the longest such run; zero when the column's rates are unbroken.</returns>
    private static int LongestPause(RbaTestColumn column)
    {
        int longest = 0;
        int run = 0;
        for (int row = FirstRate(column) + 1; row <= LastRate(column); row++)
        {
            run = IsRate(column.Cells[row]) ? 0 : run + 1;
            longest = Math.Max(longest, run);
        }

        return longest;
    }
}
