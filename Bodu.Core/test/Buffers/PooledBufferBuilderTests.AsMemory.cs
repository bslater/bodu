// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.AsMemory.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AsMemory"/> on an empty builder returns a memory region
    /// with length zero.
    /// </summary>
    [TestMethod]
    public void AsMemory_WhenEmpty_ShouldReturnEmptyMemory()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.AreEqual(0, builder.AsMemory().Length);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AsMemory"/> returns a region whose length equals
    /// <see cref="PooledBufferBuilder{T}.Count"/> and whose contents match the appended elements.
    /// </summary>
    [TestMethod]
    public void AsMemory_WhenItemsBuffered_ShouldReturnMemoryMatchingCountAndContents()
    {
        int[] expected = { 5, 10, 15 };
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(expected);

        System.Memory<int> memory = builder.AsMemory();

        Assert.AreEqual(builder.Count, memory.Length);
        CollectionAssert.AreEqual(expected, memory.ToArray());
    }

    /// <summary>
    /// Verifies that the memory returned by <see cref="PooledBufferBuilder{T}.AsMemory"/> contains the same
    /// elements as the span returned by <see cref="PooledBufferBuilder{T}.AsSpan"/>.
    /// </summary>
    [TestMethod]
    public void AsMemory_WhenItemsBuffered_ShouldMatchAsSpanContents()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(Enumerable.Range(1, 8));

        CollectionAssert.AreEqual(builder.AsSpan().ToArray(), builder.AsMemory().ToArray());
    }

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.AsMemory"/> after disposal throws
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void AsMemory_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.AsMemory();
        });
    }
}
