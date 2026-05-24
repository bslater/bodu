// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleEndedRingCollectionTestsBase.TryRemoveFromTail.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class DoubleEndedRingCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="TryRemoveFromTail(TCollection, out int)"/> returns <see langword="false"/>
    /// and the default value when the collection is empty.
    /// </summary>
    [TestMethod]
    public void TryRemoveFromTail_WhenEmpty_ShouldReturnFalseAndDefault()
    {
        TCollection collection = CreateCollection(3);
        Assert.IsFalse(TryRemoveFromTail(collection, out var item));
        Assert.AreEqual(default, item);
    }
    /// <summary>
    /// Verifies that <see cref="TryRemoveFromTail(TCollection, out int)"/> returns the tail element and
    /// <see langword="true"/> when items are present.
    /// </summary>
    [TestMethod]
    public void TryRemoveFromTail_WhenItemsPresent_ShouldReturnTrueAndTail()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 10);

        Assert.IsTrue(TryRemoveFromTail(collection, out var item));
        Assert.AreEqual(10, item);
        Assert.IsTrue(GetIsEmpty(collection));
    }

}
