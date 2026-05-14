// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IndexedSetTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="IndexedSet{T}" />.
/// </summary>
[TestClass]
public partial class IndexedSetTests
{
    /// <summary>
    /// Verifies the happy-path smoke check: items added in sequence are accessible at their insertion indices.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemsAreUnique_ShouldBeIndexAddressable()
    {
        var sut = new IndexedSet<int>();

        Assert.IsTrue(sut.Add(10));
        Assert.IsTrue(sut.Add(20));
        Assert.IsTrue(sut.Add(30));

        Assert.AreEqual(3, sut.Count);
        Assert.AreEqual(10, sut[0]);
        Assert.AreEqual(20, sut[1]);
        Assert.AreEqual(30, sut[2]);
    }

    /// <summary>
    /// Creates an <see cref="IndexedSet{T}" /> seeded with the specified items.
    /// </summary>
    /// <param name="items">The items to seed.</param>
    /// <param name="comparer">An optional comparer.</param>
    /// <returns>The populated set.</returns>
    private static IndexedSet<T> CreateSet<T>(IEnumerable<T> items, IEqualityComparer<T>? comparer = null)
        where T : notnull
    {
        var set = new IndexedSet<T>(comparer);
        foreach (T item in items)
            set.Add(item);

        return set;
    }

    /// <summary>
    /// Materialises the elements of <paramref name="set" /> using the public indexer.
    /// </summary>
    /// <param name="set">The set to snapshot.</param>
    /// <returns>An array containing the elements in insertion order.</returns>
    private static T[] SnapshotByIndexer<T>(IndexedSet<T> set)
        where T : notnull
    {
        var result = new T[set.Count];
        for (var i = 0; i < set.Count; i++)
            result[i] = set[i];

        return result;
    }
}
