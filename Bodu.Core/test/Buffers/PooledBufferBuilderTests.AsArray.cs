// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.AsArray.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AsArray"/> returns the underlying array whose first
    /// <see cref="PooledBufferBuilder{T}.WrittenCount"/> elements match those visible via
    /// <see cref="PooledBufferBuilder{T}.WrittenSpan"/>.
    /// </summary>
    [TestMethod]
    public void AsArray_WhenAccessedAfterAppend_ShouldMatchWrittenSpanContents()
    {
        var source = new[] { 10, 20, 30 };
        using var builder = new PooledBufferBuilder<int>();

        builder.AppendRange(source);
        System.ReadOnlySpan<int> span = builder.WrittenSpan;
        int[] array = builder.AsArray();

        CollectionAssert.AreEqual(span.ToArray(), array.Take(span.Length).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AsArray"/> on an empty builder returns a non-null array.
    /// </summary>
    [TestMethod]
    public void AsArray_WhenEmpty_ShouldReturnNonNullArray()
    {
        using var builder = new PooledBufferBuilder<int>();

        int[] array = builder.AsArray();

        Assert.IsNotNull(array);
    }

    /// <summary>
    /// Verifies that mutating the array returned by <see cref="PooledBufferBuilder{T}.AsArray"/> is reflected in
    /// subsequent reads of <see cref="PooledBufferBuilder{T}.WrittenSpan"/>, confirming it aliases internal state.
    /// </summary>
    [TestMethod]
    public void AsArray_WhenMutated_ShouldBeReflectedInWrittenSpan()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.Append(1);

        int[] array = builder.AsArray();
        array[0] = 99;

        Assert.AreEqual(99, builder.WrittenSpan[0]);
    }

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.AsArray"/> after disposal throws
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void AsArray_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.AsArray();
        });
    }
}
