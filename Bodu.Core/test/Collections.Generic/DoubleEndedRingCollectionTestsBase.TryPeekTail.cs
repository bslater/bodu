// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleEndedRingCollectionTestsBase.TryPeekTail.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class DoubleEndedRingCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="TryPeekTail(TCollection, out int)"/> returns <see langword="false"/> and
    /// the default value when the collection is empty.
    /// </summary>
    [TestMethod]
    public void TryPeekTail_WhenEmpty_ShouldReturnFalseAndDefault()
    {
        TCollection collection = CreateCollection(3);
        Assert.IsFalse(TryPeekTail(collection, out var item));
        Assert.AreEqual(default, item);
    }
    /// <summary>
    /// Verifies that <see cref="TryPeekTail(TCollection, out int)"/> succeeds when the collection has items.
    /// </summary>
    [TestMethod]
    public void TryPeekTail_WhenItemsPresent_ShouldReturnTrueAndTail()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 7);
        AddToTail(collection, 8);

        Assert.IsTrue(TryPeekTail(collection, out var item));
        Assert.AreEqual(8, item);
        Assert.AreEqual(2, GetCount(collection));
    }

}
