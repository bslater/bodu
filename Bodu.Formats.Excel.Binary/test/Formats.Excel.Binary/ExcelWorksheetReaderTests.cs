// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Verifies the behavior of <see cref="ExcelWorksheetReader" /> against synthetic BIFF8 cell records, exercising the
/// value-record and malformed-record paths the real-world fixture does not contain.
/// </summary>
[TestClass]
public partial class ExcelWorksheetReaderTests
{
    /// <summary>
    /// Reads every cell from a synthetic worksheet substream into a position-keyed dictionary.
    /// </summary>
    /// <param name="body">The records that constitute the worksheet substream body.</param>
    /// <returns>A dictionary mapping each populated cell's (row, column) to its value.</returns>
    private static Dictionary<(int Row, int Column), ExcelCell> ReadGrid(params byte[][] body)
    {
        Dictionary<(int Row, int Column), ExcelCell> grid = new();
        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(body);
        while (reader.TryReadCell(out ExcelCell cell))
            grid[(cell.RowIndex, cell.ColumnIndex)] = cell;

        return grid;
    }
}
