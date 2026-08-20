// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableRowTests.TryGetCell.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstTableRow.TryGetCell" />.
/// </summary>
public partial class PstTableRowTests
{
    /// <summary>
    /// Verifies that fixed-width cells read inline from the row across the 4-, 2-, and 1-byte regions.
    /// </summary>
    [TestMethod]
    public void TryGetCell_WhenCellIsFixedWidth_ShouldReadInline()
    {
        (PstFile file, PstTableContext context) = PstTableContextTests.OpenContextForRowTests();
        using (file)
        {
            PstTableRow row = context.EnumerateRows().First();

            Assert.IsTrue(row.TryGetCell(0x1111, out PstPropertyValue int32Value));
            Assert.AreEqual(7, int32Value.GetInt32());

            Assert.IsTrue(row.TryGetCell(0x3333, out PstPropertyValue int16Value));
            Assert.AreEqual((short)0x0102, int16Value.GetInt16());

            Assert.IsTrue(row.TryGetCell(0x4444, out PstPropertyValue booleanValue));
            Assert.IsTrue(booleanValue.GetBoolean());
        }
    }

    /// <summary>
    /// Verifies that a variable-size cell resolves its value reference into the heap.
    /// </summary>
    [TestMethod]
    public void TryGetCell_WhenCellIsVariableSize_ShouldResolveReference()
    {
        (PstFile file, PstTableContext context) = PstTableContextTests.OpenContextForRowTests();
        using (file)
        {
            PstTableRow row = context.EnumerateRows().First();

            Assert.IsTrue(row.TryGetCell(0x2222, out PstPropertyValue value));
            Assert.AreEqual("alpha", value.GetString());
        }
    }

    /// <summary>
    /// Verifies that a cell whose existence bit is clear reports absence even though the column is declared.
    /// </summary>
    [TestMethod]
    public void TryGetCell_WhenExistenceBitIsClear_ShouldReturnFalse()
    {
        (PstFile file, PstTableContext context) = PstTableContextTests.OpenContextForRowTests();
        using (file)
        {
            PstTableRow row = context.EnumerateRows().Last();

            Assert.IsFalse(row.TryGetCell(0x2222, out _));
        }
    }

    /// <summary>
    /// Verifies that an undeclared column reports absence.
    /// </summary>
    [TestMethod]
    public void TryGetCell_WhenColumnIsUndeclared_ShouldReturnFalse()
    {
        (PstFile file, PstTableContext context) = PstTableContextTests.OpenContextForRowTests();
        using (file)
        {
            PstTableRow row = context.EnumerateRows().First();

            Assert.IsFalse(row.TryGetCell(0x7FFF, out _));
        }
    }
}
