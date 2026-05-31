// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.AppendRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.Collections.Generic.IEnumerable{T})"/>
    /// with an empty <see cref="System.Collections.Generic.List{T}"/> does not change
    /// <see cref="PooledBufferBuilder{T}.WrittenCount"/> (ICollection fast-path with count zero).
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenCollectionIsEmpty_ShouldNotChangeWrittenCount_UsingICollectionFastPath()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.Append(7);

        builder.AppendRange(new System.Collections.Generic.List<int>());

        Assert.AreEqual(1, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.AppendRange(System.ReadOnlySpan{T})"/> after
    /// disposal throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenDisposed_ShouldThrowExactly()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.AppendRange(new[] { 1, 2, 3 }.AsSpan());
        });
    }
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.Collections.Generic.IEnumerable{T})"/>
    /// with an <see cref="System.Collections.Generic.IEnumerable{T}"/> source appends all items correctly.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenEnumerableUsed_ShouldAppendAllItems_UsingIEnumerable()
    {
        var source = Enumerable.Range(1, 50).ToArray();
        using var builder = new PooledBufferBuilder<int>();

        builder.AppendRange((System.Collections.Generic.IEnumerable<int>)source);

        CollectionAssert.AreEqual(source, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.Collections.Generic.IEnumerable{T})"/>
    /// expands the buffer when the source exceeds the initial capacity.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenExceedsInitialSize_ShouldExpandBuffer_UsingIEnumerable()
    {
        var source = Enumerable.Range(1, 1000).ToArray();
        using var builder = new PooledBufferBuilder<int>();

        builder.AppendRange((System.Collections.Generic.IEnumerable<int>)source);

        Assert.AreEqual(1000, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.Collections.Generic.IEnumerable{T})"/>
    /// uses the <see cref="System.Collections.Generic.ICollection{T}"/> fast-path when the source is a
    /// <see cref="System.Collections.Generic.List{T}"/>, appending all items correctly.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenListSource_ShouldAppendAllItems_UsingICollectionFastPath()
    {
        var source = new System.Collections.Generic.List<int> { 7, 14, 21, 28 };
        using var builder = new PooledBufferBuilder<int>();

        builder.AppendRange(source);

        Assert.AreEqual(4, builder.WrittenCount);
        CollectionAssert.AreEqual(source, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.ReadOnlyMemory{T})"/> with an empty
    /// memory region does not change <see cref="PooledBufferBuilder{T}.WrittenCount"/>.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenMemoryIsEmpty_ShouldNotChangeWrittenCount_UsingReadOnlyMemory()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.Append(3);

        builder.AppendRange(System.ReadOnlyMemory<int>.Empty);

        Assert.AreEqual(1, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.ReadOnlyMemory{T})"/> appends all
    /// elements from the memory region in order.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenMemoryProvided_ShouldAppendAllItemsInOrder_UsingReadOnlyMemory()
    {
        int[] expected = [2, 4, 6, 8];
        using var builder = new PooledBufferBuilder<int>();

        builder.AppendRange(expected.AsMemory());

        CollectionAssert.AreEqual(expected, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.Collections.Generic.IEnumerable{T})"/>
    /// with an empty sequence does not change <see cref="PooledBufferBuilder{T}.WrittenCount"/>.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenSourceIsEmpty_ShouldNotChangeWrittenCount_UsingIEnumerable()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.Append(1);

        builder.AppendRange(System.Linq.Enumerable.Empty<int>());

        Assert.AreEqual(1, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.AppendRange(System.Collections.Generic.IEnumerable{T})"/>
    /// with a <see langword="null"/> source throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenSourceIsNull_ShouldThrowExactly()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            builder.AppendRange((System.Collections.Generic.IEnumerable<int>)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.ReadOnlySpan{T})"/> appends to
    /// existing elements rather than replacing them.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenSpanAppendedAfterExistingItems_ShouldCombineAllItems_UsingReadOnlySpan()
    {
        using var builder = new PooledBufferBuilder<int>(8);
        builder.Append(1);
        builder.Append(2);

        builder.AppendRange(new[] { 3, 4, 5 }.AsSpan());

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.ReadOnlySpan{T})"/> grows the buffer
    /// when the span is larger than the remaining capacity.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenSpanExceedsCapacity_ShouldGrowBufferAndRetainAllItems_UsingReadOnlySpan()
    {
        var expected = Enumerable.Range(0, 200).ToArray();
        using var builder = new PooledBufferBuilder<int>(4);

        builder.AppendRange(expected.AsSpan());

        Assert.AreEqual(200, builder.WrittenCount);
        CollectionAssert.AreEqual(expected, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.ReadOnlySpan{T})"/> with an empty span
    /// does not change <see cref="PooledBufferBuilder{T}.WrittenCount"/>.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenSpanIsEmpty_ShouldNotChangeWrittenCount_UsingReadOnlySpan()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.Append(5);

        builder.AppendRange([]);

        Assert.AreEqual(1, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.ReadOnlySpan{T})"/> appends all
    /// elements from the span in order.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenSpanProvided_ShouldAppendAllItemsInOrder_UsingReadOnlySpan()
    {
        int[] expected = [3, 6, 9, 12];
        using var builder = new PooledBufferBuilder<int>(2);

        builder.AppendRange(expected.AsSpan());

        CollectionAssert.AreEqual(expected, builder.WrittenSpan.ToArray());
    }

}
