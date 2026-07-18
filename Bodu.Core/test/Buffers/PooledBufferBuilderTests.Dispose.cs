// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        Assert.ThrowsExactly<ObjectDisposedException>(builder.Reset);
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

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Dispose"/> does not retain references to elements stored in
    /// the buffer once the strong references held by the test have been dropped. Without clearing on return, the
    /// pooled array would survive in <see cref="System.Buffers.ArrayPool{T}.Shared"/> with live references that
    /// the GC could never collect.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenBufferHoldsReferenceItems_ShouldNotRetainReferences()
    {
        WeakReference[] weakRefs = CreateAndDisposeBuilder();

        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        for (int i = 0; i < weakRefs.Length; i++)
            Assert.IsFalse(weakRefs[i].IsAlive, $"Reference {i} was retained after Dispose.");

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static WeakReference[] CreateAndDisposeBuilder()
        {
            var builder = new PooledBufferBuilder<object>(initialCapacity: 8);
            var refs = new WeakReference[5];

            for (int i = 0; i < refs.Length; i++)
            {
                object item = new();
                refs[i] = new WeakReference(item);
                builder.Append(item);
            }

            builder.Dispose();
            return refs;
        }
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.Reset"/> clears reference slots in the written region so the
    /// GC can collect the referenced objects even though the rented array is retained for further use.
    /// </summary>
    [TestMethod]
    public void Reset_WhenBufferHoldsReferenceItems_ShouldNotRetainReferences()
    {
        WeakReference[] weakRefs;
        var builder = new PooledBufferBuilder<object>(initialCapacity: 8);

        try
        {
            weakRefs = FillAndReset(builder);

            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            for (int i = 0; i < weakRefs.Length; i++)
                Assert.IsFalse(weakRefs[i].IsAlive, $"Reference {i} was retained after Reset.");
        }
        finally
        {
            builder.Dispose();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static WeakReference[] FillAndReset(PooledBufferBuilder<object> b)
        {
            var refs = new WeakReference[5];

            for (int i = 0; i < refs.Length; i++)
            {
                object item = new();
                refs[i] = new WeakReference(item);
                b.Append(item);
            }

            b.Reset();
            return refs;
        }
    }

}
