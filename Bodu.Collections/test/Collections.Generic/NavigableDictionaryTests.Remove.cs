// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that removing a present key returns <see langword="true" /> and decrements the count.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyIsPresent_ShouldReturnTrueAndDecrementCount()
    {
        var sut = CreateDictionary(1, 2, 3);

        bool removed = sut.Remove(2);

        Assert.IsTrue(removed);
        Assert.AreEqual(2, sut.Count);
        Assert.IsFalse(sut.ContainsKey(2));
    }

    /// <summary>
    /// Verifies that removing an absent key returns <see langword="false" /> and leaves the dictionary unchanged.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyIsAbsent_ShouldReturnFalseAndLeaveDictionaryUnchanged()
    {
        var sut = CreateDictionary(1, 2, 3);

        bool removed = sut.Remove(99);

        Assert.IsFalse(removed);
        Assert.AreEqual(3, sut.Count);
    }

    /// <summary>
    /// Verifies that the value-returning overload surfaces the removed value for a present key.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyIsPresent_ForValueReturningOverload_ShouldReturnRemovedValue()
    {
        var sut = CreateDictionary(1, 2, 3);

        bool removed = sut.Remove(2, out int value);

        Assert.IsTrue(removed);
        Assert.AreEqual(200, value);
        Assert.IsFalse(sut.ContainsKey(2));
    }

    /// <summary>
    /// Verifies that the value-returning overload yields the default value for an absent key.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyIsAbsent_ForValueReturningOverload_ShouldReturnFalseAndDefaultValue()
    {
        var sut = CreateDictionary(1, 2, 3);

        bool removed = sut.Remove(99, out int value);

        Assert.IsFalse(removed);
        Assert.AreEqual(0, value);
        Assert.AreEqual(3, sut.Count);
    }

    /// <summary>
    /// Verifies that removing a leaf node preserves the remaining key order.
    /// </summary>
    [TestMethod]
    public void Remove_WhenTargetIsLeaf_ShouldPreserveOrder()
    {
        // 2 becomes the root with leaves 1 and 3; removing 1 removes a leaf.
        var sut = CreateDictionary(2, 1, 3);

        Assert.IsTrue(sut.Remove(1));

        CollectionAssert.AreEqual(new[] { 2, 3 }, sut.Select(e => e.Key).ToList());
    }

    /// <summary>
    /// Verifies that removing the root of the tree preserves the remaining key order.
    /// </summary>
    [TestMethod]
    public void Remove_WhenTargetIsRoot_ShouldPreserveOrder()
    {
        var sut = CreateDictionary(2, 1, 3);

        Assert.IsTrue(sut.Remove(2));

        CollectionAssert.AreEqual(new[] { 1, 3 }, sut.Select(e => e.Key).ToList());
    }

    /// <summary>
    /// Verifies that removing an interior node with two children (replaced by its in-order successor) preserves the
    /// remaining key order, rank integrity, and each surviving key's value.
    /// </summary>
    [TestMethod]
    public void Remove_WhenTargetHasTwoChildren_ShouldPreserveOrderRanksAndValues()
    {
        var sut = CreateDictionary(50, 25, 75, 10, 30, 60, 90, 27, 35);

        // 25 has two children (10 and 30 with its own children); its successor is 27.
        Assert.IsTrue(sut.Remove(25));

        CollectionAssert.AreEqual(new[] { 10, 27, 30, 35, 50, 60, 75, 90 }, sut.Select(e => e.Key).ToList());
        for (int rank = 0; rank < sut.Count; rank++)
        {
            KeyValuePair<int, int> entry = sut.GetAt(rank);
            Assert.AreEqual(rank, sut.IndexOfKey(entry.Key));
            Assert.AreEqual(entry.Key * 100, entry.Value, $"Value pairing broke for key {entry.Key}.");
        }
    }

    /// <summary>
    /// Verifies that removing a node with a single child splices the child into place.
    /// </summary>
    [TestMethod]
    public void Remove_WhenTargetHasSingleChild_ShouldSpliceChild()
    {
        var sut = CreateDictionary(10, 5, 20, 15);

        // 20 has the single (red) child 15.
        Assert.IsTrue(sut.Remove(20));

        CollectionAssert.AreEqual(new[] { 5, 10, 15 }, sut.Select(e => e.Key).ToList());
    }

    /// <summary>
    /// Verifies that removing the only entry empties the dictionary.
    /// </summary>
    [TestMethod]
    public void Remove_WhenLastEntryRemoved_ShouldEmptyDictionary()
    {
        var sut = CreateDictionary(7);

        Assert.IsTrue(sut.Remove(7));

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.TryGetMinEntry(out _));
    }

    /// <summary>
    /// Verifies that draining the dictionary key by key — ascending, descending, and middle-out — always keeps the
    /// remainder sorted, rank-consistent, and value-faithful, exercising every red-black deletion path.
    /// </summary>
    [TestMethod]
    public void Remove_WhenDrainingInTargetedOrders_ShouldStayConsistent()
    {
        int[][] removalOrders =
        [
            Enumerable.Range(0, 33).ToArray(),
            Enumerable.Range(0, 33).Reverse().ToArray(),
            Enumerable.Range(0, 33).OrderBy(i => Math.Abs(i - 16)).ToArray(),
        ];

        foreach (int[] order in removalOrders)
        {
            var sut = new NavigableDictionary<int, int>(
                Enumerable.Range(0, 33).Select(key => new KeyValuePair<int, int>(key, key * 100)));
            var expected = new List<int>(Enumerable.Range(0, 33));

            foreach (int key in order)
            {
                Assert.IsTrue(sut.Remove(key, out int value));
                Assert.AreEqual(key * 100, value);
                expected.Remove(key);

                CollectionAssert.AreEqual(expected, sut.Select(e => e.Key).ToList());
                for (int rank = 0; rank < sut.Count; rank++)
                {
                    Assert.AreEqual(expected[rank], sut.GetAt(rank).Key);
                    Assert.AreEqual(expected[rank] * 100, sut.GetAt(rank).Value);
                }
            }
        }
    }

    /// <summary>
    /// Verifies that removing a <see langword="null" /> key throws <see cref="ArgumentNullException" /> for both
    /// overloads.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.Remove(null!);
        }, "key");

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.Remove(null!, out _);
        }, "key");
    }
}
