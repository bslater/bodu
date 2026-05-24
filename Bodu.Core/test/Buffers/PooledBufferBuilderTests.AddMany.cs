// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.AddMany.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AddMany"/> appends the specified number of copies of
    /// the value to the buffer.
    /// </summary>
    [TestMethod]
    public void AddMany_WhenCalled_ShouldAppendCorrectNumberOfCopies()
    {
        using var builder = new PooledBufferBuilder<int>();

        builder.AddMany(7, 5);

        Assert.AreEqual(5, builder.WrittenCount);
        CollectionAssert.AreEqual(new[] { 7, 7, 7, 7, 7 }, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AddMany"/> grows the buffer when necessary.
    /// </summary>
    [TestMethod]
    public void AddMany_WhenCountExceedsCapacity_ShouldGrowBufferAndAppendAllCopies()
    {
        using var builder = new PooledBufferBuilder<int>(2);

        builder.AddMany(42, 100);

        Assert.AreEqual(100, builder.WrittenCount);
        Assert.IsTrue(builder.WrittenSpan.ToArray().All(v => v == 42));
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AddMany"/> throws <see cref="ArgumentOutOfRangeException"/>
    /// when the count is negative.
    /// </summary>
    [TestMethod]
    public void AddMany_WhenCountIsNegative_ShouldThrowExactly()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            builder.AddMany(0, -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AddMany"/> with a count of one appends exactly a single
    /// copy of the value.
    /// </summary>
    [TestMethod]
    public void AddMany_WhenCountIsOne_ShouldAppendExactlySingleCopy()
    {
        using var builder = new PooledBufferBuilder<int>();

        builder.AddMany(99, 1);

        Assert.AreEqual(1, builder.WrittenCount);
        Assert.AreEqual(99, builder.WrittenSpan[0]);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AddMany"/> with a count of zero does not change the
    /// builder's state.
    /// </summary>
    [TestMethod]
    public void AddMany_WhenCountIsZero_ShouldNotChangeWrittenCount()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.Append(1);

        builder.AddMany(99, 0);

        Assert.AreEqual(1, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AddMany"/> throws <see cref="ObjectDisposedException"/>
    /// after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void AddMany_WhenDisposed_ShouldThrowExactly()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.AddMany(1, 3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.AddMany"/> fills all slots correctly when
    /// <typeparamref name="T"/> is a reference type.
    /// </summary>
    [TestMethod]
    public void AddMany_WhenReferenceType_ShouldFillAllSlotsWithSameReference()
    {
        const string value = "hello";
        using var builder = new PooledBufferBuilder<string>();

        builder.AddMany(value, 4);

        Assert.AreEqual(4, builder.WrittenCount);
        Assert.IsTrue(builder.WrittenSpan.ToArray().All(v => ReferenceEquals(v, value)));
    }

}
