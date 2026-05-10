// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.AppendRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.Collections.Generic.IEnumerable{T})"/>
    /// with an <see cref="System.Collections.Generic.IEnumerable{T}"/> source appends all items correctly.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenEnumerableUsed_ShouldAppendAllItems_UsingIEnumerable()
    {
        int[] source = Enumerable.Range(1, 50).ToArray();
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
        int[] source = Enumerable.Range(1, 1000).ToArray();
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
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.AppendRange(System.Collections.Generic.IEnumerable{T})"/>
    /// with a <see langword="null"/> source throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenSourceIsNull_ShouldThrowArgumentNullException_UsingIEnumerable()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            builder.AppendRange((System.Collections.Generic.IEnumerable<int>)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.ReadOnlySpan{T})"/> appends all
    /// elements from the span in order.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenSpanProvided_ShouldAppendAllItemsInOrder_UsingReadOnlySpan()
    {
        int[] expected = { 3, 6, 9, 12 };
        using var builder = new PooledBufferBuilder<int>(2);

        builder.AppendRange(expected.AsSpan());

        CollectionAssert.AreEqual(expected, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AppendRange(System.ReadOnlySpan{T})"/> grows the buffer
    /// when the span is larger than the remaining capacity.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenSpanExceedsCapacity_ShouldGrowBufferAndRetainAllItems_UsingReadOnlySpan()
    {
        int[] expected = Enumerable.Range(0, 200).ToArray();
        using var builder = new PooledBufferBuilder<int>(4);

        builder.AppendRange(expected.AsSpan());

        Assert.AreEqual(200, builder.WrittenCount);
        CollectionAssert.AreEqual(expected, builder.WrittenSpan.ToArray());
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
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.AppendRange(System.ReadOnlySpan{T})"/> after
    /// disposal throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void AppendRange_WhenDisposed_ShouldThrowObjectDisposedException_UsingReadOnlySpan()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.AppendRange(new[] { 1, 2, 3 }.AsSpan());
        });
    }
}
