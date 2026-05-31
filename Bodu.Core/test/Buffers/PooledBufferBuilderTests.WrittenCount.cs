// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.WrittenCount.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.WrittenCount"/> throws <see cref="ObjectDisposedException"/>
    /// after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void WrittenCount_WhenDisposed_ShouldThrowExactly()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.WrittenCount;
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.WrittenCount"/> is not changed by calling
    /// <see cref="PooledBufferBuilder{T}.GetSpan"/> without a subsequent <see cref="PooledBufferBuilder{T}.Advance"/>.
    /// </summary>
    [TestMethod]
    public void WrittenCount_WhenGetSpanCalledWithoutAdvance_ShouldRemainUnchanged()
    {
        using var builder = new PooledBufferBuilder<int>(16);
        builder.Append(5);

        _ = builder.GetSpan(4);

        Assert.AreEqual(1, builder.WrittenCount);
    }
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.WrittenCount"/> is zero on a newly constructed builder.
    /// </summary>
    [TestMethod]
    public void WrittenCount_WhenInitialised_ShouldBeZero()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.AreEqual(0, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.WrittenCount"/> returns the correct count after items are
    /// appended.
    /// </summary>
    [TestMethod]
    public void WrittenCount_WhenItemsAdded_ShouldReturnAccurateValue()
    {
        using var builder = new PooledBufferBuilder<string>();
        builder.AppendRange(["a", "b", "c"]);

        Assert.AreEqual(3, builder.WrittenCount);
    }

}
