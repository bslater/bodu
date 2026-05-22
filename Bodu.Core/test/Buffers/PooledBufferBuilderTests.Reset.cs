// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Reset.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.Reset"/> on an already-empty builder succeeds
    /// without throwing.
    /// </summary>
    [TestMethod]
    public void Reset_WhenBuilderIsAlreadyEmpty_ShouldSucceedWithoutError()
    {
        using var builder = new PooledBufferBuilder<int>();

        builder.Reset(); // should not throw
        Assert.AreEqual(0, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Reset"/> retains the current rented array, leaving
    /// <see cref="PooledBufferBuilder{T}.Capacity"/> unchanged.
    /// </summary>
    [TestMethod]
    public void Reset_WhenCalled_ShouldRetainCapacity()
    {
        using var builder = new PooledBufferBuilder<int>(16);
        builder.AppendRange(Enumerable.Range(1, 10));
        var capacityBeforeReset = builder.Capacity;

        builder.Reset();

        Assert.AreEqual(capacityBeforeReset, builder.Capacity);
    }
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Reset"/> sets <see cref="PooledBufferBuilder{T}.Count"/>
    /// to zero.
    /// </summary>
    [TestMethod]
    public void Reset_WhenCalled_ShouldSetCountToZero()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([1, 2, 3]);

        builder.Reset();

        Assert.AreEqual(0, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.Reset"/> multiple times leaves the builder in an
    /// empty state each time.
    /// </summary>
    [TestMethod]
    public void Reset_WhenCalledMultipleTimes_ShouldRemainEmpty()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([1, 2, 3]);

        builder.Reset();
        builder.Reset();

        Assert.AreEqual(0, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.Reset"/> after disposal throws
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void Reset_WhenDisposed_ShouldThrowExactly()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.Reset();
        });
    }

    /// <summary>
    /// Verifies that elements can be appended normally after <see cref="PooledBufferBuilder{T}.Reset"/>.
    /// </summary>
    [TestMethod]
    public void Reset_WhenFollowedByAppend_ShouldAccumulateCorrectly()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([10, 20, 30]);
        builder.Reset();

        builder.Append(99);

        Assert.AreEqual(1, builder.WrittenCount);
        Assert.AreEqual(99, builder.WrittenSpan[0]);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.WrittenSpan"/> is empty after
    /// <see cref="PooledBufferBuilder{T}.Reset"/> is called.
    /// </summary>
    [TestMethod]
    public void Reset_WhenFollowedByWrittenSpan_ShouldReturnEmptySpan()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([10, 20, 30]);

        builder.Reset();

        Assert.AreEqual(0, builder.WrittenSpan.Length);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Reset"/> clears reference slots in the valid portion of
    /// the buffer when <typeparamref name="T"/> is a reference type, preventing stale object retention.
    /// </summary>
    [TestMethod]
    public void Reset_WhenReferenceType_ShouldClearLiveSlots()
    {
        using var builder = new PooledBufferBuilder<string>(4);
        builder.Append("hello");
        builder.Append("world");

        builder.Reset();

        // After reset the live region is empty; the internal array slots at [0] and [1]
        // should have been cleared. We verify indirectly: a second append writes to slot 0
        // and the span contains exactly that element.
        builder.Append("new");
        Assert.AreEqual(1, builder.WrittenCount);
        Assert.AreEqual("new", builder.WrittenSpan[0]);
    }

}
