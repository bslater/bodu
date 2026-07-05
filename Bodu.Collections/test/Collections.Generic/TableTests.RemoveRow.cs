// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.RemoveRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that <c>RemoveRow</c> removes every cell of the row and subtracts them all from the count.
    /// </summary>
    [TestMethod]
    public void RemoveRow_WhenRowExists_ShouldRemoveAllItsCells()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsTrue(table.RemoveRow("r1"));

        Assert.AreEqual(1, table.Count);
        Assert.IsFalse(table.ContainsRow("r1"));
        Assert.IsFalse(table.Contains("r1", "c1"));
        Assert.IsFalse(table.Contains("r1", "c2"));
        Assert.IsTrue(table.Contains("r2", "c1"));
    }

    /// <summary>
    /// Verifies that <c>RemoveRow</c> returns <see langword="false" /> for an absent row and leaves the table
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void RemoveRow_WhenRowMissing_ShouldReturnFalse()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsFalse(table.RemoveRow("missing"));
        Assert.AreEqual(3, table.Count);
    }

    /// <summary>
    /// Verifies that <c>RemoveRow</c> with a <see langword="null" /> row key throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void RemoveRow_WhenRowIsNull_ShouldThrowArgumentNullException()
    {
        var table = new Table<string, string, int>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.RemoveRow(null!);
        });
        Assert.AreEqual("row", ex.ParamName);
    }
}
