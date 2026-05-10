// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.WrittenCount.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.WrittenCount"/> returns the correct count after items are
    /// appended.
    /// </summary>
    [TestMethod]
    public void WrittenCount_WhenItemsAdded_ShouldReturnAccurateValue()
    {
        using var builder = new PooledBufferBuilder<string>();
        builder.AppendRange(new[] { "a", "b", "c" });

        Assert.AreEqual(3, builder.WrittenCount);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.WrittenCount"/> throws <see cref="ObjectDisposedException"/>
    /// after the builder has been disposed.
    /// </summary>
    [TestMethod]
    public void WrittenCount_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.WrittenCount;
        });
    }
}
