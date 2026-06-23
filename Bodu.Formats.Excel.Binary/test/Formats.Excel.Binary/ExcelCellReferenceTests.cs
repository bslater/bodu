// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCellReferenceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Verifies the behavior of <see cref="ExcelCellReference" />, the A1 reference converter.
/// </summary>
[TestClass]
public class ExcelCellReferenceTests
{
    /// <summary>
    /// Verifies that a zero-based column index converts to its A1 column name.
    /// </summary>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="expected">The expected column name.</param>
    [TestMethod]
    [DataRow(0, "A")]
    [DataRow(25, "Z")]
    [DataRow(26, "AA")]
    [DataRow(27, "AB")]
    [DataRow(51, "AZ")]
    [DataRow(701, "ZZ")]
    [DataRow(702, "AAA")]
    public void ColumnName_WhenIndex_ShouldReturnExpectedName(int columnIndex, string expected)
    {
        Assert.AreEqual(expected, ExcelCellReference.ColumnName(columnIndex));
    }

    /// <summary>
    /// Verifies that a negative column index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ColumnName_WhenIndexNegative_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = ExcelCellReference.ColumnName(-1);
        });
    }

    /// <summary>
    /// Verifies that zero-based coordinates convert to their A1 reference.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="expected">The expected A1 reference.</param>
    [TestMethod]
    [DataRow(0, 0, "A1")]
    [DataRow(9, 27, "AB10")]
    [DataRow(99, 0, "A100")]
    public void ToA1_WhenCoordinates_ShouldReturnExpectedReference(int rowIndex, int columnIndex, string expected)
    {
        Assert.AreEqual(expected, ExcelCellReference.ToA1(rowIndex, columnIndex));
    }

    /// <summary>
    /// Verifies that a valid A1 reference parses to its zero-based coordinates, accepting lower-case column letters.
    /// </summary>
    /// <param name="reference">The A1 reference.</param>
    /// <param name="expectedRow">The expected zero-based row index.</param>
    /// <param name="expectedColumn">The expected zero-based column index.</param>
    [TestMethod]
    [DataRow("A1", 0, 0)]
    [DataRow("AB10", 9, 27)]
    [DataRow("ab10", 9, 27)]
    [DataRow("Z1", 0, 25)]
    public void TryParseA1_WhenValid_ShouldReturnCoordinates(string reference, int expectedRow, int expectedColumn)
    {
        bool parsed = ExcelCellReference.TryParseA1(reference, out int rowIndex, out int columnIndex);

        Assert.IsTrue(parsed);
        Assert.AreEqual(expectedRow, rowIndex);
        Assert.AreEqual(expectedColumn, columnIndex);
    }

    /// <summary>
    /// Verifies that a malformed reference is rejected.
    /// </summary>
    /// <param name="reference">The candidate reference.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow("1")]
    [DataRow("A")]
    [DataRow("A0")]
    [DataRow("1A")]
    [DataRow("A1B")]
    [DataRow("A 1")]
    public void TryParseA1_WhenInvalid_ShouldReturnFalse(string reference)
    {
        Assert.IsFalse(ExcelCellReference.TryParseA1(reference, out _, out _));
    }

    /// <summary>
    /// Verifies that the column name and A1 conversions round-trip through parsing.
    /// </summary>
    [TestMethod]
    public void ToA1_WhenRoundTrippedThroughParse_ShouldRecoverCoordinates()
    {
        string reference = ExcelCellReference.ToA1(123, 456);

        Assert.IsTrue(ExcelCellReference.TryParseA1(reference, out int rowIndex, out int columnIndex));
        Assert.AreEqual(123, rowIndex);
        Assert.AreEqual(456, columnIndex);
    }
}
