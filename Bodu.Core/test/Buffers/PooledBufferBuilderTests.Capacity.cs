// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Capacity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Capacity"/> grows when elements are appended beyond the
    /// initial capacity. The append count is derived from the actual rented length so the test does not rely on a
    /// specific <see cref="ArrayPool{T}"/> bucket size.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenCapacityExceeded_ShouldIncreaseAfterGrowth()
    {
        using var builder = new PooledBufferBuilder<int>(2);
        var initialCapacity = builder.Capacity;

        for (var i = 0; i <= initialCapacity; i++)
            builder.Append(i);

        Assert.IsGreaterThan(
            initialCapacity, builder.Capacity,
            $"Expected capacity to grow beyond {initialCapacity}, but actual capacity was {builder.Capacity}.");

        Assert.IsGreaterThanOrEqualTo(
            builder.WrittenCount, builder.Capacity,
            $"Expected capacity {builder.Capacity} to be at least written count {builder.WrittenCount}.");
    }
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Capacity"/> is at least as large as the
    /// <c>initialCapacity</c> supplied to the constructor.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenConstructed_ShouldBeAtLeastInitialCapacity()
    {
        using var builder = new PooledBufferBuilder<int>(32);

        Assert.IsGreaterThanOrEqualTo(32, builder.Capacity);
    }

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.Capacity"/> after disposal throws
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.Capacity;
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Capacity"/> is not reduced by <see cref="PooledBufferBuilder{T}.Reset"/>.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenResetCalled_ShouldBeUnchanged()
    {
        using var builder = new PooledBufferBuilder<int>(16);
        builder.AppendRange(Enumerable.Range(1, 10));
        var capacityBeforeReset = builder.Capacity;

        builder.Reset();

        Assert.AreEqual(capacityBeforeReset, builder.Capacity);
    }

}
