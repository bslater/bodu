// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.ICollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{
    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> correctly copies all elements to the destination array starting at index zero.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenBufferHasElements_ShouldCopyElementsToArray()
    {
        var buffer = new CircularBuffer<char>(3);
        buffer.Enqueue('a');
        buffer.Enqueue('b');
        var target = new char[3];
        ((ICollection)buffer).CopyTo(target, 0);
        Assert.AreEqual('a', target[0]);
        Assert.AreEqual('b', target[1]);
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> throws ArgumentNullException when the destination array is null.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsNull_ShouldThrowException()
    {
        var buffer = new CircularBuffer<string>(1);
        buffer.Enqueue("a");
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ((ICollection)buffer).CopyTo(null, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> throws ArgumentException when the destination is a multidimensional array.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsMultiDimensional_ShouldThrowException()
    {
        var buffer = new CircularBuffer<int>(1);
        buffer.Enqueue(1);
        var multiDim = new int[2, 2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)buffer).CopyTo(multiDim, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> throws ArgumentException when the destination array has a non-zero lower bound.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsNotZeroBased_ShouldThrowException()
    {
        var buffer = new CircularBuffer<int>(1);
        buffer.Enqueue(1);
        var nonZeroBased = Array.CreateInstance(typeof(int), lengths: [4], lowerBounds: [1]);
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)buffer).CopyTo(nonZeroBased, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> throws ArgumentOutOfRangeException when the target index is negative.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenIndexIsNegative_ShouldThrowException()
    {
        var buffer = new CircularBuffer<int>(2);
        buffer.Enqueue(1);
        var array = new int[3];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ((ICollection)buffer).CopyTo(array, -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> throws ArgumentException when the target index equals the array length, leaving no addressable position.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenIndexEqualsArrayLength_ShouldThrowException()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);
        var array = new int[6];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)buffer).CopyTo(array, 6);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> throws ArgumentException when the destination array is too small to hold all elements.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsTooSmall_ShouldThrowException()
    {
        var buffer = new CircularBuffer<int>(2);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        var array = new int[1];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)buffer).CopyTo(array, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> throws ArgumentException when the target index offset leaves insufficient space for all elements.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenIndexOffsetLeavesInsufficientSpace_ShouldThrowException()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);
        var array = new int[4];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)buffer).CopyTo(array, 2);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> throws ArgumentException when the destination array element type is incompatible with the buffer element type.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsWrongType_ShouldThrowException()
    {
        var buffer = new CircularBuffer<int>(1);
        buffer.Enqueue(1);
        var wrongType = new string[2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)buffer).CopyTo(wrongType, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> succeeds and copies elements to the correct positions when the destination array is larger than required.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsLargerThanRequired_ShouldCopyWithoutThrowing()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);
        var array = new int[10];
        ((ICollection)buffer).CopyTo(array, 0);
        Assert.AreEqual(1, array[0]);
        Assert.AreEqual(2, array[1]);
        Assert.AreEqual(3, array[2]);
        Assert.AreEqual(0, array[9]);
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> does not modify the destination array when the buffer is empty.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenBufferIsEmpty_ShouldLeaveArrayUnchanged()
    {
        var buffer = new CircularBuffer<int>(3);
        var array = new[] { 9, 8, 7 };
        ((ICollection)buffer).CopyTo(array, 0);
        CollectionAssert.AreEqual(new[] { 9, 8, 7 }, array);
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> writes elements at the correct offset when a non-zero index is supplied.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenIndexIsNonZero_ShouldCopyToCorrectOffset()
    {
        var buffer = new CircularBuffer<int>(2);
        buffer.Enqueue(10);
        buffer.Enqueue(20);
        var array = new int[4];
        ((ICollection)buffer).CopyTo(array, 2);
        Assert.AreEqual(0, array[0]);
        Assert.AreEqual(0, array[1]);
        Assert.AreEqual(10, array[2]);
        Assert.AreEqual(20, array[3]);
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> preserves logical enqueue order when the internal buffer has wrapped around.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenBufferHasWrappedAround_ShouldCopyInEnqueueOrder()
    {
        var buffer = new CircularBuffer<int>(3);
        buffer.Enqueue(1);
        buffer.Enqueue(2);
        buffer.Enqueue(3);
        buffer.Dequeue();
        buffer.Enqueue(4);
        var array = new int[3];
        ((ICollection)buffer).CopyTo(array, 0);
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, array);
    }

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.CopyTo" /> accepts and correctly copies elements into an object array when the buffer element type is assignment-compatible.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsCompatibleObjectArray_ShouldCopySuccessfully()
    {
        var buffer = new CircularBuffer<string>(2);
        buffer.Enqueue("hello");
        buffer.Enqueue("world");
        var array = new object[2];
        ((ICollection)buffer).CopyTo(array, 0);
        Assert.AreEqual("hello", array[0]);
        Assert.AreEqual("world", array[1]);
    }
}
