// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.EnsureCapacity.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.EnsureCapacity"/> does not change
    /// <see cref="PooledBufferBuilder{T}.Capacity"/> when the current capacity already satisfies the request.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenRequestedCapacityAlreadySatisfied_ShouldNotGrow()
    {
        using var builder = new PooledBufferBuilder<int>(32);
        int capacityBefore = builder.Capacity;

        builder.EnsureCapacity(8);

        Assert.AreEqual(capacityBefore, builder.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.EnsureCapacity"/> grows the buffer to at least the
    /// requested minimum when the current capacity is insufficient.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenRequestedCapacityExceedsCurrent_ShouldGrowToAtLeastMinimum()
    {
        using var builder = new PooledBufferBuilder<int>(4);

        builder.EnsureCapacity(200);

        Assert.IsTrue(builder.Capacity >= 200);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.EnsureCapacity"/> does not change
    /// <see cref="PooledBufferBuilder{T}.WrittenCount"/>.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenCalled_ShouldPreserveWrittenCount()
    {
        using var builder = new PooledBufferBuilder<int>(4);
        builder.AppendRange(new[] { 1, 2, 3 });

        builder.EnsureCapacity(128);

        Assert.AreEqual(3, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.EnsureCapacity"/> throws
    /// <see cref="ArgumentOutOfRangeException"/> when the requested minimum is negative.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenMinimumIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            builder.EnsureCapacity(-1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.EnsureCapacity"/> throws
    /// <see cref="ObjectDisposedException"/> after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.EnsureCapacity(16);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.EnsureCapacity"/> with a minimum of zero succeeds without
    /// changing <see cref="PooledBufferBuilder{T}.Capacity"/>.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenMinimumIsZero_ShouldSucceedWithoutChangingCapacity()
    {
        using var builder = new PooledBufferBuilder<int>(16);
        int capacityBefore = builder.Capacity;

        builder.EnsureCapacity(0);

        Assert.AreEqual(capacityBefore, builder.Capacity);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.EnsureCapacity"/> when the minimum exactly equals the
    /// current capacity does not trigger a growth.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenMinimumEqualsCurrentCapacity_ShouldNotGrow()
    {
        using var builder = new PooledBufferBuilder<int>(16);
        int currentCapacity = builder.Capacity;

        builder.EnsureCapacity(currentCapacity);

        Assert.AreEqual(currentCapacity, builder.Capacity);
    }

    /// <summary>
    /// Verifies that existing written elements are preserved after <see cref="PooledBufferBuilder{T}.EnsureCapacity"/>
    /// triggers a buffer growth.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenGrowthOccurs_ShouldPreserveExistingContents()
    {
        int[] expected = { 10, 20, 30 };
        using var builder = new PooledBufferBuilder<int>(4);
        builder.AppendRange(expected);

        builder.EnsureCapacity(512);

        CollectionAssert.AreEqual(expected, builder.WrittenSpan.ToArray());
    }
}
