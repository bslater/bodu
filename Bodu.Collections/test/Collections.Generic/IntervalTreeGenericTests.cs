// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeGenericTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="IntervalTree{TKey, TValue}" />.
/// </summary>
[TestClass]
public partial class IntervalTreeGenericTests
{
    /// <summary>
    /// Verifies the happy-path smoke check: adding overlapping entries stores them all and a stabbing query
    /// returns the entries containing the point with their values.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void IntervalTree_AddAndQueryPoint_ShouldReturnContainingEntries()
    {
        var sut = new IntervalTree<int, string>();

        sut.Add(9, 11, "stand-up");
        sut.Add(10, 12, "design review");
        sut.Add(14, 15, "1:1");

        CollectionAssert.AreEqual(
            new[] { (9, 11, "stand-up"), (10, 12, "design review") }, sut.QueryPoint(10).ToList());
        Assert.AreEqual(3, sut.Count);
    }

    /// <summary>
    /// Creates an <see cref="IntervalTree{TKey, TValue}" /> of <see cref="int" /> endpoints and <see cref="string" />
    /// values seeded with the specified entries via <see cref="IntervalTree{TKey, TValue}.Add" />.
    /// </summary>
    /// <param name="entries">The entries to add, in the given order.</param>
    /// <returns>The populated tree.</returns>
    private static IntervalTree<int, string> CreateTree(params (int Low, int High, string Value)[] entries)
    {
        var tree = new IntervalTree<int, string>();
        foreach ((int low, int high, string value) in entries)
            tree.Add(low, high, value);

        return tree;
    }
}
