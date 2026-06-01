// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.RemoveFromHead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="RemoveFromHead(TCollection)"/> drains the collection to empty when called
    /// once for every element.
    /// </summary>
    [TestMethod]
    public void RemoveFromHead_WhenCalledRepeatedly_ShouldDrainToEmpty()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        Assert.AreEqual(1, RemoveFromHead(collection));
        Assert.AreEqual(2, RemoveFromHead(collection));
        Assert.AreEqual(3, RemoveFromHead(collection));
        Assert.IsTrue(GetIsEmpty(collection));
    }

    /// <summary>
    /// Verifies that <see cref="RemoveFromHead(TCollection)"/> throws <see cref="InvalidOperationException"/>
    /// when invoked on an empty collection.
    /// </summary>
    [TestMethod]
    public void RemoveFromHead_WhenEmpty_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(3);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = RemoveFromHead(collection);
        });
    }

    /// <summary>
    /// Verifies that adding then removing through several full cycles preserves FIFO order across the
    /// internal ring boundary.
    /// </summary>
    [TestMethod]
    public void RemoveFromHead_WhenInterleavedWithAdds_ShouldRetainFifoOrder()
    {
        TCollection collection = CreateCollection(4);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        Assert.AreEqual(1, RemoveFromHead(collection));
        AddToTail(collection, 3);
        AddToTail(collection, 4);
        Assert.AreEqual(2, RemoveFromHead(collection));
        AddToTail(collection, 5);

        CollectionAssert.AreEqual(new[] { 3, 4, 5 }, ToArray(collection));
    }
    /// <summary>
    /// Verifies that <see cref="RemoveFromHead(TCollection)"/> returns the oldest element and decrements
    /// <see cref="GetCount(TCollection)"/> by one.
    /// </summary>
    [TestMethod]
    public void RemoveFromHead_WhenItemsPresent_ShouldReturnOldestAndDecrementCount()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        Assert.AreEqual(1, RemoveFromHead(collection));
        Assert.AreEqual(2, GetCount(collection));
        Assert.AreEqual(2, PeekHead(collection));
    }

}
