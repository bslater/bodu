// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.Comparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that a case-insensitive row comparer resolves row keys regardless of casing across the flat surface
    /// and the views.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenRowComparerIsOrdinalIgnoreCase_ShouldResolveRowKeysCaseInsensitively()
    {
        var table = new Table<string, string, int>(StringComparer.OrdinalIgnoreCase, null);
        table.Add("Alpha", "c1", 1);

        Assert.IsTrue(table.ContainsRow("ALPHA"));
        Assert.AreEqual(1, table["alpha", "c1"]);
        Assert.AreEqual(1, table.Row("aLpHa").Count);
        Assert.AreEqual(1, table.Column("c1")["ALPHA"]);

        // The differently cased key addresses the same row: the cell is a duplicate.
        Assert.IsFalse(table.TryAdd("ALPHA", "c1", 2));
        Assert.AreEqual(1, table.RowKeys.Count);
    }

    /// <summary>
    /// Verifies that a case-insensitive column comparer resolves column keys regardless of casing across the flat
    /// surface, the views, and the distinct <c>ColumnKeys</c> computation.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenColumnComparerIsOrdinalIgnoreCase_ShouldResolveColumnKeysCaseInsensitively()
    {
        var table = new Table<string, string, int>(null, StringComparer.OrdinalIgnoreCase);
        table.Add("r1", "Total", 10);
        table.Add("r2", "TOTAL", 20);

        Assert.IsTrue(table.ContainsColumn("total"));
        Assert.AreEqual(10, table["r1", "TOTAL"]);
        Assert.AreEqual(20, table.Column("total")["r2"]);
        Assert.IsTrue(table.Row("r1").ContainsKey("tOtAl"));

        // The two casings are one column under the comparer: ColumnKeys deduplicates to a single key.
        Assert.AreEqual(1, table.ColumnKeys.Count);

        Assert.IsFalse(table.TryAdd("r1", "toTal", 99));
    }

    /// <summary>
    /// Verifies that case-insensitive comparers on both axes address one cell for every casing combination, and that
    /// removal through a differently cased pair removes that cell.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenBothAxesAreOrdinalIgnoreCase_ShouldAddressSingleCellAcrossCasings()
    {
        var table = new Table<string, string, int>(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);
        table.Add("Row", "Col", 5);

        Assert.AreEqual(5, table["ROW", "COL"]);
        Assert.AreEqual(1, table.Count);

        table["row", "col"] = 6;
        Assert.AreEqual(6, table["Row", "Col"]);
        Assert.AreEqual(1, table.Count);

        Assert.IsTrue(table.Remove("rOw", "cOl"));
        Assert.IsTrue(table.IsEmpty);
    }
}
