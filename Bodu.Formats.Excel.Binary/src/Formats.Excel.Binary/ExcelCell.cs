// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCell.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Represents a single populated cell read from a BIFF8 worksheet, carrying its zero-based position and raw value.
/// </summary>
/// <remarks>
/// The reader emits a cell only for a populated value; absent cells are simply not returned, so a worksheet is a sparse
/// sequence of cells. Numeric cells are returned as raw <see cref="double" /> values with no date or format
/// interpretation applied.
/// </remarks>
public readonly record struct ExcelCell
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelCell" /> class.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index of the cell.</param>
    /// <param name="columnIndex">The zero-based column index of the cell.</param>
    /// <param name="kind">The classification of the cell's value.</param>
    /// <param name="stringValue">The text value, when applicable.</param>
    /// <param name="numberValue">The numeric value, when applicable.</param>
    /// <param name="booleanValue">The boolean value, when applicable.</param>
    private ExcelCell(int rowIndex, int columnIndex, ExcelCellKind kind, string? stringValue, double? numberValue, bool? booleanValue)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        Kind = kind;
        StringValue = stringValue;
        NumberValue = numberValue;
        BooleanValue = booleanValue;
    }

    /// <summary>
    /// Gets the zero-based row index of the cell.
    /// </summary>
    /// <returns>The row index.</returns>
    public int RowIndex { get; }

    /// <summary>
    /// Gets the zero-based column index of the cell.
    /// </summary>
    /// <returns>The column index.</returns>
    public int ColumnIndex { get; }

    /// <summary>
    /// Gets the classification of the cell's value.
    /// </summary>
    /// <returns>The cell kind.</returns>
    public ExcelCellKind Kind { get; }

    /// <summary>
    /// Gets the text value when <see cref="Kind" /> is <see cref="ExcelCellKind.String" />; otherwise
    /// <see langword="null" />.
    /// </summary>
    /// <returns>The text value, or <see langword="null" />.</returns>
    public string? StringValue { get; }

    /// <summary>
    /// Gets the numeric value when <see cref="Kind" /> is <see cref="ExcelCellKind.Number" />; otherwise
    /// <see langword="null" />.
    /// </summary>
    /// <returns>The numeric value, or <see langword="null" />.</returns>
    public double? NumberValue { get; }

    /// <summary>
    /// Gets the boolean value when <see cref="Kind" /> is <see cref="ExcelCellKind.Boolean" />; otherwise
    /// <see langword="null" />.
    /// </summary>
    /// <returns>The boolean value, or <see langword="null" />.</returns>
    public bool? BooleanValue { get; }

    /// <summary>
    /// Creates a text cell.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="value">The text value.</param>
    /// <returns>An <see cref="ExcelCell" /> of kind <see cref="ExcelCellKind.String" />.</returns>
    public static ExcelCell Text(int rowIndex, int columnIndex, string value) =>
        new(rowIndex, columnIndex, ExcelCellKind.String, value, null, null);

    /// <summary>
    /// Creates a numeric cell.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="value">The numeric value.</param>
    /// <returns>An <see cref="ExcelCell" /> of kind <see cref="ExcelCellKind.Number" />.</returns>
    public static ExcelCell Number(int rowIndex, int columnIndex, double value) =>
        new(rowIndex, columnIndex, ExcelCellKind.Number, null, value, null);

    /// <summary>
    /// Creates a boolean cell.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="value">The boolean value.</param>
    /// <returns>An <see cref="ExcelCell" /> of kind <see cref="ExcelCellKind.Boolean" />.</returns>
    public static ExcelCell Boolean(int rowIndex, int columnIndex, bool value) =>
        new(rowIndex, columnIndex, ExcelCellKind.Boolean, null, null, value);

    /// <summary>
    /// Creates an error cell.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <returns>An <see cref="ExcelCell" /> of kind <see cref="ExcelCellKind.Error" />.</returns>
    public static ExcelCell Error(int rowIndex, int columnIndex) =>
        new(rowIndex, columnIndex, ExcelCellKind.Error, null, null, null);
}
