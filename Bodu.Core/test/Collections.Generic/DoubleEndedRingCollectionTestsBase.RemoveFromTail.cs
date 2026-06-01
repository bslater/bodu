// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleEndedRingCollectionTestsBase.RemoveFromTail.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class DoubleEndedRingCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="RemoveFromTail(TCollection)"/> drains the collection to empty when called
    /// once for every element.
    /// </summary>
    [TestMethod]
    public void RemoveFromTail_WhenCalledRepeatedly_ShouldDrainToEmpty()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        Assert.AreEqual(3, RemoveFromTail(collection));
        Assert.AreEqual(2, RemoveFromTail(collection));
        Assert.AreEqual(1, RemoveFromTail(collection));
        Assert.IsTrue(GetIsEmpty(collection));
    }

    /// <summary>
    /// Verifies that <see cref="RemoveFromTail(TCollection)"/> throws when the collection is empty.
    /// </summary>
    [TestMethod]
    public void RemoveFromTail_WhenEmpty_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(3);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = RemoveFromTail(collection);
        });
    }
    /// <summary>
    /// Verifies that <see cref="RemoveFromTail(TCollection)"/> returns the most-recently-added element
    /// (LIFO) when invoked after a series of tail-side adds.
    /// </summary>
    [TestMethod]
    public void RemoveFromTail_WhenItemsPresent_ShouldReturnTailAndDecrementCount()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        Assert.AreEqual(3, RemoveFromTail(collection));
        Assert.AreEqual(2, GetCount(collection));
        Assert.AreEqual(2, PeekTail(collection));
    }

}
