// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase{T,T}.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="GetCount(TCollection)"/> resets to zero after <see cref="Clear(TCollection)"/>.
    /// </summary>
    [TestMethod]
    public void Count_AfterClear_ShouldBeZero()
    {
        TCollection collection = CreateCollection(5);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        Clear(collection);

        Assert.AreEqual(0, GetCount(collection));
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.Count"/> on the non-generic interface matches
    /// <see cref="GetCount(TCollection)"/>.
    /// </summary>
    [TestMethod]
    public void Count_OnICollectionInterface_ShouldMatchTypedCount()
    {
        TCollection collection = CreateCollection(5);
        AddToTail(collection, 1);
        AddToTail(collection, 2);

        Assert.HasCount(GetCount(collection), collection);
    }

    /// <summary>
    /// Verifies that <see cref="GetCount(TCollection)"/> tracks tail-side adds.
    /// </summary>
    [TestMethod]
    public void Count_WhenItemsAdded_ShouldReflectTotal()
    {
        TCollection collection = CreateCollection(5);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        Assert.AreEqual(3, GetCount(collection));
    }

    /// <summary>
    /// Verifies that <see cref="GetCount(TCollection)"/> tracks head-side removals.
    /// </summary>
    [TestMethod]
    public void Count_WhenItemsRemoved_ShouldReflectTotal()
    {
        TCollection collection = CreateCollection(5);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);
        _ = RemoveFromHead(collection);

        Assert.AreEqual(2, GetCount(collection));
    }
    /// <summary>
    /// Verifies that <see cref="GetCount(TCollection)"/> is zero on a freshly constructed collection.
    /// </summary>
    [TestMethod]
    public void Count_WhenNewlyConstructed_ShouldBeZero()
    {
        TCollection collection = CreateCollection(3);
        Assert.AreEqual(0, GetCount(collection));
    }

}
