// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.AppendRange.NonCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(IEnumerable{T})" /> grows the internal buffer when the lazy iterator
    /// yields more elements than fit in the initial capacity.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenLazyIteratorExceedsInitialCapacity_ShouldGrowBuffer()
    {
        using var builder = new PooledBufferBuilder<int>();

        builder.AppendRange(YieldRange(2000));

        Assert.AreEqual(2000, builder.WrittenCount);
        Assert.AreEqual(1, builder.WrittenSpan[0]);
        Assert.AreEqual(2000, builder.WrittenSpan[1999]);
    }

    /// <summary>
    /// Verifies that appending an empty lazy iterator leaves the buffer empty.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenLazyIteratorIsEmpty_ShouldLeaveBufferEmpty()
    {
        using var builder = new PooledBufferBuilder<int>();

        builder.AppendRange(YieldRange(0));

        Assert.AreEqual(0, builder.WrittenCount);
    }
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(IEnumerable{T})" /> appends every yielded element when the source is
    /// a yield-based iterator that does not implement <see cref="ICollection{T}" />, exercising the foreach fallback path rather than
    /// the bulk-copy fast path.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenSourceIsLazyIterator_ShouldAppendEveryYieldedElement()
    {
        using var builder = new PooledBufferBuilder<int>();

        builder.AppendRange(YieldRange(5));

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, builder.WrittenSpan.ToArray());
    }

    private static IEnumerable<int> YieldRange(int count)
    {
        for (var i = 1; i <= count; i++)
            yield return i;
    }

}
