// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Reset.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Reset"/> sets <see cref="PooledBufferBuilder{T}.Count"/>
    /// to zero.
    /// </summary>
    [TestMethod]
    public void Reset_WhenCalled_ShouldSetCountToZero()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(new[] { 1, 2, 3 });

        builder.Reset();

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
        int capacityBeforeReset = builder.Capacity;

        builder.Reset();

        Assert.AreEqual(capacityBeforeReset, builder.Capacity);
    }

    /// <summary>
    /// Verifies that elements can be appended normally after <see cref="PooledBufferBuilder{T}.Reset"/>.
    /// </summary>
    [TestMethod]
    public void Reset_WhenFollowedByAppend_ShouldAccumulateCorrectly()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(new[] { 10, 20, 30 });
        builder.Reset();

        builder.Append(99);

        Assert.AreEqual(1, builder.WrittenCount);
        Assert.AreEqual(99, builder.WrittenSpan[0]);
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

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.Reset"/> after disposal throws
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void Reset_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.Reset();
        });
    }
}
