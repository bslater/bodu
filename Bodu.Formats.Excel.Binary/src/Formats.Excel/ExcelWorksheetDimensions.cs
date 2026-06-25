// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetDimensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Describes the used range of a worksheet as declared by its <c>DIMENSIONS</c> record: the half-open span of rows and
/// columns that bounds the sheet's populated cells.
/// </summary>
/// <remarks>
/// <para>
/// The used range covers rows <c>[FirstRowIndex, FirstRowIndex + RowCount)</c> and columns
/// <c>[FirstColumnIndex, FirstColumnIndex + ColumnCount)</c>. Excel records the extent it has allocated, which may be
/// larger than the region that actually holds values, so this range is an upper bound rather than a tight fit.
/// </para>
/// <para>
/// A worksheet with no <c>DIMENSIONS</c> record yields the default value, whose counts are zero.
/// </para>
/// </remarks>
public readonly record struct ExcelWorksheetDimensions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelWorksheetDimensions" /> struct.
    /// </summary>
    /// <param name="firstRowIndex">The zero-based index of the first used row.</param>
    /// <param name="rowCount">The number of rows in the used range.</param>
    /// <param name="firstColumnIndex">The zero-based index of the first used column.</param>
    /// <param name="columnCount">The number of columns in the used range.</param>
    internal ExcelWorksheetDimensions(int firstRowIndex, int rowCount, int firstColumnIndex, int columnCount)
    {
        FirstRowIndex = firstRowIndex;
        RowCount = rowCount;
        FirstColumnIndex = firstColumnIndex;
        ColumnCount = columnCount;
    }

    /// <summary>
    /// Gets the zero-based index of the first used row.
    /// </summary>
    /// <value>The first row index of the used range.</value>
    public int FirstRowIndex { get; }

    /// <summary>
    /// Gets the number of rows in the used range.
    /// </summary>
    /// <value>The count of rows spanned by the used range.</value>
    public int RowCount { get; }

    /// <summary>
    /// Gets the zero-based index of the first used column.
    /// </summary>
    /// <value>The first column index of the used range.</value>
    public int FirstColumnIndex { get; }

    /// <summary>
    /// Gets the number of columns in the used range.
    /// </summary>
    /// <value>The count of columns spanned by the used range.</value>
    public int ColumnCount { get; }
}
