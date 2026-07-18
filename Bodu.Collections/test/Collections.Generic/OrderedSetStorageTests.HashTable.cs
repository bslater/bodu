// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageTests.HashTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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

        for (int i = 0; i < 1000; i++)
            sut.Add(i);

        for (int i = 0; i < 1000; i += 2)
            Assert.IsTrue(sut.Remove(i));

        Assert.AreEqual(500, sut.Count);
        for (int i = 0; i < sut.Count; i++)
        {
            int item = sut.GetAt(i);
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
    /// <summary>
    /// Verifies that removing a single element by index performs O(1) hash-table maintenance — it must not rehash
    /// every stored element through <see cref="IEqualityComparer{T}.GetHashCode" />.
    /// </summary>
    [TestMethod]
    public void HashTable_WhenRemoveAtSingleElement_ShouldInvokeGetHashCodeConstantTimes()
    {
        var comparer = new CountingIntComparer();
        var sut = new OrderedSetStorage<int>(512, comparer);
        for (int i = 0; i < 200; i++)
            sut.Add(i);

        int before = comparer.GetHashCodeCalls;
        sut.RemoveAt(100);
        int calls = comparer.GetHashCodeCalls - before;

        Assert.IsTrue(calls <= 2, $"RemoveAt invoked GetHashCode {calls} times; expected O(1), not O(n).");
        Assert.AreEqual(199, sut.Count);
        Assert.IsFalse(sut.Contains(100));
        for (int i = 0; i < sut.Count; i++)
            Assert.AreEqual(i, sut.IndexOf(sut.GetAt(i)));
    }

    /// <summary>
    /// Verifies that replacing a single element performs a two-node bucket fix-up — it must not rehash every stored
    /// element through <see cref="IEqualityComparer{T}.GetHashCode" />.
    /// </summary>
    [TestMethod]
    public void HashTable_WhenReplaceAtSingleElement_ShouldInvokeGetHashCodeConstantTimes()
    {
        var comparer = new CountingIntComparer();
        var sut = new OrderedSetStorage<int>(512, comparer);
        for (int i = 0; i < 200; i++)
            sut.Add(i);

        int before = comparer.GetHashCodeCalls;
        sut.ReplaceAt(100, 5000);
        int calls = comparer.GetHashCodeCalls - before;

        Assert.IsTrue(calls <= 3, $"ReplaceAt invoked GetHashCode {calls} times; expected O(1), not O(n).");
        Assert.AreEqual(5000, sut.GetAt(100));
        Assert.IsFalse(sut.Contains(100));
        Assert.AreEqual(100, sut.IndexOf(5000));
    }

    /// <summary>
    /// Verifies that moving and inserting single elements perform O(1) hash-table maintenance — neither operation
    /// may rehash every stored element through <see cref="IEqualityComparer{T}.GetHashCode" />.
    /// </summary>
    [TestMethod]
    public void HashTable_WhenMoveAndInsertSingleElement_ShouldInvokeGetHashCodeConstantTimes()
    {
        var comparer = new CountingIntComparer();
        var sut = new OrderedSetStorage<int>(512, comparer);
        for (int i = 0; i < 200; i++)
            sut.Add(i);

        int before = comparer.GetHashCodeCalls;
        sut.Move(10, 150);
        int moveCalls = comparer.GetHashCodeCalls - before;

        before = comparer.GetHashCodeCalls;
        Assert.IsTrue(sut.TryInsert(50, 9000));
        int insertCalls = comparer.GetHashCodeCalls - before;

        Assert.IsTrue(moveCalls <= 3, $"Move invoked GetHashCode {moveCalls} times; expected O(1), not O(n).");
        Assert.IsTrue(insertCalls <= 3, $"TryInsert invoked GetHashCode {insertCalls} times; expected O(1), not O(n).");
        Assert.AreEqual(151, sut.IndexOf(10)); // moved to 150, then shifted up once by the insert at 50
        Assert.AreEqual(50, sut.IndexOf(9000));
        for (int i = 0; i < sut.Count; i++)
            Assert.AreEqual(i, sut.IndexOf(sut.GetAt(i)));
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

        for (int i = 0; i < size; i++)
            Assert.IsTrue(sut.Add(i));

        Assert.AreEqual(size, sut.Count);
        for (int i = 0; i < size; i++)
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

        for (int i = 0; i < items.Length; i++)
        {
            items[i] = new HashCollider("c" + i);
            Assert.IsTrue(sut.Add(items[i]));
        }

        for (int i = 0; i < items.Length; i++)
        {
            Assert.IsTrue(sut.Contains(items[i]));
            Assert.AreEqual(i, sut.IndexOf(items[i]));
        }
    }

    /// <summary>
    /// An <see cref="IEqualityComparer{T}" /> for <see cref="int" /> that counts
    /// <see cref="IEqualityComparer{T}.GetHashCode" /> invocations, allowing tests to assert the hash-table
    /// maintenance cost of individual operations.
    /// </summary>
    private sealed class CountingIntComparer
        : IEqualityComparer<int>
    {

        /// <summary>
        /// Gets the number of <see cref="GetHashCode(int)" /> invocations observed so far.
        /// </summary>
        /// <value>The cumulative invocation count.</value>
        public int GetHashCodeCalls { get; private set; }

        /// <inheritdoc />
        public bool Equals(int x, int y) => x == y;

        /// <inheritdoc />
        public int GetHashCode(int obj)
        {
            GetHashCodeCalls++;
            return obj;
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
