// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.TryPeekHead.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{
    /// <summary>
    /// Verifies that <see cref="TryPeekHead(TCollection, out int)"/> succeeds when the collection has items.
    /// </summary>
    [TestMethod]
    public void TryPeekHead_WhenItemsPresent_ShouldReturnTrueAndHead()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 7);

        Assert.IsTrue(TryPeekHead(collection, out int item));
        Assert.AreEqual(7, item);
        Assert.AreEqual(1, GetCount(collection));
    }

    /// <summary>
    /// Verifies that <see cref="TryPeekHead(TCollection, out int)"/> returns <see langword="false"/> and
    /// the default value when the collection is empty.
    /// </summary>
    [TestMethod]
    public void TryPeekHead_WhenEmpty_ShouldReturnFalseAndDefault()
    {
        var collection = CreateCollection(3);
        Assert.IsFalse(TryPeekHead(collection, out int item));
        Assert.AreEqual(default, item);
    }
}
