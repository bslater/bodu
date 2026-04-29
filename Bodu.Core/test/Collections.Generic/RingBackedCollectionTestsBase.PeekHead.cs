// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.PeekHead.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{
    /// <summary>
    /// Verifies that <see cref="PeekHead(TCollection)"/> returns the head element without modifying the count.
    /// </summary>
    [TestMethod]
    public void PeekHead_WhenItemsPresent_ShouldReturnHeadWithoutRemoving()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);

        Assert.AreEqual(1, PeekHead(collection));
        Assert.AreEqual(2, GetCount(collection));
    }

    /// <summary>
    /// Verifies that <see cref="PeekHead(TCollection)"/> reflects the new head after a previous head has
    /// been removed.
    /// </summary>
    [TestMethod]
    public void PeekHead_AfterRemoveFromHead_ShouldReturnNewHead()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        _ = RemoveFromHead(collection);

        Assert.AreEqual(2, PeekHead(collection));
    }

    /// <summary>
    /// Verifies that <see cref="PeekHead(TCollection)"/> throws <see cref="InvalidOperationException"/> when
    /// the collection is empty.
    /// </summary>
    [TestMethod]
    public void PeekHead_WhenEmpty_ShouldThrowExactly()
    {
        var collection = CreateCollection(3);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = PeekHead(collection);
        });
    }
}
