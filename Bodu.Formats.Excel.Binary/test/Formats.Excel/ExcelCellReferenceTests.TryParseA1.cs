// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCellReferenceTests.TryParseA1.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelCellReferenceTests
{
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
}
