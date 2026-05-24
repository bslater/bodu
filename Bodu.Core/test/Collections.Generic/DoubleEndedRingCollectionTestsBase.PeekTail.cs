// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleEndedRingCollectionTestsBase.PeekTail.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class DoubleEndedRingCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="PeekTail(TCollection)"/> reflects the new tail after the previous tail has
    /// been removed.
    /// </summary>
    [TestMethod]
    public void PeekTail_AfterRemoveFromTail_ShouldReturnNewTail()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        _ = RemoveFromTail(collection);

        Assert.AreEqual(1, PeekTail(collection));
    }

    /// <summary>
    /// Verifies that <see cref="PeekTail(TCollection)"/> throws <see cref="InvalidOperationException"/>
    /// when the collection is empty.
    /// </summary>
    [TestMethod]
    public void PeekTail_WhenEmpty_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(3);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = PeekTail(collection);
        });
    }
    /// <summary>
    /// Verifies that <see cref="PeekTail(TCollection)"/> returns the tail element without removing it.
    /// </summary>
    [TestMethod]
    public void PeekTail_WhenItemsPresent_ShouldReturnTailWithoutRemoving()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);

        Assert.AreEqual(2, PeekTail(collection));
        Assert.AreEqual(2, GetCount(collection));
    }

}
