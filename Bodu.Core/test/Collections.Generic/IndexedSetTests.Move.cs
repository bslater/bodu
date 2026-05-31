// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.Move.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IndexedSetTests
{

    /// <summary>
    /// Verifies that moved elements remain locatable through <see cref="IndexedSet{T}.IndexOf(T)" /> at
    /// their new positions.
    /// </summary>
    [TestMethod]
    public void Move_WhenItemRelocated_ShouldUpdateIndexOf()
    {
        IndexedSet<int> sut = CreateSet([10, 20, 30, 40]);

        sut.Move(0, 2);

        Assert.AreEqual(2, sut.IndexOf(10));
        Assert.AreEqual(0, sut.IndexOf(20));
        Assert.AreEqual(1, sut.IndexOf(30));
        Assert.AreEqual(3, sut.IndexOf(40));
    }

    /// <summary>
    /// Verifies that moving an element backward shifts intervening elements rightwards.
    /// </summary>
    [TestMethod]
    public void Move_WhenMovingBackward_ShouldShiftInterveningRight()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3, 4, 5]);

        sut.Move(3, 1);

        CollectionAssert.AreEqual(new[] { 1, 4, 2, 3, 5 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that moving an element forward shifts intervening elements leftwards.
    /// </summary>
    [TestMethod]
    public void Move_WhenMovingForward_ShouldShiftInterveningLeft()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3, 4, 5]);

        sut.Move(1, 3);

        CollectionAssert.AreEqual(new[] { 1, 3, 4, 2, 5 }, SnapshotByIndexer(sut));
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Move(int, int)" /> rejects an out-of-range <c>newIndex</c>.
    /// </summary>
    [TestMethod]
    [DataRow(0, -1)]
    [DataRow(0, 3)]
    [DataRow(0, int.MaxValue)]
    public void Move_WhenNewIndexIsOutOfRange_ShouldThrowExactly(int oldIndex, int newIndex)
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.Move(oldIndex, newIndex);
        });
    }
    // --------------------------------------------------------
    // Move — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Move(int, int)" /> rejects an out-of-range <c>oldIndex</c>.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(3, 0)]
    [DataRow(int.MaxValue, 0)]
    public void Move_WhenOldIndexIsOutOfRange_ShouldThrowExactly(int oldIndex, int newIndex)
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.Move(oldIndex, newIndex);
        });
    }

    // --------------------------------------------------------
    // Move — behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.Move(int, int)" /> with identical indices is a no-op.
    /// </summary>
    [TestMethod]
    public void Move_WhenSourceAndDestinationAreEqual_ShouldLeaveSetUnchanged()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        sut.Move(1, 1);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, SnapshotByIndexer(sut));
    }

}
