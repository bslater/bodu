// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCellTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the behavior of <see cref="ExcelCell" />, the value-type cell record produced by the worksheet reader.
/// </summary>
[TestClass]
public class ExcelCellTests
{
    /// <summary>
    /// Verifies that a text cell carries its string value and exposes <see langword="null" /> for the other value
    /// projections.
    /// </summary>
    [TestMethod]
    public void Text_WhenCreated_ShouldExposeStringValueOnly()
    {
        ExcelCell cell = ExcelCell.Text(1, 2, "hello", formatIndex: 5);

        Assert.AreEqual(1, cell.RowIndex);
        Assert.AreEqual(2, cell.ColumnIndex);
        Assert.AreEqual(ExcelCellKind.String, cell.Kind);
        Assert.AreEqual("hello", cell.StringValue);
        Assert.AreEqual((ushort)5, cell.FormatIndex);
        Assert.IsNull(cell.NumberValue);
        Assert.IsNull(cell.BooleanValue);
        Assert.IsNull(cell.ErrorValue);
        Assert.IsFalse(cell.IsDateFormatted);
    }

    /// <summary>
    /// Verifies that a numeric cell carries its number value and the date-format flag, with the other projections
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Number_WhenCreatedDateFormatted_ShouldExposeNumberValueAndDateFlag()
    {
        ExcelCell cell = ExcelCell.Number(3, 4, 44929.0, formatIndex: 14, isDateFormatted: true);

        Assert.AreEqual(ExcelCellKind.Number, cell.Kind);
        Assert.AreEqual(44929.0, cell.NumberValue!.Value, 0.0);
        Assert.IsTrue(cell.IsDateFormatted);
        Assert.AreEqual((ushort)14, cell.FormatIndex);
        Assert.IsNull(cell.StringValue);
        Assert.IsNull(cell.BooleanValue);
        Assert.IsNull(cell.ErrorValue);
    }

    /// <summary>
    /// Verifies that a boolean cell carries its boolean value, with the other projections <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Boolean_WhenCreated_ShouldExposeBooleanValueOnly()
    {
        ExcelCell cell = ExcelCell.Boolean(0, 0, true);

        Assert.AreEqual(ExcelCellKind.Boolean, cell.Kind);
        Assert.IsTrue(cell.BooleanValue!.Value);
        Assert.IsNull(cell.StringValue);
        Assert.IsNull(cell.NumberValue);
        Assert.IsNull(cell.ErrorValue);
    }

    /// <summary>
    /// Verifies that an error cell carries its error code, with the other projections <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Error_WhenCreated_ShouldExposeErrorValueOnly()
    {
        ExcelCell cell = ExcelCell.Error(0, 0, ExcelErrorCode.DivideByZero);

        Assert.AreEqual(ExcelCellKind.Error, cell.Kind);
        Assert.AreEqual(ExcelErrorCode.DivideByZero, cell.ErrorValue!.Value);
        Assert.IsNull(cell.StringValue);
        Assert.IsNull(cell.NumberValue);
        Assert.IsNull(cell.BooleanValue);
    }

    /// <summary>
    /// Verifies that two cells created with the same coordinates, kind, and value compare equal by value.
    /// </summary>
    [TestMethod]
    public void Equality_WhenSameContent_ShouldBeEqual()
    {
        ExcelCell first = ExcelCell.Number(2, 3, 1.5);
        ExcelCell second = ExcelCell.Number(2, 3, 1.5);

        Assert.AreEqual(first, second);
        Assert.IsTrue(first == second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    /// Verifies that cells differing in value do not compare equal.
    /// </summary>
    [TestMethod]
    public void Equality_WhenDifferentValue_ShouldNotBeEqual()
    {
        ExcelCell first = ExcelCell.Number(2, 3, 1.5);
        ExcelCell second = ExcelCell.Number(2, 3, 2.5);

        Assert.AreNotEqual(first, second);
        Assert.IsTrue(first != second);
    }

    /// <summary>
    /// Verifies that a default <see cref="ExcelCell" /> classifies as <see cref="ExcelCellKind.Blank" /> with no value.
    /// </summary>
    [TestMethod]
    public void Default_WhenUninitialized_ShouldBeBlank()
    {
        ExcelCell cell = default;

        Assert.AreEqual(ExcelCellKind.Blank, cell.Kind);
        Assert.IsNull(cell.StringValue);
        Assert.IsNull(cell.NumberValue);
        Assert.IsNull(cell.BooleanValue);
        Assert.IsNull(cell.ErrorValue);
    }
}
