// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleEndedRingCollectionTestsBase.TryAddToHead.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class DoubleEndedRingCollectionTestsBase<TTest, TCollection>
{
    /// <summary>
    /// Verifies that <see cref="TryAddToHead(TCollection, int)"/> returns <see langword="true"/> and stores
    /// the element when capacity is available.
    /// </summary>
    [TestMethod]
    public void TryAddToHead_WhenSpaceAvailable_ShouldReturnTrueAndStore()
    {
        var collection = CreateCollection(3);
        Assert.IsTrue(TryAddToHead(collection, 1));
        Assert.AreEqual(1, PeekHead(collection));
        Assert.AreEqual(1, GetCount(collection));
    }
}
