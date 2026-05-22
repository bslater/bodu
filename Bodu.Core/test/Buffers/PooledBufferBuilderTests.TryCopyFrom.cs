// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.TryCopyFrom.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.TryCopyFrom"/> replaces any previously buffered data
    /// with the source contents.
    /// </summary>
    [TestMethod]
    public void TryCopyFrom_WhenBuilderHasExistingItems_ShouldReplaceWithSourceContents()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([99, 88, 77]);

        var replacement = new System.Collections.Generic.List<int> { 1, 2 };
        builder.TryCopyFrom(replacement);

        Assert.AreEqual(2, builder.WrittenCount);
        CollectionAssert.AreEqual(replacement, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.TryCopyFrom"/> throws <see cref="ObjectDisposedException"/>
    /// when called on a disposed builder.
    /// </summary>
    [TestMethod]
    public void TryCopyFrom_WhenDisposed_ShouldThrowExactly()
    {
        var builder = new PooledBufferBuilder<int>();
        builder.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            builder.TryCopyFrom(new System.Collections.Generic.List<int> { 1 });
        });
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.TryCopyFrom"/> with an empty collection resets
    /// <see cref="PooledBufferBuilder{T}.WrittenCount"/> to zero.
    /// </summary>
    [TestMethod]
    public void TryCopyFrom_WhenEmptyCollectionPassed_ShouldSucceedAndSetCountToZero()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([1, 2, 3]);

        var success = builder.TryCopyFrom(new System.Collections.Generic.List<int>());

        Assert.IsTrue(success);
        Assert.AreEqual(0, builder.WrittenCount);
    }
    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.TryCopyFrom"/> returns <see langword="true"/> and copies
    /// the source contents when passed a <see cref="System.Collections.Generic.List{T}"/> (which implements
    /// <see cref="System.Collections.Generic.ICollection{T}"/>).
    /// </summary>
    [TestMethod]
    public void TryCopyFrom_WhenListPassed_ShouldCopyCorrectly_UsingICollection()
    {
        var source = new System.Collections.Generic.List<int> { 1, 2, 3 };
        using var builder = new PooledBufferBuilder<int>();

        var success = builder.TryCopyFrom(source);

        Assert.IsTrue(success);
        CollectionAssert.AreEqual(source, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.TryCopyFrom"/> returns <see langword="false"/> and does
    /// not modify the builder when the source does not implement
    /// <see cref="System.Collections.Generic.ICollection{T}"/>.
    /// </summary>
    [TestMethod]
    public void TryCopyFrom_WhenSourceIsNotICollection_ShouldReturnFalseAndLeaveBuilderUnchanged()
    {
        using var builder = new PooledBufferBuilder<int>();
        builder.AppendRange([10, 20]);

        // Use a custom IReadOnlyCollection that is not ICollection<T>.
        var success = builder.TryCopyFrom(new NonCollectionReadOnlyCollection([1, 2, 3]));

        Assert.IsFalse(success);
        // Builder should be unchanged.
        CollectionAssert.AreEqual(new[] { 10, 20 }, builder.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.TryCopyFrom"/> with a <see langword="null"/> source
    /// throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void TryCopyFrom_WhenSourceIsNull_ShouldThrowExactly()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            builder.TryCopyFrom(null!);
        });
    }

    // ---------------------------------------------------------------------------
    // Helper: an IReadOnlyCollection<int> that does NOT implement ICollection<int>
    // ---------------------------------------------------------------------------
    private sealed class NonCollectionReadOnlyCollection
        : System.Collections.Generic.IReadOnlyCollection<int>
    {

        private readonly int[] _items;

        internal NonCollectionReadOnlyCollection(int[] items) => _items = items;

        public int Count => _items.Length;

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();

        public System.Collections.Generic.IEnumerator<int> GetEnumerator() =>
            ((System.Collections.Generic.IEnumerable<int>)_items).GetEnumerator();

    }

}
