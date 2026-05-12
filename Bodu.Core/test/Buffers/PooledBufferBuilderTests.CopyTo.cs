// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.CopyTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.CopyTo"/> copies all written elements to the destination
    /// span in order.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationIsLargeEnough_ShouldCopyAllWrittenElements()
    {
        int[] expected = [10, 20, 30, 40];
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(expected);

        var destination = new int[expected.Length];
        builder.CopyTo(destination);

        CollectionAssert.AreEqual(expected, destination);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.CopyTo"/> does not modify the builder's state.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCalled_ShouldNotChangeWrittenCountOrContents()
    {
        int[] items = [1, 2, 3];
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(items);

        builder.CopyTo(new int[items.Length]);

        Assert.AreEqual(items.Length, builder.WrittenCount);
        CollectionAssert.AreEqual(items, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.CopyTo"/> throws <see cref="ArgumentException"/> when
    /// the destination span is too small.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationIsTooSmall_ShouldThrowArgumentException()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            builder.CopyTo(new int[2]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.CopyTo"/> throws <see cref="ObjectDisposedException"/>
    /// after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.CopyTo(new int[4]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.CopyTo"/> on an empty builder into an empty span succeeds
    /// without throwing.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenBuilderIsEmpty_ShouldSucceedWithEmptyDestination()
    {
        using var builder = new PooledBufferBuilder<int>();

        builder.CopyTo([]); // should not throw
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.CopyTo"/> into a destination larger than
    /// <see cref="PooledBufferBuilder{T}.WrittenCount"/> copies only the written elements and leaves the
    /// remaining destination slots untouched.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationIsLargerThanWrittenCount_ShouldCopyOnlyWrittenElements()
    {
        int[] expected = [1, 2, 3];
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(expected);

        var destination = new int[10]; // larger than WrittenCount
        builder.CopyTo(destination);

        CollectionAssert.AreEqual(expected, destination.Take(builder.WrittenCount).ToArray());
        // remaining slots stay at default value
        Assert.AreEqual(0, destination[3]);
    }
}
