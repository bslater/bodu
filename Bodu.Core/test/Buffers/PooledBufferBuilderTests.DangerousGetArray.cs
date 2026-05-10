// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.DangerousGetArray.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.DangerousGetArray"/> returns a segment whose count equals
    /// <see cref="PooledBufferBuilder{T}.WrittenCount"/> and whose contents match the written elements.
    /// </summary>
    [TestMethod]
    public void DangerousGetArray_WhenItemsBuffered_ShouldReturnSegmentMatchingWrittenRegion()
    {
        int[] expected = { 3, 6, 9 };
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(expected);

        System.ArraySegment<int> segment = builder.DangerousGetArray();

        Assert.AreEqual(builder.WrittenCount, segment.Count);
        CollectionAssert.AreEqual(expected, segment.ToArray());
    }

    /// <summary>
    /// Verifies that the array returned by <see cref="PooledBufferBuilder{T}.DangerousGetArray"/> is the same
    /// reference as the array returned by <see cref="PooledBufferBuilder{T}.AsArray"/>.
    /// </summary>
    [TestMethod]
    public void DangerousGetArray_WhenCalled_ShouldAliasInternalArray()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.Append(1);

        System.ArraySegment<int> segment = builder.DangerousGetArray();

        Assert.AreSame(builder.AsArray(), segment.Array);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.DangerousGetArray"/> on an empty builder returns a
    /// segment with count zero.
    /// </summary>
    [TestMethod]
    public void DangerousGetArray_WhenEmpty_ShouldReturnSegmentWithCountZero()
    {
        using var builder = new PooledBufferBuilder<int>();

        System.ArraySegment<int> segment = builder.DangerousGetArray();

        Assert.AreEqual(0, segment.Count);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.DangerousGetArray"/> throws
    /// <see cref="ObjectDisposedException"/> after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void DangerousGetArray_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.DangerousGetArray();
        });
    }
}
