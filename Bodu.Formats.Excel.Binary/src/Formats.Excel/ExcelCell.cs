// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCell.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

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
    /// <param name="errorValue">The error code, when applicable.</param>
    /// <param name="formatIndex">The number-format index applied to the cell.</param>
    /// <param name="isDateFormatted">Whether the cell is a number formatted as a date or time.</param>
    private ExcelCell(int rowIndex, int columnIndex, ExcelCellKind kind, string? stringValue, double? numberValue, bool? booleanValue, ExcelErrorCode? errorValue, ushort formatIndex, bool isDateFormatted)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        Kind = kind;
        StringValue = stringValue;
        NumberValue = numberValue;
        BooleanValue = booleanValue;
        ErrorValue = errorValue;
        FormatIndex = formatIndex;
        IsDateFormatted = isDateFormatted;
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
    /// Gets the error code when <see cref="Kind" /> is <see cref="ExcelCellKind.Error" />; otherwise
    /// <see langword="null" />.
    /// </summary>
    /// <returns>The spreadsheet error code, or <see langword="null" />.</returns>
    public ExcelErrorCode? ErrorValue { get; }

    /// <summary>
    /// Gets the number-format index applied to the cell.
    /// </summary>
    /// <returns>
    /// The format index referenced by the cell's record; <c>0</c> (the General format) when no format is recorded.
    /// </returns>
    public ushort FormatIndex { get; }

    /// <summary>
    /// Gets a value indicating whether the cell is a number formatted as a date or time.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when <see cref="Kind" /> is <see cref="ExcelCellKind.Number" /> and the cell's format is
    /// a date or time format; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Use <see cref="ExcelSerialDate" /> with the workbook's date system to convert a date-formatted number to a
    /// calendar value.
    /// </remarks>
    public bool IsDateFormatted { get; }

    /// <summary>
    /// Creates a text cell.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="value">The text value.</param>
    /// <param name="formatIndex">The number-format index applied to the cell.</param>
    /// <returns>An <see cref="ExcelCell" /> of kind <see cref="ExcelCellKind.String" />.</returns>
    public static ExcelCell Text(int rowIndex, int columnIndex, string value, ushort formatIndex = 0) =>
        new(rowIndex, columnIndex, ExcelCellKind.String, value, null, null, null, formatIndex, false);

    /// <summary>
    /// Creates a numeric cell.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="value">The numeric value.</param>
    /// <param name="formatIndex">The number-format index applied to the cell.</param>
    /// <param name="isDateFormatted">Whether the cell's format renders the number as a date or time.</param>
    /// <returns>An <see cref="ExcelCell" /> of kind <see cref="ExcelCellKind.Number" />.</returns>
    public static ExcelCell Number(int rowIndex, int columnIndex, double value, ushort formatIndex = 0, bool isDateFormatted = false) =>
        new(rowIndex, columnIndex, ExcelCellKind.Number, null, value, null, null, formatIndex, isDateFormatted);

    /// <summary>
    /// Creates a boolean cell.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="value">The boolean value.</param>
    /// <param name="formatIndex">The number-format index applied to the cell.</param>
    /// <returns>An <see cref="ExcelCell" /> of kind <see cref="ExcelCellKind.Boolean" />.</returns>
    public static ExcelCell Boolean(int rowIndex, int columnIndex, bool value, ushort formatIndex = 0) =>
        new(rowIndex, columnIndex, ExcelCellKind.Boolean, null, null, value, null, formatIndex, false);

    /// <summary>
    /// Creates an error cell.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="errorCode">The spreadsheet error code.</param>
    /// <param name="formatIndex">The number-format index applied to the cell.</param>
    /// <returns>An <see cref="ExcelCell" /> of kind <see cref="ExcelCellKind.Error" />.</returns>
    public static ExcelCell Error(int rowIndex, int columnIndex, ExcelErrorCode errorCode, ushort formatIndex = 0) =>
        new(rowIndex, columnIndex, ExcelCellKind.Error, null, null, null, errorCode, formatIndex, false);
}
