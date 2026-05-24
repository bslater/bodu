// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="OrderedSet{T}" />.
/// </summary>
[TestClass]
public partial class OrderedSetTests
{

    /// <summary>
    /// Verifies the happy-path smoke check: adding three unique items preserves insertion order and exposes
    /// them through the read-only list view.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemsAreUnique_ShouldPreserveInsertionOrder()
    {
        var sut = new OrderedSet<int>();

        Assert.IsTrue(sut.Add(1));
        Assert.IsTrue(sut.Add(2));
        Assert.IsTrue(sut.Add(3));

        Assert.AreEqual(3, sut.Count);
        Assert.AreEqual(1, sut[0]);
        Assert.AreEqual(2, sut[1]);
        Assert.AreEqual(3, sut[2]);
    }

    /// <summary>
    /// Creates an <see cref="OrderedSet{T}" /> seeded with the specified items.
    /// </summary>
    /// <param name="items">The items to seed.</param>
    /// <param name="comparer">An optional comparer.</param>
    /// <returns>The populated set.</returns>
    private static OrderedSet<T> CreateSet<T>(IEnumerable<T> items, IEqualityComparer<T>? comparer = null)
        where T : notnull
    {
        var set = new OrderedSet<T>(comparer);
        foreach (T item in items)
            set.Add(item);

        return set;
    }

    /// <summary>
    /// Materialises the elements of <paramref name="set" /> using the public enumerator.
    /// </summary>
    /// <param name="set">The set to snapshot.</param>
    /// <returns>A list containing the elements in enumeration order.</returns>
    private static List<T> SnapshotByEnumerator<T>(OrderedSet<T> set)
        where T : notnull
    {
        var result = new List<T>(set.Count);
        foreach (T item in set)
            result.Add(item);

        return result;
    }

    /// <summary>
    /// Materialises the elements of <paramref name="set" /> using the public read-only list indexer.
    /// </summary>
    /// <param name="set">The set to snapshot.</param>
    /// <returns>An array containing the elements in insertion order.</returns>
    private static T[] SnapshotByIndexer<T>(OrderedSet<T> set)
        where T : notnull
    {
        var result = new T[set.Count];
        for (var i = 0; i < set.Count; i++)
            result[i] = set[i];

        return result;
    }

}
