// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Represents a single populated row of a worksheet: its zero-based index and the populated cells it contains, in
/// ascending column order.
/// </summary>
/// <remarks>
/// A row exposes only its populated cells; absent cells are omitted, so the row is a sparse sequence. Rows with no
/// populated cell are not materialized.
/// </remarks>
public sealed class ExcelRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelRow" /> class.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="cells">The populated cells of the row, in ascending column order.</param>
    internal ExcelRow(int rowIndex, IReadOnlyList<ExcelCell> cells)
    {
        RowIndex = rowIndex;
        Cells = cells;
    }

    /// <summary>
    /// Gets the zero-based row index.
    /// </summary>
    /// <returns>The row index within the worksheet.</returns>
    public int RowIndex { get; }

    /// <summary>
    /// Gets the populated cells of the row, in ascending column order.
    /// </summary>
    /// <returns>A read-only list of the row's populated cells.</returns>
    public IReadOnlyList<ExcelCell> Cells { get; }
}
