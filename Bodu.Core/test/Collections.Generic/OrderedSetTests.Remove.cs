// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetTests.Remove.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class OrderedSetTests
{

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.Clear" /> on an already-empty set remains empty.
    /// </summary>
    [TestMethod]
    public void Clear_WhenAlreadyEmpty_ShouldRemainEmpty()
    {
        var sut = new OrderedSet<int>();

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
    }

    // --------------------------------------------------------
    // Clear
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.Clear" /> removes every element and resets <see cref="OrderedSet{T}.Count" />.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldRemoveAllElements()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.Contains(1));
        Assert.IsFalse(sut.Contains(2));
        Assert.IsFalse(sut.Contains(3));
    }

    /// <summary>
    /// Verifies that items added after <see cref="OrderedSet{T}.Clear" /> are tracked normally.
    /// </summary>
    [TestMethod]
    public void Clear_WhenItemsReAddedAfterClear_ShouldTrackNewItems()
    {
        OrderedSet<int> sut = CreateSet([1, 2]);

        sut.Clear();
        sut.Add(99);

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(99, sut[0]);
    }

    // --------------------------------------------------------
    // Remove — behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that removing an absent item returns <see langword="false" /> and leaves the set unchanged.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsAbsent_ShouldReturnFalseAndLeaveSetUnchanged()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        var removed = sut.Remove(99);

        Assert.IsFalse(removed);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }
    // --------------------------------------------------------
    // Remove — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.Remove(T)" /> rejects a <see langword="null" /> item.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsNull_ShouldThrowExactly()
    {
        OrderedSet<string> sut = CreateSet(["a"]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Remove(null!);
        });
    }

    /// <summary>
    /// Verifies that removing a present item returns <see langword="true" /> and compacts the remaining order.
    /// </summary>
    [TestMethod]
    [DataRow(1, new[] { 2, 3 })]
    [DataRow(2, new[] { 1, 3 })]
    [DataRow(3, new[] { 1, 2 })]
    public void Remove_WhenItemIsPresent_ShouldReturnTrueAndCompactOrder(int target, int[] expected)
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        var removed = sut.Remove(target);

        Assert.IsTrue(removed);
        CollectionAssert.AreEqual(expected, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that re-adding a previously removed item appends it to the end.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemReAddedAfterRemoval_ShouldAppendAtEnd()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        sut.Remove(2);
        sut.Add(2);

        CollectionAssert.AreEqual(new[] { 1, 3, 2 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that a removed item is no longer reported by <see cref="OrderedSet{T}.Contains(T)" /> or
    /// <see cref="OrderedSet{T}.IndexOf(T)" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemRemoved_ShouldNoLongerBeReportedPresent()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        sut.Remove(2);

        Assert.IsFalse(sut.Contains(2));
        Assert.AreEqual(-1, sut.IndexOf(2));
    }

}
