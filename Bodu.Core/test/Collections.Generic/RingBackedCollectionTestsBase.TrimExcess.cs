// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.TrimExcess.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="TrimExcess(TCollection)"/> reduces capacity to match
    /// <see cref="GetCount(TCollection)"/> when the collection contains items.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCollectionHasItems_ShouldShrinkCapacityToCount()
    {
        TCollection collection = CreateCollection(50);
        AddToTail(collection, 1);
        AddToTail(collection, 2);

        TrimExcess(collection);

        Assert.AreEqual(2, GetCapacity(collection));
        Assert.AreEqual(2, GetCount(collection));
    }

    /// <summary>
    /// Verifies that <see cref="TrimExcess(TCollection)"/> on an empty collection shrinks the capacity to one.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCollectionIsEmpty_ShouldShrinkToOne()
    {
        TCollection collection = CreateCollection(50);
        TrimExcess(collection);

        Assert.AreEqual(1, GetCapacity(collection));
    }

    /// <summary>
    /// Verifies that <see cref="TrimExcess(TCollection)"/> preserves logical head-to-tail order even when
    /// the storage was wrapped before trimming.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenStorageWrapped_ShouldPreserveOrder()
    {
        TCollection collection = CreateCollection(4);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);
        AddToTail(collection, 4);
        _ = RemoveFromHead(collection);
        _ = RemoveFromHead(collection);
        AddToTail(collection, 5);
        AddToTail(collection, 6);

        TrimExcess(collection);

        CollectionAssert.AreEqual(new[] { 3, 4, 5, 6 }, ToArray(collection));
    }

}
