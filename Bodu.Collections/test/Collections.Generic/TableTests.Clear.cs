// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that <c>Clear</c> removes every cell and row, resetting the count and emptiness state.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPopulated_ShouldRemoveAllCellsAndRows()
    {
        Table<string, string, int> table = CreatePopulated();

        table.Clear();

        Assert.AreEqual(0, table.Count);
        Assert.IsTrue(table.IsEmpty);
        Assert.AreEqual(0, table.RowKeys.Count);
        Assert.IsFalse(table.ContainsRow("r1"));
    }

    /// <summary>
    /// Verifies that the table is reusable after <c>Clear</c>: cells can be added again and the count restarts.
    /// </summary>
    [TestMethod]
    public void Clear_WhenFollowedByAdd_ShouldAcceptNewCells()
    {
        Table<string, string, int> table = CreatePopulated();

        table.Clear();
        table.Add("r1", "c1", 10);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table["r1", "c1"]);
    }
}
