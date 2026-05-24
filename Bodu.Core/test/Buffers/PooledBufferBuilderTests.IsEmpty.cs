// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.IsEmpty.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.IsEmpty"/> returns <see langword="true"/> on a
    /// newly constructed builder.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenBuilderIsNew_ShouldReturnTrue()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.IsTrue(builder.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.IsEmpty"/> throws <see cref="ObjectDisposedException"/>
    /// after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenDisposed_ShouldThrowExactly()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.IsEmpty;
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.IsEmpty"/> returns <see langword="false"/> after at least
    /// one element is appended.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenItemsAppended_ShouldReturnFalse()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.Append(42);

        Assert.IsFalse(builder.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.IsEmpty"/> returns <see langword="true"/> after
    /// <see cref="PooledBufferBuilder{T}.Reset"/> clears a populated builder.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenResetCalled_ShouldReturnTrue()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.Append(1);
        builder.Reset();

        Assert.IsTrue(builder.IsEmpty);
    }

}
