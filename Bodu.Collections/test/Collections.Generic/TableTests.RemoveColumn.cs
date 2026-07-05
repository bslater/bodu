// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.RemoveColumn.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that <c>RemoveColumn</c> removes the column's cell from every row and updates the count.
    /// </summary>
    [TestMethod]
    public void RemoveColumn_WhenColumnExistsInMultipleRows_ShouldRemoveAllItsCells()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsTrue(table.RemoveColumn("c1"));

        Assert.AreEqual(1, table.Count);
        Assert.IsFalse(table.ContainsColumn("c1"));
        Assert.IsTrue(table.Contains("r1", "c2"));
    }

    /// <summary>
    /// Verifies that <c>RemoveColumn</c> prunes rows whose last cell was in the removed column while keeping rows
    /// that still hold other cells.
    /// </summary>
    [TestMethod]
    public void RemoveColumn_WhenRowEmptied_ShouldPruneEmptiedRows()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsTrue(table.RemoveColumn("c1"));

        Assert.IsFalse(table.ContainsRow("r2"));
        Assert.IsTrue(table.ContainsRow("r1"));
        CollectionAssert.AreEquivalent(new[] { "r1" }, table.RowKeys.ToArray());
    }

    /// <summary>
    /// Verifies that <c>RemoveColumn</c> returns <see langword="false" /> for an absent column and leaves the table
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void RemoveColumn_WhenColumnMissing_ShouldReturnFalse()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsFalse(table.RemoveColumn("missing"));
        Assert.AreEqual(3, table.Count);
    }

    /// <summary>
    /// Verifies that <c>RemoveColumn</c> with a <see langword="null" /> column key throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void RemoveColumn_WhenColumnIsNull_ShouldThrowArgumentNullException()
    {
        var table = new Table<string, string, int>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.RemoveColumn(null!);
        });
        Assert.AreEqual("column", ex.ParamName);
    }
}
