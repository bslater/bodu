// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Provides a materialized, randomly addressable view over the populated cells of a single worksheet.
/// </summary>
/// <remarks>
/// Unlike the streaming <see cref="Biff8WorkbookReader.ReadSheetCells(int)" /> surface, a worksheet buffers every
/// populated cell once, exposing them by position through <see cref="TryGetCell(int, int, out ExcelCell)" /> and
/// grouped into <see cref="Rows" />. Absent cells are omitted, so both surfaces are sparse.
/// </remarks>
public sealed class ExcelWorksheet
{
    /// <summary>The populated cells keyed by their (row, column) position.</summary>
    private readonly Dictionary<(int Row, int Column), ExcelCell> _cellsByPosition;

    /// <summary>The populated cells in row-major order.</summary>
    private readonly ExcelCell[] _cells;

    /// <summary>The populated rows in ascending row order.</summary>
    private readonly ExcelRow[] _rows;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelWorksheet" /> class.
    /// </summary>
    /// <param name="sheet">The worksheet descriptor.</param>
    /// <param name="cells">The populated cells, in any order.</param>
    internal ExcelWorksheet(Biff8SheetInfo sheet, IEnumerable<ExcelCell> cells)
    {
        Name = sheet.Name;
        Index = sheet.Index;
        Dimensions = sheet.Dimensions;

        _cells = [.. cells];
        Array.Sort(_cells, static (a, b) => a.RowIndex != b.RowIndex
            ? a.RowIndex.CompareTo(b.RowIndex)
            : a.ColumnIndex.CompareTo(b.ColumnIndex));

        _cellsByPosition = new Dictionary<(int, int), ExcelCell>(_cells.Length);
        foreach (ExcelCell cell in _cells)
            _cellsByPosition[(cell.RowIndex, cell.ColumnIndex)] = cell;

        _rows = GroupRows(_cells);
    }

    /// <summary>
    /// Gets the worksheet name.
    /// </summary>
    /// <returns>The sheet name as declared in the workbook globals.</returns>
    public string Name { get; }

    /// <summary>
    /// Gets the zero-based position of the worksheet within the workbook.
    /// </summary>
    /// <returns>The sheet index in workbook order.</returns>
    public int Index { get; }

    /// <summary>
    /// Gets the used range declared by the worksheet's <c>DIMENSIONS</c> record.
    /// </summary>
    /// <returns>The declared used range.</returns>
    public Biff8SheetDimensions Dimensions { get; }

    /// <summary>
    /// Gets the populated cells of the worksheet, in row-major order.
    /// </summary>
    /// <returns>A read-only list of every populated cell, ordered by row then column.</returns>
    public IReadOnlyList<ExcelCell> Cells => _cells;

    /// <summary>
    /// Gets the populated rows of the worksheet, in ascending row order.
    /// </summary>
    /// <returns>A read-only list of the worksheet's populated rows.</returns>
    public IReadOnlyList<ExcelRow> Rows => _rows;

    /// <summary>
    /// Attempts to get the cell at the specified position.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="cell">When this method returns, the cell at the position when one is populated.</param>
    /// <returns><see langword="true" /> when a populated cell exists at the position.</returns>
    public bool TryGetCell(int rowIndex, int columnIndex, out ExcelCell cell) =>
        _cellsByPosition.TryGetValue((rowIndex, columnIndex), out cell);

    /// <summary>
    /// Groups an ordered set of cells into rows.
    /// </summary>
    /// <param name="cells">The cells, ordered by row then column.</param>
    /// <returns>The populated rows, in ascending row order.</returns>
    private static ExcelRow[] GroupRows(ExcelCell[] cells)
    {
        List<ExcelRow> rows = new();
        int start = 0;
        while (start < cells.Length)
        {
            int row = cells[start].RowIndex;
            int end = start;
            while (end < cells.Length && cells[end].RowIndex == row)
                end++;

            rows.Add(new ExcelRow(row, cells[start..end]));
            start = end;
        }

        return [.. rows];
    }
}
