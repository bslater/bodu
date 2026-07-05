// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that enumeration yields every cell exactly once as a tuple-keyed pair, with count parity against
    /// <c>Count</c>.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenPopulated_ShouldYieldAllCellsWithCountParity()
    {
        Table<string, string, int> table = CreatePopulated();

        KeyValuePair<(string Row, string Column), int>[] cells = [.. table];

        Assert.AreEqual(table.Count, cells.Length);
        CollectionAssert.AreEquivalent(
            new[]
            {
                new KeyValuePair<(string, string), int>(("r1", "c1"), 1),
                new KeyValuePair<(string, string), int>(("r1", "c2"), 2),
                new KeyValuePair<(string, string), int>(("r2", "c1"), 3),
            },
            cells);
    }

    /// <summary>
    /// Verifies that enumeration is row-major: all cells of one row are contiguous in the yielded sequence.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenMultipleRows_ShouldYieldRowCellsContiguously()
    {
        Table<string, string, int> table = CreatePopulated();
        table.Add("r2", "c2", 4);

        string[] rowSequence = table.Select(cell => cell.Key.Row).ToArray();

        // Row-major means each distinct row key forms exactly one consecutive run: the number of adjacent
        // transitions equals the number of distinct rows minus one.
        var transitions = 0;
        for (var i = 1; i < rowSequence.Length; i++)
        {
            if (!string.Equals(rowSequence[i], rowSequence[i - 1], StringComparison.Ordinal))
                transitions++;
        }

        Assert.AreEqual(4, rowSequence.Length);
        Assert.AreEqual(rowSequence.Distinct(StringComparer.Ordinal).Count() - 1, transitions);
    }

    /// <summary>
    /// Verifies that enumerating an empty table yields no cells.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenEmpty_ShouldYieldNothing()
    {
        var table = new Table<string, string, int>();

        Assert.IsFalse(table.Any());
    }

    /// <summary>
    /// Verifies that mutating the table while enumerating throws <see cref="InvalidOperationException" />, matching
    /// the underlying dictionary's fail-fast contract.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenTableMutatedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        Table<string, string, int> table = CreatePopulated();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var next = 0;
            foreach (KeyValuePair<(string Row, string Column), int> cell in table)
                table.Add($"new-row-{next++}", "c9", 99);
        });
    }
}
