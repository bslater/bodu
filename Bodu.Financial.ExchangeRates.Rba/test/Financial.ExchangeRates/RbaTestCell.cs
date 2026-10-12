// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaTestCell.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes one cell of a synthetic F11.1 workbook: what it holds and, for a rate, both the decimal the table
/// publishes and the double the workbook stores for it.
/// </summary>
/// <remarks>
/// A workbook stores every number as an IEEE 754 double. <see cref="Value" /> is the decimal the parser is expected to
/// recover from the cell, and <see cref="StoredValue" /> the double <see cref="RbaTestWorkbook" /> writes, which is
/// the double nearest <see cref="Value" /> unless the cell was built to sit a unit in the last place away from it.
/// </remarks>
internal readonly record struct RbaTestCell
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RbaTestCell" /> struct.
    /// </summary>
    /// <param name="kind">What the cell holds.</param>
    /// <param name="value">The published decimal of a number cell.</param>
    /// <param name="storedValue">The double the workbook stores for a number cell.</param>
    /// <param name="text">The text of a text cell.</param>
    private RbaTestCell(RbaTestCellKind kind, decimal value, double storedValue, string? text)
    {
        Kind = kind;
        Value = value;
        StoredValue = storedValue;
        Text = text;
    }

    /// <summary>
    /// Gets a cell that has no record.
    /// </summary>
    /// <value>The default cell, whose <see cref="Kind" /> is <see cref="RbaTestCellKind.Absent" />.</value>
    public static RbaTestCell Absent => default;

    /// <summary>
    /// Gets a formatted blank cell.
    /// </summary>
    /// <value>A cell whose <see cref="Kind" /> is <see cref="RbaTestCellKind.Blank" />.</value>
    public static RbaTestCell Blank => new(RbaTestCellKind.Blank, 0m, 0d, null);

    /// <summary>
    /// Gets what the cell holds.
    /// </summary>
    /// <value>The cell kind.</value>
    public RbaTestCellKind Kind { get; }

    /// <summary>
    /// Gets the published decimal of a number cell.
    /// </summary>
    /// <value>
    /// The decimal the parser should recover from the cell; zero for any other kind, and for a date serial.
    /// </value>
    public decimal Value { get; }

    /// <summary>
    /// Gets the double the workbook stores for a number cell.
    /// </summary>
    /// <value>The stored double; zero for any other kind.</value>
    public double StoredValue { get; }

    /// <summary>
    /// Gets the text of a text cell.
    /// </summary>
    /// <value>The cell text, or <see langword="null" /> for any other kind.</value>
    public string? Text { get; }

    /// <summary>
    /// Creates a text cell, such as a header label or the placeholder the RBA writes on a day without a rate.
    /// </summary>
    /// <param name="text">The cell text.</param>
    /// <returns>A cell whose <see cref="Kind" /> is <see cref="RbaTestCellKind.Text" />.</returns>
    public static RbaTestCell FromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new(RbaTestCellKind.Text, 0m, 0d, text);
    }

    /// <summary>
    /// Creates a number cell that stores the double nearest a published decimal.
    /// </summary>
    /// <param name="value">The published decimal.</param>
    /// <returns>A cell whose <see cref="StoredValue" /> is <paramref name="value" /> converted to a double.</returns>
    public static RbaTestCell FromNumber(decimal value) =>
        new(RbaTestCellKind.Number, value, (double)value, null);

    /// <summary>
    /// Creates a number cell that stores a chosen double for a published decimal, such as a double one unit in the
    /// last place away from it.
    /// </summary>
    /// <param name="value">The published decimal the parser should recover.</param>
    /// <param name="storedValue">The double the workbook stores.</param>
    /// <returns>A cell whose <see cref="Kind" /> is <see cref="RbaTestCellKind.Number" />.</returns>
    public static RbaTestCell FromNumber(decimal value, double storedValue) =>
        new(RbaTestCellKind.Number, value, storedValue, null);

    /// <summary>
    /// Creates a number cell holding an Excel date serial, which may be one no date represents.
    /// </summary>
    /// <param name="serial">The serial the workbook stores.</param>
    /// <returns>A number cell whose <see cref="StoredValue" /> is <paramref name="serial" />.</returns>
    public static RbaTestCell FromSerial(double serial) =>
        new(RbaTestCellKind.Number, 0m, serial, null);
}
