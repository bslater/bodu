// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleEndedRingCollectionTestsBase.Wrapping.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class DoubleEndedRingCollectionTestsBase<TTest, TCollection>
{
    /// <summary>
    /// Verifies that filling the collection alternately from both ends preserves logical head-to-tail order,
    /// across a range of capacities (the wrap point varies with capacity).
    /// </summary>
    /// <param name="capacity">The collection capacity (must be even and at least 2).</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(6)]
    [DataRow(16)]
    public void Wrapping_WhenFilledFromBothEnds_ShouldPreserveLogicalOrder(int capacity)
    {
        var collection = CreateCollection(capacity);
        int half = capacity / 2;

        // Fill the right half via tail (values [half, half+1, ..., capacity-1])
        for (int i = 0; i < half; i++)
            AddToTail(collection, half + i);

        // Fill the left half via head (values [half-1, half-2, ..., 0]; head-side prepends reverse the order)
        for (int i = 0; i < half; i++)
            AddToHead(collection, half - 1 - i);

        // Logical order should be 0, 1, ..., capacity-1
        var expected = new int[capacity];
        for (int i = 0; i < capacity; i++)
            expected[i] = i;

        CollectionAssert.AreEqual(expected, ToArray(collection));
    }

    /// <summary>
    /// Verifies that draining from both ends in interleaved fashion empties the collection cleanly.
    /// </summary>
    [TestMethod]
    public void Wrapping_WhenDrainedFromBothEndsInterleaved_ShouldEmptyToZero()
    {
        var collection = CreateCollection(6);
        AddToTail(collection, 3);
        AddToTail(collection, 4);
        AddToTail(collection, 5);
        AddToHead(collection, 2);
        AddToHead(collection, 1);
        AddToHead(collection, 0);

        Assert.AreEqual(0, RemoveFromHead(collection));
        Assert.AreEqual(5, RemoveFromTail(collection));
        Assert.AreEqual(1, RemoveFromHead(collection));
        Assert.AreEqual(4, RemoveFromTail(collection));
        Assert.AreEqual(2, RemoveFromHead(collection));
        Assert.AreEqual(3, RemoveFromTail(collection));

        Assert.IsTrue(GetIsEmpty(collection));
    }

    /// <summary>
    /// Verifies that <see cref="AddToHead(TCollection, int)"/> during iteration invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenAddToHeadCalledDuringIteration_ShouldThrowOnMoveNext()
    {
        var collection = CreateCollection(5);
        AddToTail(collection, 1);
        AddToTail(collection, 2);

        var enumerator = collection.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        AddToHead(collection, 0);

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="RemoveFromTail(TCollection)"/> during iteration invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenRemoveFromTailCalledDuringIteration_ShouldThrowOnMoveNext()
    {
        var collection = CreateCollection(5);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        var enumerator = collection.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = RemoveFromTail(collection);

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }
}
