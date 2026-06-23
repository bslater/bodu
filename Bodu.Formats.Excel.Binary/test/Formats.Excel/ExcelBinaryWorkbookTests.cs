// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the behavior of <see cref="ExcelBinaryWorkbook" /> against a real-world BIFF8 workbook and synthetic
/// fixtures.
/// </summary>
[TestClass]
public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Opens the embedded sample workbook.
    /// </summary>
    /// <returns>An open <see cref="ExcelBinaryWorkbook" /> over the sample fixture.</returns>
    private static ExcelBinaryWorkbook OpenSample() =>
        ExcelBinaryWorkbook.OpenRead(ExcelBinaryFixtures.OpenStream(ExcelBinaryFixtures.SampleBiff8));

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
}
