// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that <c>Contains</c> reports an existing cell and rejects both an absent row and an absent column
    /// within an existing row.
    /// </summary>
    [TestMethod]
    public void Contains_WhenQueried_ShouldReflectCellPresence()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsTrue(table.Contains("r1", "c1"));
        Assert.IsFalse(table.Contains("missing", "c1"));
        Assert.IsFalse(table.Contains("r2", "c2"));
    }

    /// <summary>
    /// Verifies that <c>ContainsRow</c> reports only rows that currently hold at least one cell.
    /// </summary>
    [TestMethod]
    public void ContainsRow_WhenQueried_ShouldReflectRowPresence()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsTrue(table.ContainsRow("r1"));
        Assert.IsTrue(table.ContainsRow("r2"));
        Assert.IsFalse(table.ContainsRow("missing"));
    }

    /// <summary>
    /// Verifies that <c>ContainsColumn</c> locates a column present in any row and rejects an absent column.
    /// </summary>
    [TestMethod]
    public void ContainsColumn_WhenQueried_ShouldScanAllRows()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsTrue(table.ContainsColumn("c1"));
        Assert.IsTrue(table.ContainsColumn("c2"));
        Assert.IsFalse(table.ContainsColumn("missing"));
    }

    /// <summary>
    /// Verifies that <c>ContainsValue</c> locates stored values across all cells and rejects a value not present.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenQueried_ShouldScanAllCells()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsTrue(table.ContainsValue(1));
        Assert.IsTrue(table.ContainsValue(3));
        Assert.IsFalse(table.ContainsValue(99));
    }

    /// <summary>
    /// Verifies that <c>ContainsValue</c> supports <see langword="null" /> values for reference-typed values.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueIsNull_ShouldMatchStoredNull()
    {
        var table = new Table<string, string, string?>();
        table.Add("r1", "c1", null);

        Assert.IsTrue(table.ContainsValue(null));

        table.Remove("r1", "c1");

        Assert.IsFalse(table.ContainsValue(null));
    }

    /// <summary>
    /// Verifies that the containment members stop reporting a cell, row, and column once removed.
    /// </summary>
    [TestMethod]
    public void Contains_WhenCellRemoved_ShouldStopReporting()
    {
        Table<string, string, int> table = CreatePopulated();

        table.Remove("r1", "c2");

        Assert.IsFalse(table.Contains("r1", "c2"));
        Assert.IsFalse(table.ContainsColumn("c2"));
        Assert.IsTrue(table.ContainsRow("r1"));
    }

    /// <summary>
    /// Verifies that the key-taking containment members throw <see cref="ArgumentNullException" /> for
    /// <see langword="null" /> keys.
    /// </summary>
    [TestMethod]
    public void Contains_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var table = new Table<string, string, int>();

        var ex1 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.Contains(null!, "c1");
        });
        Assert.AreEqual("row", ex1.ParamName);

        var ex2 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.ContainsRow(null!);
        });
        Assert.AreEqual("row", ex2.ParamName);

        var ex3 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.ContainsColumn(null!);
        });
        Assert.AreEqual("column", ex3.ParamName);
    }
}
