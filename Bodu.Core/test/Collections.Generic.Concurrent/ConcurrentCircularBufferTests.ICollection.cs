// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.ICollection.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> accepts and correctly copies elements into an object array when the buffer element type is assignment-compatible.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsCompatibleObjectArray_ShouldCopySuccessfully()
    {
        var buffer = new ConcurrentCircularBuffer<string>(2);
        buffer.Enqueue("hello");
        buffer.Enqueue("world");
        var array = new object[2];
        ((ICollection)buffer).CopyTo(array, 0);
        Assert.AreEqual("hello", array[0],
            "First element should be \"hello\" when copied into a compatible object array.");
        Assert.AreEqual("world", array[1],
            "Second element should be \"world\" when copied into a compatible object array.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> succeeds and copies elements to the correct positions when the destination array is larger than required.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsLargerThanRequired_ShouldCopyWithoutThrowing()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        var array = new TestItem[10];
        ((ICollection)buffer).CopyTo(array, 0);
        Assert.AreEqual(1, array[0]?.Value,
            "First element should have Value 1 at index 0.");
        Assert.AreEqual(2, array[1]?.Value,
            "Second element should have Value 2 at index 1.");
        Assert.AreEqual(3, array[2]?.Value,
            "Third element should have Value 3 at index 2.");
        Assert.IsNull(array[9],
            "Trailing slots beyond the copied range should remain null.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> throws ArgumentException when the destination is a multidimensional array.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsMultiDimensional_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(1));
        var multiDim = new TestItem[2, 2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)buffer).CopyTo(multiDim, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> throws ArgumentException when the destination array has a non-zero lower bound.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsNotZeroBased_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(1));
        var nonZeroBased = Array.CreateInstance(typeof(TestItem), lengths: [4], lowerBounds: [1]);
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)buffer).CopyTo(nonZeroBased, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> throws ArgumentNullException when the destination array is null.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(1));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ((ICollection)buffer).CopyTo(null, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> throws ArgumentException when the destination array is too small to hold all elements.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsTooSmall_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        var array = new TestItem[1];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)buffer).CopyTo(array, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> throws ArgumentException when the destination array element type is incompatible with the buffer element type.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsWrongType_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(1));
        var wrongType = new string[2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)buffer).CopyTo(wrongType, 0);
        });
    }
    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> correctly copies all elements to the destination array starting at index zero.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenBufferHasElements_ShouldCopyElementsToArray()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        var target = new TestItem[3];
        ((ICollection)buffer).CopyTo(target, 0);
        Assert.AreEqual(1, target[0]?.Value,
            "First element should have Value 1 after copying from the buffer.");
        Assert.AreEqual(2, target[1]?.Value,
            "Second element should have Value 2 after copying from the buffer.");
        Assert.IsNull(target[2],
            "Third slot should remain null as only two elements were enqueued.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> preserves logical enqueue order when the internal buffer has wrapped around.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenBufferHasWrappedAround_ShouldCopyInEnqueueOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        buffer.TryDequeue(out _);
        buffer.Enqueue(new TestItem(4));
        var array = new TestItem[3];
        ((ICollection)buffer).CopyTo(array, 0);
        Assert.AreEqual(2, array[0]?.Value,
            "First element should be 2 — the oldest remaining item after the dequeue.");
        Assert.AreEqual(3, array[1]?.Value,
            "Second element should be 3, preserving FIFO order across the wrap boundary.");
        Assert.AreEqual(4, array[2]?.Value,
            "Third element should be 4 — the item enqueued after the wrap.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> does not modify the destination array when the buffer is empty.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenBufferIsEmpty_ShouldLeaveArrayUnchanged()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        var sentinel1 = new TestItem(9);
        var sentinel2 = new TestItem(8);
        var sentinel3 = new TestItem(7);
        TestItem[] array = [sentinel1, sentinel2, sentinel3];
        ((ICollection)buffer).CopyTo(array, 0);
        Assert.AreSame(sentinel1, array[0],
            "Index 0 should still reference the original sentinel item when the buffer is empty.");
        Assert.AreSame(sentinel2, array[1],
            "Index 1 should still reference the original sentinel item when the buffer is empty.");
        Assert.AreSame(sentinel3, array[2],
            "Index 2 should still reference the original sentinel item when the buffer is empty.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> throws ArgumentException when the target index equals the array length, leaving no addressable position.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenIndexEqualsArrayLength_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        var array = new TestItem[6];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)buffer).CopyTo(array, 6);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> throws ArgumentOutOfRangeException when the target index is negative.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenIndexIsNegative_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(1));
        var array = new TestItem[3];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ((ICollection)buffer).CopyTo(array, -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> writes elements at the correct offset when a non-zero index is supplied.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenIndexIsNonZero_ShouldCopyToCorrectOffset()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(10));
        buffer.Enqueue(new TestItem(20));
        var array = new TestItem[4];
        ((ICollection)buffer).CopyTo(array, 2);
        Assert.IsNull(array[0],
            "Index 0 should remain null as copying started at index 2.");
        Assert.IsNull(array[1],
            "Index 1 should remain null as copying started at index 2.");
        Assert.AreEqual(10, array[2]?.Value,
            "First buffer element should appear at index 2.");
        Assert.AreEqual(20, array[3]?.Value,
            "Second buffer element should appear at index 3.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> throws ArgumentException when the target index offset leaves insufficient space for all elements.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenIndexOffsetLeavesInsufficientSpace_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        var array = new TestItem[4];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)buffer).CopyTo(array, 2);
        });
    }

}