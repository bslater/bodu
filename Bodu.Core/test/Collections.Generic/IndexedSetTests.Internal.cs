// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.Internal.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class IndexedSetTests
{

    /// <summary>
    /// Verifies that the collection constructor falls through to the zero-capacity branch when the source is a
    /// pure deferred <see cref="IEnumerable{T}" /> with no count hint.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionHasNoCountHint_ShouldStillPopulateSet()
    {
        IEnumerable<int> source = YieldNumbers();

        var sut = new IndexedSet<int>(source);

        Assert.AreEqual(3, sut.Count);
        Assert.AreEqual(7, sut[0]);
        Assert.AreEqual(8, sut[1]);
        Assert.AreEqual(9, sut[2]);

        static IEnumerable<int> YieldNumbers()
        {
            yield return 7;
            yield return 8;
            yield return 9;
        }
    }

    /// <summary>
    /// Verifies that the collection constructor uses the <see cref="IReadOnlyCollection{T}.Count" /> fast path
    /// when the source implements <see cref="IReadOnlyCollection{T}" /> but not <see cref="ICollection{T}" />.
    /// Exercises the second branch of the internal capacity hint.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionIsReadOnlyCollectionOnly_ShouldUseReadOnlyCountForCapacity()
    {
        var source = new ReadOnlyCollectionOnly<int>([10, 20, 30, 40]);

        var sut = new IndexedSet<int>(source);

        Assert.AreEqual(4, sut.Count);
        Assert.IsGreaterThanOrEqualTo(4, sut.Capacity);
    }
    /// <summary>
    /// Verifies that the internal <see cref="IndexedSet{T}.DebuggerStorage" /> property exposes the same
    /// underlying storage instance used by the public surface so the DebuggerTypeProxy can walk it.
    /// </summary>
    [TestMethod]
    public void DebuggerStorage_WhenAccessed_ShouldReturnUnderlyingStorage()
    {
        var sut = new IndexedSet<int>([1, 2, 3]);

        OrderedSetStorage<int> storage = sut.DebuggerStorage;

        Assert.IsNotNull(storage);
        Assert.AreEqual(sut.Count, storage.Count);
    }

    /// <summary>
    /// Wraps an <see cref="IEnumerable{T}" /> as <see cref="IReadOnlyCollection{T}" /> only — deliberately not
    /// implementing <see cref="ICollection{T}" /> so capacity-hint helpers see the read-only-collection branch.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    private sealed class ReadOnlyCollectionOnly<T>
        : IReadOnlyCollection<T>
    {

        private readonly T[] _items;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexedSetTests.ReadOnlyCollectionOnly{T}" /> class
        /// wrapping the supplied items.
        /// </summary>
        /// <param name="items">The items to expose. Must not be <see langword="null" />.</param>
        public ReadOnlyCollectionOnly(T[] items)
        {
            _items = items;
        }

        /// <inheritdoc />
        public int Count => _items.Length;

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            foreach (T item in _items)
                yield return item;
        }

    }

}
