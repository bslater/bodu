// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for the internal <see cref="OrderedSetStorage{T}" /> engine that backs
/// <see cref="OrderedSet{T}" /> and <see cref="IndexedSet{T}" />.
/// </summary>
[TestClass]
public partial class OrderedSetStorageTests
{

    /// <summary>
    /// Verifies the happy path for <see cref="OrderedSetStorage{T}.Add(T)" /> on an empty storage —
    /// the canonical smoke check for the engine.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNew_ShouldAppendAndReturnTrue()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        var added = sut.Add(42);

        Assert.IsTrue(added);
        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(42, sut.GetAt(0));
    }

    /// <summary>
    /// Creates a populated storage seeded with the specified items in order.
    /// </summary>
    /// <param name="items">The items to seed.</param>
    /// <param name="comparer">An optional comparer.</param>
    /// <returns>The populated storage.</returns>
    private static OrderedSetStorage<T> CreateStorage<T>(IEnumerable<T> items, IEqualityComparer<T>? comparer = null)
        where T : notnull
    {
        var storage = new OrderedSetStorage<T>(0, comparer);
        foreach (T item in items)
            storage.Add(item);

        return storage;
    }

    /// <summary>
    /// Returns the contents of <paramref name="storage" /> as a snapshot array in insertion order.
    /// </summary>
    /// <param name="storage">The storage to snapshot.</param>
    /// <returns>An array containing the elements in insertion order.</returns>
    private static T[] Snapshot<T>(OrderedSetStorage<T> storage)
        where T : notnull
    {
        var result = new T[storage.Count];
        for (var i = 0; i < storage.Count; i++)
            result[i] = storage.GetAt(i);

        return result;
    }

    /// <summary>
    /// A reference-type helper whose <see cref="GetHashCode" /> deliberately collides for every instance,
    /// allowing tests to exercise hash-table chain traversal and collision handling. Equality is identity-based.
    /// </summary>
    private sealed class HashCollider
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="HashCollider" /> class with the specified label.
        /// </summary>
        /// <param name="label">A label used for diagnostics; not part of equality.</param>
        public HashCollider(string label)
        {
            this.Label = label;
        }

        /// <summary>
        /// Gets the diagnostic label.
        /// </summary>
        /// <returns>The diagnostic label.</returns>
        public string Label { get; }

        /// <inheritdoc />
        public override bool Equals(object? obj) => ReferenceEquals(this, obj);

        /// <inheritdoc />
        public override int GetHashCode() => 0;

        /// <inheritdoc />
        public override string ToString() => this.Label;

    }

}
