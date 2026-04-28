// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder.Dispose" />, when Called, UsingPooledBufferBuilder, throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldPreventBufferAccess_UsingPooledBufferBuilder()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(Enumerable.Range(1, 5));
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => builder.AsArray());
        Assert.ThrowsExactly<ObjectDisposedException>(() => builder.AsSpan());
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = builder.Count);
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder.Dispose" />, when InvokedMultipleTimes, UsingPooledBufferBuilder, NotThrow.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInvokedMultipleTimes_ShouldNotThrow_UsingPooledBufferBuilder()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(new[] { 1, 2, 3 });

        builder.Dispose();
        builder.Dispose(); // no exception expected
    }
}
