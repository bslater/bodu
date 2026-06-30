// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorkbookDecodeContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel.Contracts;

namespace Bodu.Formats.Excel;

/// <summary>
/// Runs the shared cell-decode contract through the materialized workbook path: the same records are wrapped in a
/// complete compound workbook and read back through <see cref="ExcelBinaryWorkbook.ReadWorksheet(int)" />.
/// </summary>
[TestClass]
public sealed class ExcelWorkbookDecodeContractTests
    : ExcelCellDecodeContractTests
{
    /// <inheritdoc />
    protected override IReadOnlyDictionary<(int Row, int Column), ExcelCell> DecodeCells(ExcelCellDecodeKat kat)
    {
        // A DIMENSIONS record is prepended for a well-formed sheet; the decoder skips it, so the surfaced cells match
        // the streaming reader's view of the same value records.
        byte[][] body = [Biff8TestWorkbook.Dimensions(0, 1000, 0, 256), .. kat.Records];

        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, body));
        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        ExcelWorksheet sheet = workbook.ReadWorksheet(0);
        Dictionary<(int Row, int Column), ExcelCell> grid = new();
        foreach (ExcelCell cell in sheet.Cells)
            grid[(cell.RowIndex, cell.ColumnIndex)] = cell;

        return grid;
    }
}
