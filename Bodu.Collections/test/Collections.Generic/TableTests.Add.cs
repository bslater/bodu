// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that adding a cell stores its value and updates the count.
    /// </summary>
    [TestMethod]
    public void Add_WhenCellIsNew_ShouldStoreValue()
    {
        var table = new Table<string, string, int>();

        table.Add("r1", "c1", 7);

        Assert.AreEqual(7, table["r1", "c1"]);
        Assert.AreEqual(1, table.Count);
    }

    /// <summary>
    /// Verifies that adding a duplicate cell throws <see cref="ArgumentException" /> and leaves the stored value
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenCellIsDuplicate_ShouldThrowArgumentException()
    {
        Table<string, string, int> table = CreatePopulated();

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            table.Add("r1", "c1", 99);
        });

        Assert.AreEqual(1, table["r1", "c1"]);
        Assert.AreEqual(3, table.Count);
    }

    /// <summary>
    /// Verifies that the same column key can be added under different rows, and the same row key can hold multiple
    /// columns.
    /// </summary>
    [TestMethod]
    public void Add_WhenSameColumnDifferentRow_ShouldAddIndependentCells()
    {
        var table = new Table<string, string, int>();

        table.Add("r1", "c1", 1);
        table.Add("r2", "c1", 2);
        table.Add("r1", "c2", 3);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(1, table["r1", "c1"]);
        Assert.AreEqual(2, table["r2", "c1"]);
        Assert.AreEqual(3, table["r1", "c2"]);
    }

    /// <summary>
    /// Verifies that adding with a <see langword="null" /> row or column key throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var table = new Table<string, string, int>();

        var ex1 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            table.Add(null!, "c1", 1);
        });
        Assert.AreEqual("row", ex1.ParamName);

        var ex2 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            table.Add("r1", null!, 1);
        });
        Assert.AreEqual("column", ex2.ParamName);
    }
}
