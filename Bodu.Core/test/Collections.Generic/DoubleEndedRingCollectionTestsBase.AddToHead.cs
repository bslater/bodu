// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleEndedRingCollectionTestsBase.AddToHead.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class DoubleEndedRingCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="AddToHead(TCollection, int)"/> places the new item at the head.
    /// </summary>
    [TestMethod]
    public void AddToHead_WhenEmpty_ShouldPlaceItemAtHead()
    {
        TCollection collection = CreateCollection(3);
        AddToHead(collection, 42);

        Assert.AreEqual(1, GetCount(collection));
        Assert.AreEqual(42, PeekHead(collection));
    }

    /// <summary>
    /// Verifies that <see cref="AddToHead(TCollection, int)"/> increments <see cref="GetCount(TCollection)"/>
    /// by one for each call.
    /// </summary>
    [TestMethod]
    public void AddToHead_WhenItemsAdded_ShouldIncrementCount()
    {
        TCollection collection = CreateCollection(5);
        for (var i = 0; i < 5; i++)
        {
            AddToHead(collection, i);
            Assert.AreEqual(i + 1, GetCount(collection));
        }
    }

    /// <summary>
    /// Verifies that successive <see cref="AddToHead(TCollection, int)"/> calls reverse the insertion order
    /// when read back in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void AddToHead_WhenSpaceAvailable_ShouldReverseInsertionOrder()
    {
        TCollection collection = CreateCollection(3);
        AddToHead(collection, 1);
        AddToHead(collection, 2);
        AddToHead(collection, 3);

        CollectionAssert.AreEqual(new[] { 3, 2, 1 }, ToArray(collection));
    }

}
