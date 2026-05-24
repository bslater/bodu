// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.ToArrayAndDispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.ToArrayAndDispose"/> returns a fresh array containing
    /// every written element in order and disposes the builder so further operations throw.
    /// </summary>
    [TestMethod]
    public void ToArrayAndDispose_WhenCalled_ShouldReturnWrittenElementsAndDisposeBuilder()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([1, 2, 3, 4, 5]);

        var result = builder.ToArrayAndDispose();

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, result);
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.WrittenCount;
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.ToArrayAndDispose"/> returns an empty array for an empty
    /// builder and still disposes the builder.
    /// </summary>
    [TestMethod]
    public void ToArrayAndDispose_WhenBuilderIsEmpty_ShouldReturnEmptyArrayAndDisposeBuilder()
    {
        var builder = new PooledBufferBuilder<int>();

        var result = builder.ToArrayAndDispose();

        Assert.AreEqual(0, result.Length);
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.Append(0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.ToArrayAndDispose"/> returns a copy that is independent of
    /// any underlying pooled buffer — mutating the returned array does not corrupt subsequent pool consumers.
    /// </summary>
    [TestMethod]
    public void ToArrayAndDispose_WhenCalled_ShouldReturnIndependentCopyNotAliasingPooledBuffer()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([7, 8, 9]);

        var result = builder.ToArrayAndDispose();
        result[0] = -42;

        var second = new PooledBufferBuilder<int>();
        try
        {
            second.AppendRange([100, 200, 300]);
            CollectionAssert.AreEqual(new[] { 100, 200, 300 }, second.WrittenSpan.ToArray());
        }
        finally
        {
            second.Dispose();
        }
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.ToArrayAndDispose"/> throws
    /// <see cref="ObjectDisposedException"/> when called on an already-disposed builder.
    /// </summary>
    [TestMethod]
    public void ToArrayAndDispose_WhenAlreadyDisposed_ShouldThrowObjectDisposedException()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Append(1);
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = builder.ToArrayAndDispose();
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.ToArrayAndDispose"/> for a reference-type element clears
    /// the pooled buffer so the returned-but-disposed builder does not retain references through the pool. The
    /// returned array is the sole strong reference to the elements.
    /// </summary>
    [TestMethod]
    public void ToArrayAndDispose_WhenBufferHoldsReferenceItems_ShouldNotLeakReferencesThroughPool()
    {
        WeakReference[] weakRefs = MaterializeAndDrop();

        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        for (var i = 0; i < weakRefs.Length; i++)
            Assert.IsFalse(weakRefs[i].IsAlive, $"Reference {i} was retained after ToArrayAndDispose.");

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static WeakReference[] MaterializeAndDrop()
        {
            var builder = new PooledBufferBuilder<object>(initialCapacity: 8);
            var refs = new WeakReference[5];

            for (var i = 0; i < refs.Length; i++)
            {
                var item = new object();
                refs[i] = new WeakReference(item);
                builder.Append(item);
            }

            // Returned array is intentionally discarded so all strong references drop with this stack frame.
            _ = builder.ToArrayAndDispose();
            return refs;
        }
    }
}
