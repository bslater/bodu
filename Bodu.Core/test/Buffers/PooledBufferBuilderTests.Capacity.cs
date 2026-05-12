// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Capacity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Capacity"/> is at least as large as the
    /// <c>initialCapacity</c> supplied to the constructor.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenConstructed_ShouldBeAtLeastInitialCapacity()
    {
        using var builder = new PooledBufferBuilder<int>(32);

        Assert.IsTrue(builder.Capacity >= 32);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Capacity"/> grows when elements are appended beyond the
    /// initial capacity. The append count is derived from the actual rented length so the test does not rely on a
    /// specific <see cref="ArrayPool{T}"/> bucket size.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenCapacityExceeded_ShouldIncreaseAfterGrowth()
    {
        using var builder = new PooledBufferBuilder<int>(2);
        int initialCapacity = builder.Capacity;

        // ArrayPool may round up the rented buffer beyond the requested capacity, so append one element more
        // than the actual capacity to guarantee a growth event.
        for (int i = 0; i <= initialCapacity; i++)
            builder.Append(i);

        Assert.IsTrue(builder.Capacity > initialCapacity);
        Assert.IsTrue(builder.Capacity >= builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Capacity"/> is not reduced by <see cref="PooledBufferBuilder{T}.Reset"/>.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenResetCalled_ShouldBeUnchanged()
    {
        using var builder = new PooledBufferBuilder<int>(16);
        builder.AppendRange(Enumerable.Range(1, 10));
        int capacityBeforeReset = builder.Capacity;

        builder.Reset();

        Assert.AreEqual(capacityBeforeReset, builder.Capacity);
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
}
