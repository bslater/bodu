// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeTests
{
    /// <summary>
    /// Verifies that enumeration yields all intervals ascending by the (low, high) lexicographic pair regardless of
    /// insertion order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenIntervalsUnordered_ShouldYieldAscendingByLowThenHigh()
    {
        var sut = CreateTree((10, 20), (1, 9), (10, 15), (5, 30), (1, 3));

        CollectionAssert.AreEqual(
            new[] { (1, 3), (1, 9), (5, 30), (10, 15), (10, 20) }, SnapshotByEnumerator(sut));
    }

    /// <summary>
    /// Verifies that enumeration repeats duplicated intervals once per stored occurrence, adjacent in the order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDuplicatesStored_ShouldRepeatPerOccurrence()
    {
        var sut = CreateTree((5, 8), (1, 3), (5, 8), (5, 8));

        CollectionAssert.AreEqual(new[] { (1, 3), (5, 8), (5, 8), (5, 8) }, SnapshotByEnumerator(sut));
    }

    /// <summary>
    /// Verifies that enumerating an empty tree yields nothing.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenTreeEmpty_ShouldYieldNothing()
    {
        var sut = new IntervalTree<int>();

        Assert.AreEqual(0, SnapshotByEnumerator(sut).Count);
    }

    /// <summary>
    /// Verifies that the struct enumerator fails fast — <see cref="IntervalTree{T}.Enumerator.MoveNext" /> throws
    /// <see cref="InvalidOperationException" /> after any structural mutation, including a duplicate-count change.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenTreeMutated_ShouldThrowInvalidOperationException()
    {
        var sut = CreateTree((1, 5), (6, 9));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach ((int Low, int High) _ in sut)
                sut.Add(10, 12);
        });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach ((int Low, int High) _ in sut)
                sut.Add(1, 5);
        });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach ((int Low, int High) _ in sut)
                sut.Clear();
        });
    }

    /// <summary>
    /// Verifies that <see cref="IntervalTree{T}.Enumerator.Reset" /> restarts the walk from the smallest interval.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenReset_ShouldRestartFromSmallestInterval()
    {
        var sut = CreateTree((1, 5), (6, 9));
        IntervalTree<int>.Enumerator enumerator = sut.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());

        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual((1, 5), enumerator.Current);
    }

    /// <summary>
    /// Verifies that the <see cref="IEnumerable{T}" /> interface path yields the same ascending sequence as the
    /// struct enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAccessedThroughInterface_ShouldYieldSameSequence()
    {
        var sut = CreateTree((10, 20), (1, 9), (1, 9));
        IEnumerable<(int Low, int High)> interfaceView = sut;

        CollectionAssert.AreEqual(SnapshotByEnumerator(sut), interfaceView.ToList());
    }
}
