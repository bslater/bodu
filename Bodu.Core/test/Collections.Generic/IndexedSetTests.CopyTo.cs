// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.CopyTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

public partial class IndexedSetTests
{
    // --------------------------------------------------------
    // CopyTo — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.CopyTo(T[], int)" /> rejects a <see langword="null" /> array.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        IndexedSet<int> sut = CreateSet([1, 2]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.CopyTo(T[], int)" /> throws <see cref="ArgumentException" />
    /// when the array is too small.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsTooSmall_ShouldThrowArgumentException()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.CopyTo(new int[2], 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.CopyTo(T[], int)" /> rejects a negative offset.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void CopyTo_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException(int arrayIndex)
    {
        IndexedSet<int> sut = CreateSet([1, 2]);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            sut.CopyTo(new int[10], arrayIndex);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.CopyTo(T[], int)" /> starts copying at the supplied offset.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsNonZero_ShouldCopyAtCorrectPosition()
    {
        IndexedSet<int> sut = CreateSet([10, 20]);
        int[] target = [99, 99, 99, 99];

        sut.CopyTo(target, 1);

        CollectionAssert.AreEqual(new[] { 99, 10, 20, 99 }, target);
    }

    // --------------------------------------------------------
    // CopyTo — behaviour
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.CopyTo(T[], int)" /> copies elements in insertion order.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsZero_ShouldCopyInInsertionOrder()
    {
        IndexedSet<int> sut = CreateSet([10, 20, 30]);
        var target = new int[3];

        sut.CopyTo(target, 0);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.CopyTo(T[], int)" /> throws <see cref="ArgumentException" />
    /// when the remaining space after the offset is insufficient.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenRemainingSpaceAfterOffsetIsInsufficient_ShouldThrowArgumentException()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.CopyTo(new int[5], 3);
        });
    }

    /// <summary>
    /// Verifies that copying an empty set does not modify the destination array.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenSetIsEmpty_ShouldNotModifyArray()
    {
        var sut = new IndexedSet<int>();
        int[] target = [99, 88];

        sut.CopyTo(target, 0);

        CollectionAssert.AreEqual(new[] { 99, 88 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.ToArray" /> returns a disconnected snapshot.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ShouldReturnDisconnectedSnapshot()
    {
        IndexedSet<int> sut = CreateSet([1, 2, 3]);
        var snapshot = sut.ToArray();

        sut.Add(4);
        sut.Remove(1);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, snapshot);
    }

    // --------------------------------------------------------
    // ToArray
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.ToArray" /> on an empty set returns an empty array.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSetIsEmpty_ShouldReturnEmptyArray()
    {
        var sut = new IndexedSet<int>();

        var array = sut.ToArray();

        Assert.IsEmpty(array);
    }

    /// <summary>
    /// Verifies that <see cref="IndexedSet{T}.ToArray" /> returns the elements in insertion order.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSetPopulated_ShouldReturnInsertionOrderedArray()
    {
        IndexedSet<int> sut = CreateSet([10, 20, 30]);

        var array = sut.ToArray();

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, array);
    }

}
