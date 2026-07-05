// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.TryAdd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that <c>TryAdd</c> adds a new cell and returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenCellIsNew_ShouldReturnTrueAndStoreValue()
    {
        var table = new Table<string, string, int>();

        Assert.IsTrue(table.TryAdd("r1", "c1", 5));
        Assert.AreEqual(5, table["r1", "c1"]);
        Assert.AreEqual(1, table.Count);
    }

    /// <summary>
    /// Verifies that <c>TryAdd</c> returns <see langword="false" /> for a duplicate cell and leaves the table
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenCellIsDuplicate_ShouldReturnFalseAndLeaveTableUnchanged()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsFalse(table.TryAdd("r1", "c1", 99));
        Assert.AreEqual(1, table["r1", "c1"]);
        Assert.AreEqual(3, table.Count);
    }

    /// <summary>
    /// Verifies that <c>TryAdd</c> with a <see langword="null" /> row or column key throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var table = new Table<string, string, int>();

        var ex1 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.TryAdd(null!, "c1", 1);
        });
        Assert.AreEqual("row", ex1.ParamName);

        var ex2 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.TryAdd("r1", null!, 1);
        });
        Assert.AreEqual("column", ex2.ParamName);
    }
}
