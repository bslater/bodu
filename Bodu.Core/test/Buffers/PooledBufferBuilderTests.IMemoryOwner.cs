// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.IMemoryOwner.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    /// <summary>
    /// Verifies that accessing <see cref="IMemoryOwner{T}.Memory"/> after disposal throws
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void IMemoryOwner_Memory_WhenDisposed_ShouldThrowExactly()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();
        IMemoryOwner<int> owner = builder;

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = owner.Memory;
        });
    }
    /// <summary>
    /// Verifies that the <see cref="IMemoryOwner{T}.Memory"/> property returns an empty memory region when the
    /// builder has no written elements.
    /// </summary>
    [TestMethod]
    public void IMemoryOwner_Memory_WhenEmpty_ShouldReturnEmptyMemory()
    {
        using var builder = new PooledBufferBuilder<int>();
        IMemoryOwner<int> owner = builder;

        Assert.AreEqual(0, owner.Memory.Length);
    }

    /// <summary>
    /// Verifies that the <see cref="IMemoryOwner{T}.Memory"/> property returns a memory region containing all
    /// written elements in the correct order.
    /// </summary>
    [TestMethod]
    public void IMemoryOwner_Memory_WhenItemsBuffered_ShouldContainAllWrittenElements()
    {
        int[] expected = [1, 2, 3, 4, 5];
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(expected.AsSpan());
        IMemoryOwner<int> owner = builder;

        CollectionAssert.AreEqual(expected, owner.Memory.ToArray());
    }

    /// <summary>
    /// Verifies that the memory returned by <see cref="IMemoryOwner{T}.Memory"/> has the same length as
    /// <see cref="PooledBufferBuilder{T}.WrittenCount"/>.
    /// </summary>
    [TestMethod]
    public void IMemoryOwner_Memory_WhenItemsBuffered_ShouldHaveLengthEqualToWrittenCount()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([10, 20, 30]);
        IMemoryOwner<int> owner = builder;

        Assert.AreEqual(builder.WrittenCount, owner.Memory.Length);
    }

    /// <summary>
    /// Verifies that the memory returned by <see cref="IMemoryOwner{T}.Memory"/> matches the contents of
    /// <see cref="PooledBufferBuilder{T}.WrittenMemory"/>.
    /// </summary>
    [TestMethod]
    public void IMemoryOwner_Memory_WhenItemsBuffered_ShouldMatchWrittenMemory()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(Enumerable.Range(1, 6));
        IMemoryOwner<int> owner = builder;

        CollectionAssert.AreEqual(builder.WrittenMemory.ToArray(), owner.Memory.ToArray());
    }

    /// <summary>
    /// Verifies that the <see cref="IMemoryOwner{T}.Memory"/> property returns an empty region after
    /// <see cref="PooledBufferBuilder{T}.Reset"/> is called.
    /// </summary>
    [TestMethod]
    public void IMemoryOwner_Memory_WhenResetCalled_ShouldReturnEmptyMemory()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([1, 2, 3]);
        builder.Reset();
        IMemoryOwner<int> owner = builder;

        Assert.AreEqual(0, owner.Memory.Length);
    }

}
