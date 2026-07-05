// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that the indexer getter returns the value of an existing cell.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenCellExists_ShouldReturnValue()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.AreEqual(2, table["r1", "c2"]);
    }

    /// <summary>
    /// Verifies that reading a missing cell throws <see cref="KeyNotFoundException" />, both for an absent row and
    /// for an absent column within an existing row.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenCellMissing_ShouldThrowKeyNotFoundException()
    {
        Table<string, string, int> table = CreatePopulated();

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = table["missing", "c1"];
        });

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = table["r2", "c2"];
        });
    }

    /// <summary>
    /// Verifies that the indexer setter adds a new cell, creating the row when absent.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSettingNewCell_ShouldAddCellAndCreateRow()
    {
        var table = new Table<string, string, int>();

        table["r1", "c1"] = 42;

        Assert.AreEqual(42, table["r1", "c1"]);
        Assert.AreEqual(1, table.Count);
        Assert.IsTrue(table.ContainsRow("r1"));
    }

    /// <summary>
    /// Verifies that the indexer setter overwrites an existing cell's value without changing the count.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSettingExistingCell_ShouldOverwriteValueWithoutCountChange()
    {
        Table<string, string, int> table = CreatePopulated();

        table["r1", "c1"] = 99;

        Assert.AreEqual(99, table["r1", "c1"]);
        Assert.AreEqual(3, table.Count);
    }

    /// <summary>
    /// Verifies that the indexer throws <see cref="ArgumentNullException" /> for a <see langword="null" /> row or
    /// column key on both the getter and the setter.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var table = new Table<string, string, int>();

        var ex1 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table[null!, "c1"];
        });
        Assert.AreEqual("row", ex1.ParamName);

        var ex2 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table["r1", null!];
        });
        Assert.AreEqual("column", ex2.ParamName);

        var ex3 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            table[null!, "c1"] = 1;
        });
        Assert.AreEqual("row", ex3.ParamName);

        var ex4 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            table["r1", null!] = 1;
        });
        Assert.AreEqual("column", ex4.ParamName);
    }
}
