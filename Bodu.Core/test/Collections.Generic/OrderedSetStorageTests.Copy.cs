// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageTests.Copy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class OrderedSetStorageTests
{
    // --------------------------------------------------------
    // CopyTo — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.CopyTo(T[], int)" /> rejects a <see langword="null" /> array.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.CopyTo(T[], int)" /> throws <see cref="ArgumentException" />
    /// when the array is too small to hold the storage.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsTooSmall_ShouldThrowExactly()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.CopyTo(new int[2], 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.CopyTo(T[], int)" /> rejects a negative offset.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void CopyTo_WhenIndexIsNegative_ShouldThrowExactly(int arrayIndex)
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.CopyTo(new int[10], arrayIndex);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.CopyTo(T[], int)" /> copies the elements starting at the
    /// specified offset, leaving earlier slots untouched.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsNonZero_ShouldCopyStartingAtOffset()
    {
        OrderedSetStorage<int> sut = CreateStorage([10, 20]);
        int[] target = [99, 99, 99, 99];

        sut.CopyTo(target, 1);

        CollectionAssert.AreEqual(new[] { 99, 10, 20, 99 }, target);
    }

    // --------------------------------------------------------
    // CopyTo — successful copy behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.CopyTo(T[], int)" /> copies the elements in insertion order.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsZero_ShouldCopyInInsertionOrder()
    {
        OrderedSetStorage<int> sut = CreateStorage([10, 20, 30]);
        int[] target = new int[3];

        sut.CopyTo(target, 0);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.CopyTo(T[], int)" /> throws <see cref="ArgumentException" />
    /// when there is insufficient space after the supplied offset.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenRemainingSpaceAfterOffsetIsInsufficient_ShouldThrowExactly()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.CopyTo(new int[5], 3);
        });
    }

    /// <summary>
    /// Verifies that copying from an empty storage does not modify the destination.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenStorageIsEmpty_ShouldNotModifyArray()
    {
        var sut = new OrderedSetStorage<int>(0, null);
        int[] target = [99, 88];

        sut.CopyTo(target, 0);

        CollectionAssert.AreEqual(new[] { 99, 88 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.ToArray" /> returns a fresh array that is independent of
    /// subsequent storage mutations.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ShouldReturnDisconnectedSnapshot()
    {
        OrderedSetStorage<int> sut = CreateStorage([1, 2, 3]);
        int[] snapshot = sut.ToArray();

        sut.Add(4);

        Assert.HasCount(3, snapshot);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, snapshot);
    }

    // --------------------------------------------------------
    // ToArray
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.ToArray" /> on an empty storage returns an empty array.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenStorageIsEmpty_ShouldReturnEmptyArray()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        int[] array = sut.ToArray();

        Assert.IsEmpty(array);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorage{T}.ToArray" /> returns a snapshot of insertion order with
    /// matching length and contents.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenStoragePopulated_ShouldReturnInsertionOrderedArray()
    {
        OrderedSetStorage<int> sut = CreateStorage([10, 20, 30]);

        int[] array = sut.ToArray();

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, array);
        Assert.HasCount(sut.Count, array);
    }

}
