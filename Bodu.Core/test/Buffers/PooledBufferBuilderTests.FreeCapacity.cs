// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.FreeCapacity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.FreeCapacity"/> is reduced by the amount passed to
    /// <see cref="PooledBufferBuilder{T}.Advance"/>.
    /// </summary>
    [TestMethod]
    public void FreeCapacity_WhenAdvanceCalled_ShouldDecreaseByAdvanceAmount()
    {
        using var builder = new PooledBufferBuilder<int>(16);
        var freeCapacityBefore = builder.FreeCapacity;
        _ = builder.GetSpan(4);

        builder.Advance(4);

        Assert.AreEqual(freeCapacityBefore - 4, builder.FreeCapacity);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.FreeCapacity"/> throws <see cref="ObjectDisposedException"/>
    /// after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void FreeCapacity_WhenDisposed_ShouldThrowExactly()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.FreeCapacity;
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.FreeCapacity"/> equals <see cref="PooledBufferBuilder{T}.Capacity"/>
    /// when the builder is empty.
    /// </summary>
    [TestMethod]
    public void FreeCapacity_WhenEmpty_ShouldEqualCapacity()
    {
        using var builder = new PooledBufferBuilder<int>(16);

        Assert.AreEqual(builder.Capacity, builder.FreeCapacity);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.FreeCapacity"/> is not changed by calling
    /// <see cref="PooledBufferBuilder{T}.GetSpan"/> without a subsequent <see cref="PooledBufferBuilder{T}.Advance"/>.
    /// </summary>
    [TestMethod]
    public void FreeCapacity_WhenGetSpanCalledWithoutAdvance_ShouldRemainUnchanged()
    {
        using var builder = new PooledBufferBuilder<int>(16);
        builder.Append(1);
        var freeCapacityBefore = builder.FreeCapacity;

        _ = builder.GetSpan(4); // does not commit data

        Assert.AreEqual(freeCapacityBefore, builder.FreeCapacity);
    }
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.FreeCapacity"/> equals
    /// <see cref="PooledBufferBuilder{T}.Capacity"/> minus <see cref="PooledBufferBuilder{T}.WrittenCount"/>.
    /// </summary>
    [TestMethod]
    public void FreeCapacity_WhenItemsBuffered_ShouldEqualCapacityMinusWrittenCount()
    {
        using var builder = new PooledBufferBuilder<int>(32);
        builder.AppendRange([1, 2, 3, 4]);

        Assert.AreEqual(builder.Capacity - builder.WrittenCount, builder.FreeCapacity);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.FreeCapacity"/> is restored to
    /// <see cref="PooledBufferBuilder{T}.Capacity"/> after <see cref="PooledBufferBuilder{T}.Reset"/>.
    /// </summary>
    [TestMethod]
    public void FreeCapacity_WhenResetCalled_ShouldEqualCapacity()
    {
        using var builder = new PooledBufferBuilder<int>(16);
        builder.AppendRange(Enumerable.Range(1, 8));
        builder.Reset();

        Assert.AreEqual(builder.Capacity, builder.FreeCapacity);
    }

}
