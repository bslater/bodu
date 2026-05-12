// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.AddToTail.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{
    /// <summary>
    /// Verifies that <see cref="AddToTail(TCollection, int)"/> places a new element at the tail end of an
    /// otherwise empty collection so that it becomes the head when read back.
    /// </summary>
    [TestMethod]
    public void AddToTail_WhenEmpty_ShouldPlaceItemAtHead()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 42);

        Assert.AreEqual(1, GetCount(collection));
        Assert.AreEqual(42, PeekHead(collection));
    }

    /// <summary>
    /// Verifies that successive <see cref="AddToTail(TCollection, int)"/> calls preserve insertion order
    /// when read back from head to tail.
    /// </summary>
    [TestMethod]
    public void AddToTail_WhenSpaceAvailable_ShouldAppendInInsertionOrder()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, ToArray(collection));
    }

    /// <summary>
    /// Verifies that <see cref="AddToTail(TCollection, int)"/> increments <see cref="GetCount(TCollection)"/>
    /// by one for each call, exercised across multiple counts.
    /// </summary>
    /// <param name="itemCount">The number of items to add.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(8)]
    [DataRow(64)]
    public void AddToTail_WhenItemsAdded_ShouldIncrementCount(int itemCount)
    {
        TCollection collection = CreateCollection(Math.Max(itemCount, 1));
        for (var i = 0; i < itemCount; i++)
        {
            AddToTail(collection, i);
            Assert.AreEqual(i + 1, GetCount(collection));
        }
    }

    /// <summary>
    /// Verifies that <see cref="AddToTail(TCollection, int)"/> accepts duplicate values without merging or de-duplicating.
    /// </summary>
    [TestMethod]
    public void AddToTail_WhenDuplicatesProvided_ShouldStoreAll()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 7);
        AddToTail(collection, 7);
        AddToTail(collection, 7);

        CollectionAssert.AreEqual(new[] { 7, 7, 7 }, ToArray(collection));
    }
}
