// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleEndedRingCollectionTestsBase.Wrapping.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class DoubleEndedRingCollectionTestsBase<TTest, TCollection>
{
    /// <summary>
    /// Verifies that filling the collection alternately from both ends preserves logical head-to-tail order.
    /// </summary>
    [TestMethod]
    public void Wrapping_WhenFilledFromBothEnds_ShouldPreserveLogicalOrder()
    {
        var collection = CreateCollection(6);
        AddToTail(collection, 3);
        AddToTail(collection, 4);
        AddToTail(collection, 5);
        AddToHead(collection, 2);
        AddToHead(collection, 1);
        AddToHead(collection, 0);

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, ToArray(collection));
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
