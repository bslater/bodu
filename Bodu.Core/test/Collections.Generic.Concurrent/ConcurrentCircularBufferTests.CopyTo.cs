// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.CopyTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> succeeds and copies elements to the correct positions when the destination array is larger than required.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsLargerThanRequired_ShouldCopyWithoutThrowing()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        var array = new TestItem[10];

        buffer.CopyTo(array, 0);

        Assert.AreEqual(1, array[0].Value, "First element should be copied to index 0.");
        Assert.AreEqual(2, array[1].Value, "Second element should be copied to index 1.");
        Assert.AreEqual(3, array[2].Value, "Third element should be copied to index 2.");
        Assert.IsNull(array[9], "Trailing slots beyond the copied range should remain unmodified.");
    }
    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> preserves <see langword="null" /> entries in the destination array.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenBufferContainsNull_ShouldCopyNullValues()
    {
        var buffer = new ConcurrentCircularBuffer<string>(2);
        buffer.Enqueue(null);
        buffer.Enqueue("X");

        var array = new string[2];
        buffer.CopyTo(array, 0);

        Assert.IsNull(array[0]);
        Assert.AreEqual("X", array[1]);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> writes items to the destination in FIFO order and leaves trailing slots untouched.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenBufferHasElements_ShouldCopyElementsInFifoOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        var array = new TestItem[3];
        buffer.CopyTo(array, 0);

        Assert.AreEqual(1, array[0]?.Value);
        Assert.AreEqual(2, array[1]?.Value);
        Assert.IsNull(array[2]);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> on an empty buffer leaves the destination array unchanged.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenBufferIsEmpty_ShouldNotModifyDestination()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        var array = new TestItem[] { new(100), new(101), new(102), new(103) };

        buffer.CopyTo(array, 0);

        // No elements copied; destination unchanged
        CollectionAssert.AreEqual(new[] { 100, 101, 102, 103 }, array.Select(x => x.Value).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> never throws when a concurrent thread is calling <see cref="ConcurrentCircularBuffer{T}.Clear" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenConcurrentClear_ShouldNotThrow()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        for (var i = 0; i < 5; i++) buffer.Enqueue(new TestItem(i));

        var exceptions = new ConcurrentBag<Exception>();

        var clearer = Task.Run(() =>
        {
            for (var i = 0; i < 50; i++)
            {
                buffer.Clear();
                Thread.SpinWait(10);
            }
        });

        var copier = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                try
                {
                    var array = new TestItem[buffer.Capacity];
                    buffer.CopyTo(array, 0);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                Thread.SpinWait(5);
            }
        });

        Task.WaitAll(clearer, copier);

        Assert.IsEmpty(exceptions, "CopyTo threw during concurrent Clear.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> never throws under a concurrent producer and always writes into an array of the expected length.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenConcurrentEnqueue_ShouldNotThrowAndProduceWellSizedCopies()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(10);
        for (var i = 0; i < 5; i++) buffer.Enqueue(new TestItem(i));

        var exceptions = new ConcurrentBag<Exception>();
        var copies = new ConcurrentBag<TestItem[]>();

        var writer = Task.Run(() =>
        {
            for (var i = 5; i < 50; i++)
            {
                buffer.TryEnqueue(new TestItem(i));
                Thread.SpinWait(10);
            }
        });

        var copier = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                try
                {
                    var array = new TestItem[buffer.Capacity];
                    buffer.CopyTo(array, 0);
                    copies.Add(array);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                Thread.SpinWait(5);
            }
        });

        Task.WaitAll(writer, copier);

        Assert.IsEmpty(exceptions, "CopyTo threw an exception under concurrency.");
        Assert.IsTrue(copies.All(copy => copy.Length == buffer.Capacity));
    }

    /// <summary>
    /// Verifies that CopyTo performs a shallow copy, meaning that mutating the state of a copied reference-type
    /// element is reflected in the buffer, as both hold a reference to the same object.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCopiedReferenceTypeElementIsMutated_ShouldReflectInBuffer()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        var array = new TestItem[2];
        buffer.CopyTo(array, 0);

        // Mutate the object via the copied reference
        array[0].Value = 99;

        var verify = new TestItem[2];
        buffer.CopyTo(verify, 0);

        Assert.AreEqual(99, verify[0].Value,
            "CopyTo is a shallow copy — mutating a reference-type element via the destination array affects the same " +
            "object held in the buffer, as both reference the same instance.");
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> destination array to <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationArrayIsNull_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(MinCapacity);
        buffer.Enqueue(new TestItem(1));

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            buffer.CopyTo(null!, 0);
        });
    }

    /// <summary>
    /// Verifies that CopyTo produces an independent snapshot such that replacing an element in the destination
    /// array does not affect the contents of the buffer.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationElementIsReplaced_ShouldNotAffectBuffer()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        var array = new TestItem[3];
        buffer.CopyTo(array, 0);

        // Replace the element in the copied array
        array[0] = new TestItem(99);

        // Re-snapshot the buffer to confirm internal state is unchanged
        var verify = new TestItem[3];
        buffer.CopyTo(verify, 0);

        Assert.AreEqual(1, verify[0].Value,
            "Replacing array[0] should not affect the buffer's first element; CopyTo must produce an independent array.");
        Assert.AreEqual(2, verify[1].Value,
            "Buffer's second element should be unaffected by any modification to the destination array.");
        Assert.AreEqual(3, verify[2].Value,
            "Buffer's third element should be unaffected by any modification to the destination array.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> with a non-zero destination index writes elements starting at that offset and leaves earlier slots intact.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationHasNonZeroOffset_ShouldPlaceElementsAtIndex()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        var array = new TestItem[] { new(-1), new(-1), new(-1), new(-1), new(-1) };
        buffer.CopyTo(array, 2);

        // Expect: [-1, -1, 1, 2, 3]
        CollectionAssert.AreEqual(new[] { -1, -1, 1, 2, 3 }, array.Select(x => x?.Value ?? -1).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> accepts a destination index equal to the array length when the buffer is empty.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationIndexAtUpperBound_ShouldSucceedForZeroCount()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        var array = new TestItem[3];

        // Empty buffer => count==0, index==array.Length is valid (no copy)
        buffer.CopyTo(array, array.Length);

        // No exceptions and array remains default
        CollectionAssert.AreEqual(new TestItem[3], array);
    }

    /// <summary>
    /// Verifies that a destination index equal to the array length throws <see cref="ArgumentException" /> when the buffer is non-empty.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationIndexBeyondCopyRange_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));

        var array = new TestItem[3];

        // index == array.Length is invalid when count > 0 (insufficient space)
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            buffer.CopyTo(array, array.Length);
        });
    }

    /// <summary>
    /// Verifies that a negative destination index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationIndexIsNegative_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(1));

        var array = new TestItem[3];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            buffer.CopyTo(array, -1);
        });
    }

    /// <summary>
    /// Verifies that a destination array sized to match <see cref="ConcurrentCircularBuffer{T}.Count" /> exactly receives every element in FIFO order.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationIsExactFit_ShouldCopyAllElements()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(4);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        var array = new TestItem[3]; // exactly Count
        buffer.CopyTo(array, 0);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, array.Select(x => x.Value).ToArray());
    }

    /// <summary>
    /// Verifies that a destination array smaller than the buffer's current count throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationTooSmall_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        var array = new TestItem[1];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            buffer.CopyTo(array, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> throws ArgumentException when a valid mid-array index leaves insufficient trailing space for all elements.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexOffsetLeavesInsufficientSpace_ShouldThrowExactly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        var array = new TestItem[4]; // valid size, but offset of 2 leaves only 2 slots for 3 elements

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            buffer.CopyTo(array, 2);
        });
    }

    /// <summary>
    /// Verifies that after overwrites evict the oldest items, <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> writes only the most recent survivors in FIFO order.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenOverwriteEvictsItems_ShouldCopyNewestItemsOnly()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));
        buffer.Enqueue(new TestItem(4)); // evicts 1
        buffer.Enqueue(new TestItem(5)); // evicts 2

        var array = new TestItem[3];
        buffer.CopyTo(array, 0);

        CollectionAssert.AreEqual(new[] { 3, 4, 5 }, array.Select(x => x.Value).ToArray());
    }

    /// <summary>
    /// Verifies that after the buffer has wrapped, <see cref="ConcurrentCircularBuffer{T}.CopyTo" /> still writes elements in the logical FIFO order.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenWrapAroundOccurred_ShouldPreserveLogicalOrder()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        // Force wrap: dequeue one, enqueue another
        buffer.TryDequeue(out _);
        buffer.Enqueue(new TestItem(4));

        var array = new TestItem[3];
        buffer.CopyTo(array, 0);

        // FIFO after wrap is [2,3,4]
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, array.Select(x => x.Value).ToArray());
    }

}