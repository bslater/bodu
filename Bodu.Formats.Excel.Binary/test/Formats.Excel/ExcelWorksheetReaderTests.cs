// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel.Contracts;

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the behavior of <see cref="ExcelWorksheetReader" /> against synthetic BIFF8 cell records, exercising the
/// streaming decode surface of the shared cell-decode contract together with the reader's own enumeration, grouping,
/// disposal, and malformed-record paths.
/// </summary>
[TestClass]
public partial class ExcelWorksheetReaderTests
    : ExcelCellDecodeContractTests
{
    /// <inheritdoc />
    protected override IReadOnlyDictionary<(int Row, int Column), ExcelCell> DecodeCells(ExcelCellDecodeKat kat) =>
        ReadGrid(kat.Records);

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

    /// <summary>
    /// Builds a worksheet substream that opens with a BIFF4 beginning-of-file record (version <c>0x0400</c>) and holds
    /// the number 9 in A1.
    /// </summary>
    /// <returns>The substream bytes.</returns>
    private static byte[] Biff4Substream() =>
        Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Record(0x0809, [0x00, 0x04, 0x10, 0x00, 0xBB, 0x0D, 0xCC, 0x07]),
            Biff8TestWorkbook.Number(0, 0, 9),
            Biff8TestWorkbook.Eof());
}
