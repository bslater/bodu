// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="IntervalTree{T}" />.
/// </summary>
[TestClass]
public partial class IntervalTreeTests
{
    /// <summary>
    /// Verifies the happy-path smoke check: adding overlapping intervals stores them all and a stabbing query
    /// returns the intervals containing the point in ascending order.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void IntervalTree_AddAndQueryPoint_ShouldReturnContainingIntervals()
    {
        var sut = new IntervalTree<int>();

        sut.Add(9, 11);
        sut.Add(10, 12);
        sut.Add(14, 15);

        CollectionAssert.AreEqual(new[] { (9, 11), (10, 12) }, sut.QueryPoint(10).ToList());
        Assert.AreEqual(3, sut.Count);
    }

    /// <summary>
    /// Creates an <see cref="IntervalTree{T}" /> of <see cref="int" /> seeded with the specified intervals via
    /// <see cref="IntervalTree{T}.Add" />.
    /// </summary>
    /// <param name="intervals">The intervals to add, in the given order.</param>
    /// <returns>The populated tree.</returns>
    private static IntervalTree<int> CreateTree(params (int Low, int High)[] intervals)
    {
        var tree = new IntervalTree<int>();
        foreach ((int low, int high) in intervals)
            tree.Add(low, high);

        return tree;
    }

    /// <summary>
    /// Materialises the intervals of <paramref name="tree" /> using the public struct enumerator.
    /// </summary>
    /// <typeparam name="T">The endpoint type of the tree.</typeparam>
    /// <param name="tree">The tree to snapshot.</param>
    /// <returns>A list containing the intervals in enumeration order.</returns>
    private static List<(T Low, T High)> SnapshotByEnumerator<T>(IntervalTree<T> tree)
        where T : notnull
    {
        var result = new List<(T Low, T High)>(tree.Count);
        foreach ((T Low, T High) interval in tree)
            result.Add(interval);

        return result;
    }
}
