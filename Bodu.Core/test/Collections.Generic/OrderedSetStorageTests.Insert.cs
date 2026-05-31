// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageTests.Insert.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class OrderedSetStorageTests
{

    /// <summary>
    /// Verifies that moved elements remain locatable through the hash table at their new indices.
    /// </summary>
    [TestMethod]
    public void Move_WhenItemRelocated_ShouldUpdateIndexOf()
    {
        OrderedSetStorage<int> sut = CreateStorage([10, 20, 30, 40]);

        sut.Move(0, 2);

        Assert.AreEqual(2, sut.IndexOf(10));
        Assert.AreEqual(0, sut.IndexOf(20));
        Assert.AreEqual(1, sut.IndexOf(30));
        Assert.AreEqual(3, sut.IndexOf(40));
    }

    /// <summary>
    /// Verifies that moving an element backward shifts the intervening elements rightwards.
    /// </summary>
    [TestMethod]
    public void Move_WhenMovingBackward_ShouldShiftInterveningRight()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3, 4, 5]);

        sut.Move(3, 1);

        CollectionAssert.AreEqual(new[] { 1, 4, 2, 3, 5 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that moving the first element to the last position cycles all other elements forward by one.
    /// </summary>
    [TestMethod]
    public void Move_WhenMovingFirstToLast_ShouldRotateLeftByOne()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3, 4]);

        sut.Move(0, 3);

        CollectionAssert.AreEqual(new[] { 2, 3, 4, 1 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that moving an element forward shifts the intervening elements leftwards.
    /// </summary>
    [TestMethod]
    public void Move_WhenMovingForward_ShouldShiftInterveningLeft()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3, 4, 5]);

        sut.Move(1, 3);

        CollectionAssert.AreEqual(new[] { 1, 3, 4, 2, 5 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that moving the last element to the first position cycles all other elements backward by one.
    /// </summary>
    [TestMethod]
    public void Move_WhenMovingLastToFirst_ShouldRotateRightByOne()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3, 4]);

        sut.Move(3, 0);

        CollectionAssert.AreEqual(new[] { 4, 1, 2, 3 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that an out-of-range <c>newIndex</c> is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, -1)]
    [DataRow(0, 3)]
    [DataRow(0, int.MaxValue)]
    public void Move_WhenNewIndexIsOutOfRange_ShouldThrowExactly(int oldIndex, int newIndex)
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.Move(oldIndex, newIndex);
        });
    }

    // --------------------------------------------------------
    // Move — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that an out-of-range <c>oldIndex</c> is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(3, 0)]
    [DataRow(int.MaxValue, 0)]
    public void Move_WhenOldIndexIsOutOfRange_ShouldThrowExactly(int oldIndex, int newIndex)
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.Move(oldIndex, newIndex);
        });
    }

    // --------------------------------------------------------
    // Move — positional behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that moving an element to the same index is a no-op and does not bump the version.
    /// </summary>
    [TestMethod]
    public void Move_WhenSourceAndDestinationAreEqual_ShouldBeNoOp()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);
        var versionBefore = sut._version;

        sut.Move(1, 1);

        Assert.AreEqual(versionBefore, sut._version);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that an out-of-range index is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    [DataRow(int.MinValue)]
    public void ReplaceAt_WhenIndexIsOutOfRange_ShouldThrowExactly(int index)
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.ReplaceAt(index, 99);
        });
    }

    /// <summary>
    /// Verifies that replacing an element with a value that already exists at a different index throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ReplaceAt_WhenValueExistsAtAnotherIndex_ShouldThrowExactly()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.ReplaceAt(0, 3);
        });
    }

    /// <summary>
    /// Verifies that replacing an index with the value it already holds is a no-op and does not bump the version.
    /// </summary>
    [TestMethod]
    public void ReplaceAt_WhenValueIsAlreadyAtIndex_ShouldBeNoOp()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);
        var versionBefore = sut._version;

        sut.ReplaceAt(1, 2);

        Assert.AreEqual(versionBefore, sut._version);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Snapshot(sut));
    }

    // --------------------------------------------------------
    // ReplaceAt — mutation behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that replacing an index with a new value updates the element and keeps it locatable
    /// through the hash table.
    /// </summary>
    [TestMethod]
    public void ReplaceAt_WhenValueIsNew_ShouldReplaceAndRehash()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        sut.ReplaceAt(1, 99);

        CollectionAssert.AreEqual(new[] { 1, 99, 3 }, Snapshot(sut));
        Assert.IsTrue(sut.Contains(99));
        Assert.IsFalse(sut.Contains(2));
        Assert.AreEqual(1, sut.IndexOf(99));
    }

    // --------------------------------------------------------
    // ReplaceAt — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.ReplaceAt(int, T)" /> rejects a <see langword="null" /> value.
    /// </summary>
    [TestMethod]
    public void ReplaceAt_WhenValueIsNull_ShouldThrowExactly()
    {
        OrderedSetStorage<string> sut = CreateStorage(["a", "b"]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.ReplaceAt(0, null!);
        });
    }

    /// <summary>
    /// Verifies that inserting at <see cref="OrderedSetStorage{T}.Count" /> appends the item.
    /// </summary>
    [TestMethod]
    public void TryInsert_WhenIndexEqualsCount_ShouldAppend()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        Assert.IsTrue(sut.TryInsert(3, 99));

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 99 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that an index strictly greater than <see cref="OrderedSetStorage{T}.Count" /> is rejected.
    /// </summary>
    [TestMethod]
    public void TryInsert_WhenIndexExceedsCount_ShouldThrowExactly()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.TryInsert(3, 99);
        });
    }

    /// <summary>
    /// Verifies that inserting at an interior index shifts later elements rightwards.
    /// </summary>
    [TestMethod]
    public void TryInsert_WhenIndexIsInterior_ShouldShiftLaterElementsRight()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        Assert.IsTrue(sut.TryInsert(1, 99));

        CollectionAssert.AreEqual(new[] { 1, 99, 2, 3 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that an out-of-range index is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(-100)]
    [DataRow(int.MinValue)]
    public void TryInsert_WhenIndexIsNegative_ShouldThrowExactly(int index)
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.TryInsert(index, 99);
        });
    }

    // --------------------------------------------------------
    // TryInsert — positional behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that inserting at the start prepends the item.
    /// </summary>
    [TestMethod]
    public void TryInsert_WhenIndexIsZero_ShouldPrepend()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        Assert.IsTrue(sut.TryInsert(0, 99));

        CollectionAssert.AreEqual(new[] { 99, 1, 2, 3 }, Snapshot(sut));
    }

    /// <summary>
    /// Verifies that an inserted item is locatable through the hash table afterwards.
    /// </summary>
    [TestMethod]
    public void TryInsert_WhenInserted_ShouldBeFindable()
    {
        OrderedSetStorage<int> sut = CreateStorage([10, 20, 30]);

        Assert.IsTrue(sut.TryInsert(1, 99));

        Assert.IsTrue(sut.Contains(99));
        Assert.AreEqual(1, sut.IndexOf(99));
    }

    // --------------------------------------------------------
    // TryInsert — duplicates
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that attempting to insert a duplicate returns <see langword="false" /> and does not change
    /// element order, count, or version.
    /// </summary>
    [TestMethod]
    public void TryInsert_WhenItemIsDuplicate_ShouldReturnFalseAndPreserveState()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);
        var versionBefore = sut._version;

        var inserted = sut.TryInsert(0, 2);

        Assert.IsFalse(inserted);
        Assert.AreEqual(3, sut.Count);
        Assert.AreEqual(versionBefore, sut._version);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Snapshot(sut));
    }
    // --------------------------------------------------------
    // TryInsert — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.TryInsert(int, T)" /> rejects a <see langword="null" /> item.
    /// </summary>
    [TestMethod]
    public void TryInsert_WhenItemIsNull_ShouldThrowExactly()
    {
        OrderedSetStorage<string> sut = CreateStorage(["a", "b"]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.TryInsert(0, null!);
        });
    }

    /// <summary>
    /// Verifies that inserting into an empty storage at index zero adds the only element.
    /// </summary>
    [TestMethod]
    public void TryInsert_WhenStorageIsEmpty_ShouldAddFirstElement()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        Assert.IsTrue(sut.TryInsert(0, 42));

        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(42, sut.GetAt(0));
    }

}
