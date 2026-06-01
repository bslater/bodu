// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.TryRemoveFromHead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="TryRemoveFromHead(TCollection, out int)"/> does not modify state when it
    /// returns <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void TryRemoveFromHead_WhenEmpty_ShouldNotModifyState()
    {
        TCollection collection = CreateCollection(3);
        var capacityBefore = GetCapacity(collection);

        _ = TryRemoveFromHead(collection, out _);

        Assert.IsTrue(GetIsEmpty(collection));
        Assert.AreEqual(capacityBefore, GetCapacity(collection));
    }

    /// <summary>
    /// Verifies that <see cref="TryRemoveFromHead(TCollection, out int)"/> returns <see langword="false"/>
    /// and the default value when the collection is empty.
    /// </summary>
    [TestMethod]
    public void TryRemoveFromHead_WhenEmpty_ShouldReturnFalseAndDefault()
    {
        TCollection collection = CreateCollection(3);
        Assert.IsFalse(TryRemoveFromHead(collection, out var item));
        Assert.AreEqual(default, item);
    }
    /// <summary>
    /// Verifies that <see cref="TryRemoveFromHead(TCollection, out int)"/> returns the head element and
    /// <see langword="true"/> when items are present.
    /// </summary>
    [TestMethod]
    public void TryRemoveFromHead_WhenItemsPresent_ShouldReturnTrueAndHead()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 10);

        Assert.IsTrue(TryRemoveFromHead(collection, out var item));
        Assert.AreEqual(10, item);
        Assert.IsTrue(GetIsEmpty(collection));
    }

}
