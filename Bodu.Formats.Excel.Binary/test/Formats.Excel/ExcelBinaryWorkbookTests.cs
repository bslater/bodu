// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the behavior of <see cref="ExcelBinaryWorkbook" /> against the sample workbook the tests build and other
/// synthetic workbooks.
/// </summary>
[TestClass]
public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Opens the sample workbook the tests build.
    /// </summary>
    /// <returns>An open <see cref="ExcelBinaryWorkbook" /> over the sample.</returns>
    private static ExcelBinaryWorkbook OpenSample() =>
        ExcelBinaryWorkbook.OpenRead(ExcelBinaryFixtures.OpenSample());

    /// <summary>
    /// Reads every populated cell of the named sheet into a position-keyed dictionary.
    /// </summary>
    /// <param name="workbook">The workbook.</param>
    /// <param name="sheetName">The sheet to read.</param>
    /// <returns>A dictionary mapping each populated cell's (row, column) to its value.</returns>
    private static Dictionary<(int Row, int Column), ExcelCell> ReadCellGrid(ExcelBinaryWorkbook workbook, string sheetName)
    {
        Dictionary<(int Row, int Column), ExcelCell> grid = new();
        using ExcelWorksheetReader reader = workbook.OpenWorksheet(sheetName);
        while (reader.TryReadCell(out ExcelCell cell))
            grid[(cell.RowIndex, cell.ColumnIndex)] = cell;

        return grid;
    }

    /// <summary>
    /// Describes a cell for comparing two readings of a workbook.
    /// </summary>
    /// <param name="cell">The cell.</param>
    /// <returns>The cell's row, column, kind and value, comma-separated and culture-invariant.</returns>
    private static string DescribeCell(ExcelCell cell) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{cell.RowIndex},{cell.ColumnIndex},{cell.Kind},{cell.StringValue}{cell.NumberValue}{cell.BooleanValue}{cell.ErrorValue}");
}
