// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorkbookReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Test;

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Verifies the behavior of <see cref="Biff8WorkbookReader" /> against a real-world BIFF8 workbook.
/// </summary>
[TestClass]
public partial class Biff8WorkbookReaderTests
{
    /// <summary>
    /// Opens the embedded sample workbook.
    /// </summary>
    /// <returns>An open <see cref="Biff8WorkbookReader" /> over the sample fixture.</returns>
    private static Biff8WorkbookReader OpenSample() =>
        Biff8WorkbookReader.Open(ExcelBinaryFixtures.OpenStream(ExcelBinaryFixtures.SampleBiff8));

    /// <summary>
    /// Reads every populated cell of the named sheet into a position-keyed dictionary.
    /// </summary>
    /// <param name="reader">The workbook reader.</param>
    /// <param name="sheetName">The sheet to read.</param>
    /// <returns>A dictionary mapping each populated cell's (row, column) to its value.</returns>
    private static Dictionary<(int Row, int Column), ExcelCell> ReadCellGrid(Biff8WorkbookReader reader, string sheetName)
    {
        Dictionary<(int Row, int Column), ExcelCell> grid = new();
        foreach (ExcelCell cell in reader.ReadSheetCells(sheetName))
            grid[(cell.RowIndex, cell.ColumnIndex)] = cell;

        return grid;
    }
}
