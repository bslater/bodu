// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.TryCopyTo.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.TryCopyTo"/> copies every written element and returns
    /// <see langword="true"/> when the destination is exactly the written size.
    /// </summary>
    [TestMethod]
    public void TryCopyTo_WhenDestinationExactlyFits_ShouldReturnTrueAndCopyAllElements()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([10, 20, 30]);
        Span<int> destination = stackalloc int[3];

        var copied = builder.TryCopyTo(destination);

        Assert.IsTrue(copied);
        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.TryCopyTo"/> returns <see langword="true"/> and copies into
    /// the leading prefix when the destination is larger than the written region, leaving the tail untouched.
    /// </summary>
    [TestMethod]
    public void TryCopyTo_WhenDestinationIsLarger_ShouldReturnTrueAndLeaveTailUntouched()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([1, 2, 3]);
        var destination = new[] { -1, -1, -1, -1, -1 };

        var copied = builder.TryCopyTo(destination);

        Assert.IsTrue(copied);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, -1, -1 }, destination);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.TryCopyTo"/> returns <see langword="false"/> and writes
    /// nothing when the destination is smaller than the written region. This is the explicit non-throwing path
    /// that differentiates this method from <see cref="PooledBufferBuilder{T}.CopyTo"/>.
    /// </summary>
    [TestMethod]
    public void TryCopyTo_WhenDestinationIsTooSmall_ShouldReturnFalseAndNotWrite()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([1, 2, 3, 4]);
        var destination = new[] { 99, 99, 99 };

        var copied = builder.TryCopyTo(destination);

        Assert.IsFalse(copied);
        CollectionAssert.AreEqual(new[] { 99, 99, 99 }, destination);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.TryCopyTo"/> returns <see langword="true"/> for an empty
    /// builder and writes nothing, regardless of the destination length (including an empty destination).
    /// </summary>
    [TestMethod]
    public void TryCopyTo_WhenBuilderIsEmpty_ShouldReturnTrueAndWriteNothing()
    {
        using var builder = new PooledBufferBuilder<int>();
        var destination = new[] { 7, 8, 9 };

        var copied = builder.TryCopyTo(destination);
        var copiedToEmpty = builder.TryCopyTo([]);

        Assert.IsTrue(copied);
        Assert.IsTrue(copiedToEmpty);
        CollectionAssert.AreEqual(new[] { 7, 8, 9 }, destination);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.TryCopyTo"/> throws <see cref="ObjectDisposedException"/>
    /// when the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void TryCopyTo_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Append(1);
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.TryCopyTo(new int[4]);
        });
    }
}
