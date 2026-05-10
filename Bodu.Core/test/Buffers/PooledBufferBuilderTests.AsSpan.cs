// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.AsSpan.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AsSpan"/> on an empty builder returns a span with length
    /// zero.
    /// </summary>
    [TestMethod]
    public void AsSpan_WhenEmpty_ShouldReturnEmptySpan()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.AreEqual(0, builder.AsSpan().Length);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AsSpan"/> returns a span whose length equals
    /// <see cref="PooledBufferBuilder{T}.Count"/> and whose contents match the appended elements.
    /// </summary>
    [TestMethod]
    public void AsSpan_WhenItemsBuffered_ShouldReturnSpanMatchingCountAndContents()
    {
        int[] expected = { 10, 20, 30 };
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(expected);

        System.Span<int> span = builder.AsSpan();

        Assert.AreEqual(builder.Count, span.Length);
        CollectionAssert.AreEqual(expected, span.ToArray());
    }

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.AsSpan"/> after disposal throws
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void AsSpan_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.AsSpan();
        });
    }
}
