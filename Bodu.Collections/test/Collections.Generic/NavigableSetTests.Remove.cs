// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that removing a present item returns <see langword="true" /> and decrements the count.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsPresent_ShouldReturnTrueAndDecrementCount()
    {
        var sut = CreateSet(1, 2, 3);

        bool removed = sut.Remove(2);

        Assert.IsTrue(removed);
        Assert.AreEqual(2, sut.Count);
        Assert.IsFalse(sut.Contains(2));
    }

    /// <summary>
    /// Verifies that removing an absent item returns <see langword="false" /> and leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsAbsent_ShouldReturnFalseAndLeaveSetUnchanged()
    {
        var sut = CreateSet(1, 2, 3);

        bool removed = sut.Remove(99);

        Assert.IsFalse(removed);
        Assert.AreEqual(3, sut.Count);
    }

    /// <summary>
    /// Verifies that removing a leaf node preserves the remaining sorted order.
    /// </summary>
    [TestMethod]
    public void Remove_WhenTargetIsLeaf_ShouldPreserveOrder()
    {
        // 2 becomes the root with leaves 1 and 3; removing 1 removes a leaf.
        var sut = CreateSet(2, 1, 3);

        Assert.IsTrue(sut.Remove(1));

        CollectionAssert.AreEqual(new[] { 2, 3 }, sut.ToList());
    }

    /// <summary>
    /// Verifies that removing the root of the tree preserves the remaining sorted order.
    /// </summary>
    [TestMethod]
    public void Remove_WhenTargetIsRoot_ShouldPreserveOrder()
    {
        var sut = CreateSet(2, 1, 3);

        Assert.IsTrue(sut.Remove(2));

        CollectionAssert.AreEqual(new[] { 1, 3 }, sut.ToList());
    }

    /// <summary>
    /// Verifies that removing an interior node with two children (replaced by its in-order successor) preserves the
    /// remaining sorted order and rank integrity.
    /// </summary>
    [TestMethod]
    public void Remove_WhenTargetHasTwoChildren_ShouldPreserveOrderAndRanks()
    {
        var sut = CreateSet(50, 25, 75, 10, 30, 60, 90, 27, 35);

        // 25 has two children (10 and 30 with its own children); its successor is 27.
        Assert.IsTrue(sut.Remove(25));

        CollectionAssert.AreEqual(new[] { 10, 27, 30, 35, 50, 60, 75, 90 }, sut.ToList());
        for (int rank = 0; rank < sut.Count; rank++)
            Assert.AreEqual(rank, sut.IndexOf(sut.GetAt(rank)));
    }

    /// <summary>
    /// Verifies that removing a node with a single child splices the child into place.
    /// </summary>
    [TestMethod]
    public void Remove_WhenTargetHasSingleChild_ShouldSpliceChild()
    {
        var sut = CreateSet(10, 5, 20, 15);

        // 20 has the single (red) child 15.
        Assert.IsTrue(sut.Remove(20));

        CollectionAssert.AreEqual(new[] { 5, 10, 15 }, sut.ToList());
    }

    /// <summary>
    /// Verifies that removing the only element empties the set and clears <see cref="NavigableSet{T}.Min" /> access.
    /// </summary>
    [TestMethod]
    public void Remove_WhenLastElementRemoved_ShouldEmptySet()
    {
        var sut = CreateSet(7);

        Assert.IsTrue(sut.Remove(7));

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.TryGetMin(out _));
    }

    /// <summary>
    /// Verifies that draining the set element by element — ascending, descending, and middle-out — always keeps the
    /// remainder sorted and rank-consistent, exercising every red-black deletion path.
    /// </summary>
    [TestMethod]
    public void Remove_WhenDrainingInTargetedOrders_ShouldStayConsistent()
    {
        int[][] removalOrders =
        [
            Enumerable.Range(0, 33).ToArray(),
            Enumerable.Range(0, 33).Reverse().ToArray(),
            Enumerable.Range(0, 33).OrderBy(i => Math.Abs(i - 16)).ToArray(),
        ];

        foreach (int[] order in removalOrders)
        {
            var sut = new NavigableSet<int>(Enumerable.Range(0, 33));
            var expected = new List<int>(Enumerable.Range(0, 33));

            foreach (int item in order)
            {
                Assert.IsTrue(sut.Remove(item));
                expected.Remove(item);

                CollectionAssert.AreEqual(expected, sut.ToList());
                for (int rank = 0; rank < sut.Count; rank++)
                    Assert.AreEqual(expected[rank], sut.GetAt(rank));
            }
        }
    }

    /// <summary>
    /// Verifies that removing a <see langword="null" /> item throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableSet<string>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.Remove(null!);
        }, "item");
    }
}
