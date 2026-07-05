// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeGenericTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeGenericTests
{
    /// <summary>
    /// Verifies that enumeration yields entries ascending by (low, high) with each interval's values in insertion
    /// order, regardless of insertion sequence.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenEntriesUnordered_ShouldYieldAscendingWithValuesInInsertionOrder()
    {
        var sut = CreateTree((10, 20, "c"), (1, 9, "a"), (10, 20, "d"), (5, 30, "b"));

        var result = new List<(int Low, int High, string Value)>();
        foreach ((int Low, int High, string Value) entry in sut)
            result.Add(entry);

        CollectionAssert.AreEqual(
            new[] { (1, 9, "a"), (5, 30, "b"), (10, 20, "c"), (10, 20, "d") }, result);
    }

    /// <summary>
    /// Verifies that enumerating an empty tree yields nothing.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenTreeEmpty_ShouldYieldNothing()
    {
        var sut = new IntervalTree<int, string>();

        Assert.AreEqual(0, sut.ToList().Count);
    }

    /// <summary>
    /// Verifies that the struct enumerator fails fast — <see cref="IntervalTree{TKey, TValue}.Enumerator.MoveNext" />
    /// throws <see cref="InvalidOperationException" /> after any mutation, including a value-list append.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenTreeMutated_ShouldThrowInvalidOperationException()
    {
        var sut = CreateTree((1, 5, "a"), (6, 9, "b"));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach ((int Low, int High, string Value) _ in sut)
                sut.Add(1, 5, "another");
        });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach ((int Low, int High, string Value) _ in sut)
                sut.Clear();
        });
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{TKey, TValue}.Enumerator.Reset" /> restarts the walk from the first
    /// entry of the smallest interval.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenReset_ShouldRestartFromFirstEntry()
    {
        var sut = CreateTree((1, 5, "first"), (1, 5, "second"), (6, 9, "third"));
        IntervalTree<int, string>.Enumerator enumerator = sut.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());

        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual((1, 5, "first"), enumerator.Current);
    }
}
