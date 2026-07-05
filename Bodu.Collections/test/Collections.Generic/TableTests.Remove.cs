// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that removing an existing cell returns <see langword="true" /> and updates the count.
    /// </summary>
    [TestMethod]
    public void Remove_WhenCellExists_ShouldReturnTrueAndDecrementCount()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsTrue(table.Remove("r1", "c1"));
        Assert.AreEqual(2, table.Count);
        Assert.IsFalse(table.Contains("r1", "c1"));
    }

    /// <summary>
    /// Verifies that removing a missing cell returns <see langword="false" /> and leaves the table unchanged.
    /// </summary>
    [TestMethod]
    public void Remove_WhenCellMissing_ShouldReturnFalse()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsFalse(table.Remove("missing", "c1"));
        Assert.IsFalse(table.Remove("r2", "c2"));
        Assert.AreEqual(3, table.Count);
    }

    /// <summary>
    /// Verifies that removing a row's last cell prunes the row itself, so <c>ContainsRow</c> and <c>RowKeys</c> no
    /// longer report it.
    /// </summary>
    [TestMethod]
    public void Remove_WhenLastCellOfRow_ShouldPruneEmptyRow()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsTrue(table.Remove("r2", "c1"));

        Assert.IsFalse(table.ContainsRow("r2"));
        Assert.IsFalse(table.RowKeys.Contains("r2"));
        CollectionAssert.AreEquivalent(new[] { "r1" }, table.RowKeys.ToArray());
    }

    /// <summary>
    /// Verifies that removing one of several cells in a row keeps the row alive.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRowStillHasCells_ShouldKeepRow()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsTrue(table.Remove("r1", "c1"));

        Assert.IsTrue(table.ContainsRow("r1"));
        Assert.AreEqual(2, table["r1", "c2"]);
    }

    /// <summary>
    /// Verifies that removing with a <see langword="null" /> row or column key throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var table = new Table<string, string, int>();

        var ex1 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.Remove(null!, "c1");
        });
        Assert.AreEqual("row", ex1.ParamName);

        var ex2 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.Remove("r1", null!);
        });
        Assert.AreEqual("column", ex2.ParamName);
    }
}
