// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.IBufferWriter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    // -----------------------------------------------------------------------
    // Advance
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Advance"/> increments <see cref="PooledBufferBuilder{T}.WrittenCount"/>
    /// by the specified amount.
    /// </summary>
    [TestMethod]
    public void Advance_WhenCalled_ShouldIncrementWrittenCount()
    {
        using var builder = new PooledBufferBuilder<int>(16);
        _ = builder.GetSpan(4);

        builder.Advance(4);

        Assert.AreEqual(4, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Advance"/> throws <see cref="ArgumentOutOfRangeException"/>
    /// when the count would exceed the current buffer length.
    /// </summary>
    [TestMethod]
    public void Advance_WhenCountExceedsBufferLength_ShouldThrowExactly()
    {
        using var builder = new PooledBufferBuilder<int>(4);
        _ = builder.GetSpan(2);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            builder.Advance(builder.Capacity + 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Advance"/> throws <see cref="ArgumentOutOfRangeException"/>
    /// when the count is negative.
    /// </summary>
    [TestMethod]
    public void Advance_WhenCountIsNegative_ShouldThrowExactly()
    {
        using var builder = new PooledBufferBuilder<int>(8);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            builder.Advance(-1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Advance"/> with zero does not change
    /// <see cref="PooledBufferBuilder{T}.WrittenCount"/>.
    /// </summary>
    [TestMethod]
    public void Advance_WhenCountIsZero_ShouldNotChangeWrittenCount()
    {
        using var builder = new PooledBufferBuilder<int>(8);
        builder.Append(1);

        builder.Advance(0);

        Assert.AreEqual(1, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Advance"/> throws <see cref="ObjectDisposedException"/>
    /// after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void Advance_WhenDisposed_ShouldThrowExactly()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.Advance(0);
        });
    }

    // -----------------------------------------------------------------------
    // GetMemory
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that writing into the memory returned by <see cref="PooledBufferBuilder{T}.GetMemory"/> and then
    /// calling <see cref="PooledBufferBuilder{T}.Advance"/> commits the data correctly.
    /// </summary>
    [TestMethod]
    public void GetMemory_WhenDataWrittenAndAdvanceCalled_ShouldExposeDataViaWrittenMemory()
    {
        using var builder = new PooledBufferBuilder<int>(8);
        System.Memory<int> memory = builder.GetMemory(3);
        memory.Span[0] = 7;
        memory.Span[1] = 14;
        memory.Span[2] = 21;
        builder.Advance(3);

        CollectionAssert.AreEqual(new[] { 7, 14, 21 }, builder.WrittenMemory.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.GetMemory"/> throws <see cref="ObjectDisposedException"/>
    /// after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void GetMemory_WhenDisposed_ShouldThrowExactly()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.GetMemory();
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.GetMemory"/> with a negative size hint throws
    /// <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [TestMethod]
    public void GetMemory_WhenSizeHintIsNegative_ShouldThrowExactly()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.GetMemory(-1);
        });
    }

    /// <summary>
    /// Verifies that a second call to <see cref="PooledBufferBuilder{T}.GetSpan"/> after
    /// <see cref="PooledBufferBuilder{T}.Advance"/> starts at the correct position.
    /// </summary>
    [TestMethod]
    public void GetSpan_WhenCalledAfterAdvance_ShouldStartAtCorrectOffset()
    {
        using var builder = new PooledBufferBuilder<int>(16);

        System.Span<int> first = builder.GetSpan(3);
        first[0] = 1;
        first[1] = 2;
        first[2] = 3;
        builder.Advance(3);

        System.Span<int> second = builder.GetSpan(2);
        second[0] = 4;
        second[1] = 5;
        builder.Advance(2);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.GetSpan"/> without a subsequent
    /// <see cref="PooledBufferBuilder{T}.Advance"/> does not change <see cref="PooledBufferBuilder{T}.WrittenCount"/>.
    /// </summary>
    [TestMethod]
    public void GetSpan_WhenCalledWithoutAdvance_ShouldNotChangeWrittenCount()
    {
        using var builder = new PooledBufferBuilder<int>(16);
        builder.Append(1);
        var countBefore = builder.WrittenCount;

        _ = builder.GetSpan(4);

        Assert.AreEqual(countBefore, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that writing into the span returned by <see cref="PooledBufferBuilder{T}.GetSpan"/> and then
    /// calling <see cref="PooledBufferBuilder{T}.Advance"/> commits the data correctly.
    /// </summary>
    [TestMethod]
    public void GetSpan_WhenDataWrittenAndAdvanceCalled_ShouldExposeDataViaWrittenSpan()
    {
        using var builder = new PooledBufferBuilder<int>(8);
        System.Span<int> span = builder.GetSpan(3);
        span[0] = 10;
        span[1] = 20;
        span[2] = 30;
        builder.Advance(3);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.GetSpan"/> throws <see cref="ObjectDisposedException"/>
    /// after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void GetSpan_WhenDisposed_ShouldThrowExactly()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.GetSpan();
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.GetSpan"/> grows the buffer when the size hint exceeds
    /// the current free capacity.
    /// </summary>
    [TestMethod]
    public void GetSpan_WhenSizeHintExceedsFreeCapacity_ShouldGrowBufferAndReturnAdequateSpan()
    {
        using var builder = new PooledBufferBuilder<int>(2);

        System.Span<int> span = builder.GetSpan(100);

        Assert.IsGreaterThanOrEqualTo(100, span.Length);
        Assert.IsGreaterThanOrEqualTo(100, builder.Capacity);
    }
    // -----------------------------------------------------------------------
    // GetSpan
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.GetSpan"/> returns a span whose length is at least
    /// <c>sizeHint</c> when the current free capacity is sufficient.
    /// </summary>
    [TestMethod]
    public void GetSpan_WhenSizeHintFitsInFreeCapacity_ShouldReturnSpanOfAtLeastSizeHint()
    {
        using var builder = new PooledBufferBuilder<int>(32);

        System.Span<int> span = builder.GetSpan(8);

        Assert.IsGreaterThanOrEqualTo(8, span.Length);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.GetSpan"/> throws <see cref="ArgumentOutOfRangeException"/>
    /// when the size hint is negative.
    /// </summary>
    [TestMethod]
    public void GetSpan_WhenSizeHintIsNegative_ShouldThrowExactly()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.GetSpan(-1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.GetSpan"/> with a zero hint returns a span with at least
    /// one element of free capacity.
    /// </summary>
    [TestMethod]
    public void GetSpan_WhenSizeHintIsZero_ShouldReturnAtLeastOneElementOfFreeCapacity()
    {
        using var builder = new PooledBufferBuilder<int>(4);

        System.Span<int> span = builder.GetSpan(0);

        Assert.IsGreaterThanOrEqualTo(1, span.Length);
    }

    /// <summary>
    /// Verifies that interleaving <see cref="PooledBufferBuilder{T}.Append"/> with
    /// <see cref="PooledBufferBuilder{T}.GetSpan"/> and <see cref="PooledBufferBuilder{T}.Advance"/> accumulates
    /// all elements in the correct order.
    /// </summary>
    [TestMethod]
    public void IBufferWriter_WhenInterleavedWithAppend_ShouldAccumulateAllElementsInOrder()
    {
        using var builder = new PooledBufferBuilder<int>(16);

        builder.Append(1);

        System.Span<int> span = builder.GetSpan(2);
        span[0] = 2;
        span[1] = 3;
        builder.Advance(2);

        builder.Append(4);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, builder.WrittenSpan.ToArray());
    }

    // -----------------------------------------------------------------------
    // IBufferWriter<T> interface usage
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}"/> can be used as an <see cref="IBufferWriter{T}"/> and
    /// that data written through the interface is accessible via <see cref="PooledBufferBuilder{T}.WrittenSpan"/>.
    /// </summary>
    [TestMethod]
    public void IBufferWriter_WhenUsedThroughInterface_ShouldAccumulateDataCorrectly()
    {
        using var builder = new PooledBufferBuilder<int>(8);
        IBufferWriter<int> writer = builder;

        System.Span<int> span = writer.GetSpan(3);
        span[0] = 100;
        span[1] = 200;
        span[2] = 300;
        writer.Advance(3);

        CollectionAssert.AreEqual(new[] { 100, 200, 300 }, builder.WrittenSpan.ToArray());
    }

}
