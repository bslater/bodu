// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.WrittenSpan.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.WrittenSpan"/> on an empty builder returns a span with
    /// length zero.
    /// </summary>
    [TestMethod]
    public void WrittenSpan_WhenEmpty_ShouldReturnEmptySpan()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.AreEqual(0, builder.WrittenSpan.Length);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.WrittenSpan"/> returns a span whose length equals
    /// <see cref="PooledBufferBuilder{T}.WrittenCount"/> and whose contents match the appended elements.
    /// </summary>
    [TestMethod]
    public void WrittenSpan_WhenItemsBuffered_ShouldReturnSpanMatchingCountAndContents()
    {
        int[] expected = [10, 20, 30];
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(expected);

        System.ReadOnlySpan<int> span = builder.WrittenSpan;

        Assert.AreEqual(builder.WrittenCount, span.Length);
        CollectionAssert.AreEqual(expected, span.ToArray());
    }

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.WrittenSpan"/> after disposal throws
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void WrittenSpan_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.WrittenSpan;
        });
    }
}
