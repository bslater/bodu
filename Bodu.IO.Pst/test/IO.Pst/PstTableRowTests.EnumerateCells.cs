// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableRowTests.EnumerateCells.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstTableRow.EnumerateCells" /> and <see cref="PstTableRow.RowId" />.
/// </summary>
public partial class PstTableRowTests
{
    /// <summary>
    /// Verifies that enumeration yields only the cells whose existence bits are set, in column order.
    /// </summary>
    [TestMethod]
    public void EnumerateCells_WhenSomeCellsAreAbsent_ShouldYieldPresentCellsInColumnOrder()
    {
        (PstFile file, PstTableContext context) = PstTableContextTests.OpenContextForRowTests();
        using (file)
        {
            var rows = context.EnumerateRows().ToList();

            Assert.AreEqual(5, rows[0].EnumerateCells().Count());

            var secondRowIds = rows[1].EnumerateCells().Select(static c => c.PropertyId).ToArray();
            CollectionAssert.AreEqual(new ushort[] { 0x67F4, 0x1111, 0x3333, 0x4444 }, secondRowIds);
        }
    }

    /// <summary>
    /// Verifies that the row identifier reads from the row's leading dword.
    /// </summary>
    [TestMethod]
    public void RowId_WhenRowIsRead_ShouldReturnLeadingDword()
    {
        (PstFile file, PstTableContext context) = PstTableContextTests.OpenContextForRowTests();
        using (file)
        {
            CollectionAssert.AreEqual(
                new uint[] { 0x100, 0x200 },
                context.EnumerateRows().Select(static r => r.RowId).ToArray());
        }
    }
}
