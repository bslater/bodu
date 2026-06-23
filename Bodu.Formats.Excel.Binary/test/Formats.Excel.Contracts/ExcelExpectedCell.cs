// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelExpectedCell.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Contracts;

/// <summary>
/// Describes a single expected cell within an <see cref="ExcelCellDecodeKat" />: its position, classification, and the
/// boxed value projected from the matching <see cref="ExcelCell" /> accessor.
/// </summary>
/// <param name="Row">The expected zero-based row index.</param>
/// <param name="Column">The expected zero-based column index.</param>
/// <param name="Kind">The expected cell classification.</param>
/// <param name="Value">
/// The expected value, boxed by kind: a <see cref="double" /> for <see cref="ExcelCellKind.Number" />, a
/// <see cref="bool" /> for <see cref="ExcelCellKind.Boolean" />, a <see cref="string" /> for
/// <see cref="ExcelCellKind.String" />, and an <see cref="ExcelErrorCode" /> for <see cref="ExcelCellKind.Error" />.
/// </param>
public sealed record ExcelExpectedCell(int Row, int Column, ExcelCellKind Kind, object? Value)
{
    /// <summary>
    /// Creates an expectation for a numeric cell.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="value">The expected numeric value.</param>
    /// <returns>An expectation of kind <see cref="ExcelCellKind.Number" />.</returns>
    public static ExcelExpectedCell Number(int row, int column, double value) =>
        new(row, column, ExcelCellKind.Number, value);

    /// <summary>
    /// Creates an expectation for a boolean cell.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="value">The expected boolean value.</param>
    /// <returns>An expectation of kind <see cref="ExcelCellKind.Boolean" />.</returns>
    public static ExcelExpectedCell Boolean(int row, int column, bool value) =>
        new(row, column, ExcelCellKind.Boolean, value);

    /// <summary>
    /// Creates an expectation for a text cell.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="value">The expected text value.</param>
    /// <returns>An expectation of kind <see cref="ExcelCellKind.String" />.</returns>
    public static ExcelExpectedCell Text(int row, int column, string value) =>
        new(row, column, ExcelCellKind.String, value);

    /// <summary>
    /// Creates an expectation for an error cell.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="errorCode">The expected spreadsheet error code.</param>
    /// <returns>An expectation of kind <see cref="ExcelCellKind.Error" />.</returns>
    public static ExcelExpectedCell Error(int row, int column, ExcelErrorCode errorCode) =>
        new(row, column, ExcelCellKind.Error, errorCode);
}
