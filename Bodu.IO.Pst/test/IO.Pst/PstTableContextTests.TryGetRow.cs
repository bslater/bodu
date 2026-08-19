// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableContextTests.TryGetRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstTableContext.TryGetRow" />: keyed row lookup through the row index.
/// </summary>
public partial class PstTableContextTests
{
    /// <summary>
    /// Verifies that a present row identifier resolves to its matrix row.
    /// </summary>
    [TestMethod]
    public void TryGetRow_WhenRowIdIsPresent_ShouldReturnRow()
    {
        (PstFile file, PstTableContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsTrue(context.TryGetRow(SecondRowId, out PstTableRow? row));
            Assert.AreEqual(SecondRowId, row.RowId);

            Assert.IsTrue(row.TryGetCell(0x1111, out PstPropertyValue value));
            Assert.AreEqual(9, value.GetInt32());
        }
    }

    /// <summary>
    /// Verifies that an absent row identifier reports a miss rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryGetRow_WhenRowIdIsAbsent_ShouldReturnFalse()
    {
        (PstFile file, PstTableContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsFalse(context.TryGetRow(0x9999, out _));
        }
    }
}
