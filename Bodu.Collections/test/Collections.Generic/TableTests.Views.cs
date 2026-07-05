// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.Views.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that a row view is live: cells added and removed through the table after the view was created are
    /// reflected by the held view.
    /// </summary>
    [TestMethod]
    public void Row_WhenTableMutatedAfterViewCreated_ShouldReflectMutations()
    {
        Table<string, string, int> table = CreatePopulated();
        IReadOnlyDictionary<string, int> row = table.Row("r1");

        Assert.AreEqual(2, row.Count);

        table.Add("r1", "c3", 30);
        Assert.AreEqual(3, row.Count);
        Assert.AreEqual(30, row["c3"]);

        table.Remove("r1", "c1");
        Assert.AreEqual(2, row.Count);
        Assert.IsFalse(row.ContainsKey("c1"));
    }

    /// <summary>
    /// Verifies that a row view created before the row exists reports empty, becomes populated once the row appears,
    /// and reverts to empty when the row is removed again.
    /// </summary>
    [TestMethod]
    public void Row_WhenViewCreatedBeforeRowExists_ShouldTrackRowLifecycle()
    {
        var table = new Table<string, string, int>();
        IReadOnlyDictionary<string, int> row = table.Row("future");

        Assert.AreEqual(0, row.Count);
        Assert.IsFalse(row.ContainsKey("c1"));
        Assert.IsFalse(row.Any());

        table.Add("future", "c1", 1);
        Assert.AreEqual(1, row.Count);
        Assert.AreEqual(1, row["c1"]);

        table.RemoveRow("future");
        Assert.AreEqual(0, row.Count);
        Assert.IsFalse(row.Any());
    }

    /// <summary>
    /// Verifies the row view's read-only dictionary surface: keys, values, enumeration, <c>TryGetValue</c>, and the
    /// indexer throwing for a missing column.
    /// </summary>
    [TestMethod]
    public void Row_WhenAccessedAsReadOnlyDictionary_ShouldExposeRowCells()
    {
        Table<string, string, int> table = CreatePopulated();
        IReadOnlyDictionary<string, int> row = table.Row("r1");

        CollectionAssert.AreEquivalent(new[] { "c1", "c2" }, row.Keys.ToArray());
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, row.Values.ToArray());
        Assert.AreEqual(2, row.ToArray().Length);

        Assert.IsTrue(row.TryGetValue("c2", out int value));
        Assert.AreEqual(2, value);
        Assert.IsFalse(row.TryGetValue("missing", out _));

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = row["missing"];
        });
    }

    /// <summary>
    /// Verifies that a column view aggregates the column's cells across all rows and stays live across table
    /// mutations, including a view created before any row holds the column.
    /// </summary>
    [TestMethod]
    public void Column_WhenTableMutatedAfterViewCreated_ShouldReflectMutations()
    {
        Table<string, string, int> table = CreatePopulated();
        IReadOnlyDictionary<string, int> column = table.Column("c1");
        IReadOnlyDictionary<string, int> future = table.Column("c9");

        Assert.AreEqual(2, column.Count);
        Assert.AreEqual(1, column["r1"]);
        Assert.AreEqual(3, column["r2"]);
        Assert.AreEqual(0, future.Count);

        table.Add("r3", "c1", 5);
        Assert.AreEqual(3, column.Count);
        Assert.AreEqual(5, column["r3"]);

        table["r1", "c1"] = 100;
        Assert.AreEqual(100, column["r1"]);

        table.Add("r1", "c9", 9);
        Assert.AreEqual(1, future.Count);
        Assert.AreEqual(9, future["r1"]);

        table.RemoveColumn("c1");
        Assert.AreEqual(0, column.Count);
        Assert.IsFalse(column.Any());
    }

    /// <summary>
    /// Verifies the column view's read-only dictionary surface: keys, values, enumeration, <c>ContainsKey</c>,
    /// <c>TryGetValue</c>, and the indexer throwing for a row that lacks the column.
    /// </summary>
    [TestMethod]
    public void Column_WhenAccessedAsReadOnlyDictionary_ShouldExposeColumnCells()
    {
        Table<string, string, int> table = CreatePopulated();
        IReadOnlyDictionary<string, int> column = table.Column("c2");

        CollectionAssert.AreEquivalent(new[] { "r1" }, column.Keys.ToArray());
        CollectionAssert.AreEquivalent(new[] { 2 }, column.Values.ToArray());
        Assert.AreEqual(1, column.ToArray().Length);

        Assert.IsTrue(column.ContainsKey("r1"));
        Assert.IsFalse(column.ContainsKey("r2"));

        Assert.IsTrue(column.TryGetValue("r1", out int value));
        Assert.AreEqual(2, value);
        Assert.IsFalse(column.TryGetValue("r2", out _));

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = column["r2"];
        });
    }

    /// <summary>
    /// Verifies that <c>RowKeys</c> is a live view over the outer row keys, reflecting row additions and prunes.
    /// </summary>
    [TestMethod]
    public void RowKeys_WhenTableMutated_ShouldStayLive()
    {
        Table<string, string, int> table = CreatePopulated();
        IReadOnlyCollection<string> rowKeys = table.RowKeys;

        CollectionAssert.AreEquivalent(new[] { "r1", "r2" }, rowKeys.ToArray());

        table.Add("r3", "c1", 4);
        CollectionAssert.AreEquivalent(new[] { "r1", "r2", "r3" }, rowKeys.ToArray());

        table.RemoveRow("r1");
        CollectionAssert.AreEquivalent(new[] { "r2", "r3" }, rowKeys.ToArray());
    }

    /// <summary>
    /// Verifies that <c>ColumnKeys</c> returns the distinct columns across all rows, deduplicated, and that a fresh
    /// read reflects later mutations.
    /// </summary>
    [TestMethod]
    public void ColumnKeys_WhenRead_ShouldReturnDistinctColumnsAcrossRows()
    {
        Table<string, string, int> table = CreatePopulated();

        CollectionAssert.AreEquivalent(new[] { "c1", "c2" }, table.ColumnKeys.ToArray());

        table.Add("r2", "c3", 9);
        CollectionAssert.AreEquivalent(new[] { "c1", "c2", "c3" }, table.ColumnKeys.ToArray());

        table.RemoveColumn("c1");
        CollectionAssert.AreEquivalent(new[] { "c2", "c3" }, table.ColumnKeys.ToArray());
    }

    /// <summary>
    /// Verifies that <c>RowMap</c> pairs every row key with a live row view whose cells match the table.
    /// </summary>
    [TestMethod]
    public void RowMap_WhenIterated_ShouldPairEachRowWithItsLiveView()
    {
        Table<string, string, int> table = CreatePopulated();

        Dictionary<string, IReadOnlyDictionary<string, int>> map = table.RowMap()
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        CollectionAssert.AreEquivalent(new[] { "r1", "r2" }, map.Keys.ToArray());
        Assert.AreEqual(2, map["r1"].Count);
        Assert.AreEqual(3, map["r2"]["c1"]);

        // The yielded views are live: a later table mutation is visible through the captured view.
        table.Add("r1", "c3", 30);
        Assert.AreEqual(30, map["r1"]["c3"]);
    }

    /// <summary>
    /// Verifies that <c>Row</c> and <c>Column</c> throw <see cref="ArgumentNullException" /> for
    /// <see langword="null" /> keys.
    /// </summary>
    [TestMethod]
    public void Views_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var table = new Table<string, string, int>();

        var ex1 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.Row(null!);
        });
        Assert.AreEqual("row", ex1.ParamName);

        var ex2 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.Column(null!);
        });
        Assert.AreEqual("column", ex2.ParamName);
    }
}
