// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.WrittenMemory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.WrittenMemory"/> after disposal throws
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void WrittenMemory_WhenDisposed_ShouldThrowExactly()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.WrittenMemory;
        });
    }
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.WrittenMemory"/> on an empty builder returns a memory
    /// region with length zero.
    /// </summary>
    [TestMethod]
    public void WrittenMemory_WhenEmpty_ShouldReturnEmptyMemory()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.AreEqual(0, builder.WrittenMemory.Length);
    }

    /// <summary>
    /// Verifies that the memory returned by <see cref="PooledBufferBuilder{T}.WrittenMemory"/> contains the same
    /// elements as the span returned by <see cref="PooledBufferBuilder{T}.WrittenSpan"/>.
    /// </summary>
    [TestMethod]
    public void WrittenMemory_WhenItemsBuffered_ShouldMatchWrittenSpanContents()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(Enumerable.Range(1, 8));

        CollectionAssert.AreEqual(builder.WrittenSpan.ToArray(), builder.WrittenMemory.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.WrittenMemory"/> returns a region whose length equals
    /// <see cref="PooledBufferBuilder{T}.WrittenCount"/> and whose contents match the appended elements.
    /// </summary>
    [TestMethod]
    public void WrittenMemory_WhenItemsBuffered_ShouldReturnMemoryMatchingCountAndContents()
    {
        int[] expected = [5, 10, 15];
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(expected.AsSpan());

        System.ReadOnlyMemory<int> memory = builder.WrittenMemory;

        Assert.AreEqual(builder.WrittenCount, memory.Length);
        CollectionAssert.AreEqual(expected, memory.ToArray());
    }

}
