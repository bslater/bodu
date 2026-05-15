// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageTests.HashTable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

public partial class OrderedSetStorageTests
{

    /// <summary>
    /// Verifies that interleaved add/remove operations leave the hash table in a consistent state where every
    /// surviving element is locatable at its current insertion index.
    /// </summary>
    [TestMethod]
    public void HashTable_WhenAddingAndRemovingInterleaved_ShouldRemainConsistent()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        for (var i = 0; i < 1000; i++)
            sut.Add(i);

        for (var i = 0; i < 1000; i += 2)
            Assert.IsTrue(sut.Remove(i));

        Assert.AreEqual(500, sut.Count);
        for (var i = 0; i < sut.Count; i++)
        {
            var item = sut.GetAt(i);
            Assert.AreEqual(i, sut.IndexOf(item));
            Assert.IsTrue(sut.Contains(item));
        }
    }

    /// <summary>
    /// Verifies that removing colliding items still resolves the remaining chain correctly.
    /// </summary>
    [TestMethod]
    public void HashTable_WhenColliderItemsRemoved_ShouldStillResolveRemaining()
    {
        var sut = new OrderedSetStorage<HashCollider>(0, null);
        var a = new HashCollider("a");
        var b = new HashCollider("b");
        var c = new HashCollider("c");
        var d = new HashCollider("d");

        sut.Add(a);
        sut.Add(b);
        sut.Add(c);
        sut.Add(d);

        sut.Remove(b);
        sut.Remove(c);

        Assert.IsTrue(sut.Contains(a));
        Assert.IsFalse(sut.Contains(b));
        Assert.IsFalse(sut.Contains(c));
        Assert.IsTrue(sut.Contains(d));
        Assert.AreEqual(0, sut.IndexOf(a));
        Assert.AreEqual(1, sut.IndexOf(d));
    }

    /// <summary>
    /// Verifies that supplying a comparer with consistent <see cref="IEqualityComparer{T}.GetHashCode" /> and
    /// <see cref="IEqualityComparer{T}.Equals" /> is respected even when many items share the same hash.
    /// </summary>
    [TestMethod]
    public void HashTable_WhenComparerForcesIdenticalHashes_ShouldStillDistinguishByEquals()
    {
        var sut = new OrderedSetStorage<string>(0, new FixedHashStringComparer());

        Assert.IsTrue(sut.Add("alpha"));
        Assert.IsTrue(sut.Add("beta"));
        Assert.IsTrue(sut.Add("gamma"));
        Assert.IsFalse(sut.Add("alpha"));

        Assert.AreEqual(3, sut.Count);
        Assert.IsTrue(sut.Contains("beta"));
        Assert.AreEqual(1, sut.IndexOf("beta"));
    }

    // --------------------------------------------------------
    // Comparer interaction
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a custom comparer governs lookups across the full add/contains/index/remove cycle.
    /// </summary>
    [TestMethod]
    public void HashTable_WhenCustomComparerProvided_ShouldGovernAllLookups()
    {
        var sut = new OrderedSetStorage<string>(0, StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(sut.Add("Alpha"));
        Assert.IsFalse(sut.Add("ALPHA"));
        Assert.IsTrue(sut.Add("Beta"));

        Assert.IsTrue(sut.Contains("alpha"));
        Assert.AreEqual(0, sut.IndexOf("ALPHA"));
        Assert.IsTrue(sut.Remove("alpha"));
        Assert.IsFalse(sut.Contains("alpha"));
        Assert.AreEqual(1, sut.Count);
    }
    // --------------------------------------------------------
    // Rehashing
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that filling the storage past the configured load factor triggers a rehash while preserving
    /// every element's index.
    /// </summary>
    [TestMethod]
    [DataRow(8)]
    [DataRow(64)]
    [DataRow(512)]
    [DataRow(4096)]
    public void HashTable_WhenLoadFactorExceeded_ShouldRehashAndPreserveOrder(int size)
    {
        var sut = new OrderedSetStorage<int>(0, null);

        for (var i = 0; i < size; i++)
            Assert.IsTrue(sut.Add(i));

        Assert.AreEqual(size, sut.Count);
        for (var i = 0; i < size; i++)
        {
            Assert.AreEqual(i, sut.GetAt(i));
            Assert.AreEqual(i, sut.IndexOf(i));
            Assert.IsTrue(sut.Contains(i));
        }
    }

    // --------------------------------------------------------
    // Collision handling
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a long chain of hash-colliding items is fully traversed by lookups.
    /// </summary>
    [TestMethod]
    public void HashTable_WhenManyItemsCollide_ShouldResolveEachThroughChain()
    {
        var sut = new OrderedSetStorage<HashCollider>(0, null);
        var items = new HashCollider[32];

        for (var i = 0; i < items.Length; i++)
        {
            items[i] = new HashCollider("c" + i);
            Assert.IsTrue(sut.Add(items[i]));
        }

        for (var i = 0; i < items.Length; i++)
        {
            Assert.IsTrue(sut.Contains(items[i]));
            Assert.AreEqual(i, sut.IndexOf(items[i]));
        }
    }

    /// <summary>
    /// An <see cref="IEqualityComparer{T}" /> for <see cref="string" /> that delegates equality to ordinal
    /// comparison but forces every hash code to zero in order to exercise collision handling.
    /// </summary>
    private sealed class FixedHashStringComparer
        : IEqualityComparer<string>
    {

        /// <inheritdoc />
        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);

        /// <inheritdoc />
        public int GetHashCode(string obj) => 0;

    }

}
