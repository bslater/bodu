// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.Dispose"/> prevents all subsequent read and write
    /// operations by throwing <see cref="ObjectDisposedException"/>.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldPreventBufferAccess_UsingPooledBufferBuilder()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.AppendRange(Enumerable.Range(1, 5));
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = builder.WrittenCount; });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = builder.Capacity; });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { builder.Append(1); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { builder.AppendRange([1]); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { builder.AppendRange(new[] { 1 }.AsSpan()); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = builder.AsArray(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = builder.WrittenSpan; });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = builder.WrittenMemory; });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = builder.FreeCapacity; });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = builder.GetSpan(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = builder.GetMemory(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { builder.Advance(0); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { builder.EnsureCapacity(1); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = builder.IsEmpty; });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { builder.AppendRange(System.ReadOnlyMemory<int>.Empty); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { builder.AddMany(0, 1); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { builder.CopyTo(new int[4]); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = builder.DangerousGetArray(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { builder.Sort(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { builder.Sort((a, b) => a.CompareTo(b)); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { builder.Reset(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { builder.TryCopyFrom(new System.Collections.Generic.List<int>()); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = ((System.Buffers.IMemoryOwner<int>)builder).Memory; });
    }

    /// <summary>
    /// Verifies that calling <see cref="PooledBufferBuilder{T}.Dispose"/> multiple times does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInvokedMultipleTimes_ShouldNotThrow_UsingPooledBufferBuilder()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([1, 2, 3]);

        builder.Dispose();
        builder.Dispose(); // no exception expected
    }
}
