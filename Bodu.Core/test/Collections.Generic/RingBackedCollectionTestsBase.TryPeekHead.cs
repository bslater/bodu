// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.TryPeekHead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="TryPeekHead(TCollection, out int)"/> returns <see langword="false"/> and
    /// the default value when the collection is empty.
    /// </summary>
    [TestMethod]
    public void TryPeekHead_WhenEmpty_ShouldReturnFalseAndDefault()
    {
        TCollection collection = CreateCollection(3);
        Assert.IsFalse(TryPeekHead(collection, out var item));
        Assert.AreEqual(default, item);
    }
    /// <summary>
    /// Verifies that <see cref="TryPeekHead(TCollection, out int)"/> succeeds when the collection has items.
    /// </summary>
    [TestMethod]
    public void TryPeekHead_WhenItemsPresent_ShouldReturnTrueAndHead()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 7);

        Assert.IsTrue(TryPeekHead(collection, out var item));
        Assert.AreEqual(7, item);
        Assert.AreEqual(1, GetCount(collection));
    }

}
