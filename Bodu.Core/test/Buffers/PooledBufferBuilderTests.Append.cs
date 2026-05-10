// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Append.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that appending a single item increments <see cref="PooledBufferBuilder{T}.Count"/> by one and
    /// makes the item visible via <see cref="PooledBufferBuilder{T}.AsSpan"/>.
    /// </summary>
    [TestMethod]
    public void Append_WhenItemAppended_ShouldIncrementCountAndBeVisibleInSpan()
    {
        using var builder = new PooledBufferBuilder<int>(4);

        builder.Append(42);

        Assert.AreEqual(1, builder.Count);
        Assert.AreEqual(42, builder.AsSpan()[0]);
    }

    /// <summary>
    /// Verifies that appending multiple items to a <see cref="PooledBufferBuilder{T}"/> produces a span that
    /// contains all items in insertion order.
    /// </summary>
    [TestMethod]
    public void Append_WhenMultipleItemsAppended_ShouldPreserveInsertionOrder()
    {
        using var builder = new PooledBufferBuilder<int>(4);

        builder.Append(1);
        builder.Append(2);
        builder.Append(3);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, builder.AsSpan().ToArray());
    }

    /// <summary>
    /// Verifies that appending items beyond the initial capacity causes the buffer to grow transparently and all
    /// items remain accessible.
    /// </summary>
    [TestMethod]
    public void Append_WhenCapacityExceeded_ShouldGrowBufferAndRetainAllItems()
    {
        using var builder = new PooledBufferBuilder<int>(2);

        for (int i = 0; i < 10; i++)
            builder.Append(i);

        Assert.AreEqual(10, builder.Count);
        CollectionAssert.AreEqual(Enumerable.Range(0, 10).ToArray(), builder.AsSpan().ToArray());
    }

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.Append"/> after disposal throws
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void Append_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.Append(1);
        });
    }
}
