// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that <c>Count</c> tracks the total number of cells across adds, upserts, and removals.
    /// </summary>
    [TestMethod]
    public void Count_WhenMutated_ShouldTrackTotalCells()
    {
        var table = new Table<string, string, int>();
        Assert.AreEqual(0, table.Count);

        table.Add("r1", "c1", 1);
        table["r1", "c2"] = 2;
        Assert.AreEqual(2, table.Count);

        table["r1", "c2"] = 22;
        Assert.AreEqual(2, table.Count);

        table.Add("r2", "c1", 3);
        Assert.AreEqual(3, table.Count);

        table.Remove("r1", "c1");
        Assert.AreEqual(2, table.Count);

        table.RemoveRow("r1");
        Assert.AreEqual(1, table.Count);

        table.RemoveColumn("c1");
        Assert.AreEqual(0, table.Count);
    }

    /// <summary>
    /// Verifies that <c>IsEmpty</c> transitions with the first added and last removed cell.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenMutated_ShouldReflectCellPresence()
    {
        var table = new Table<string, string, int>();
        Assert.IsTrue(table.IsEmpty);

        table.Add("r1", "c1", 1);
        Assert.IsFalse(table.IsEmpty);

        table.Remove("r1", "c1");
        Assert.IsTrue(table.IsEmpty);
    }
}
