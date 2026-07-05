// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public partial class TableTests
{
    /// <summary>
    /// Creates a table with three cells across two rows: <c>("r1", "c1") = 1</c>, <c>("r1", "c2") = 2</c>, and
    /// <c>("r2", "c1") = 3</c>.
    /// </summary>
    /// <returns>A populated <see cref="Table{TRow, TColumn, TValue}" />.</returns>
    private static Table<string, string, int> CreatePopulated()
    {
        var table = new Table<string, string, int>();
        table.Add("r1", "c1", 1);
        table.Add("r1", "c2", 2);
        table.Add("r2", "c1", 3);
        return table;
    }

    /// <summary>
    /// Verifies that cells added through the flat surface resolve through both the row and column projections.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Table_WhenPopulated_ShouldResolveThroughRowAndColumnViews()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(1, table["r1", "c1"]);

        IReadOnlyDictionary<string, int> row = table.Row("r1");
        Assert.AreEqual(2, row.Count);
        Assert.AreEqual(2, row["c2"]);

        IReadOnlyDictionary<string, int> column = table.Column("c1");
        Assert.AreEqual(2, column.Count);
        Assert.AreEqual(3, column["r2"]);
    }
}
