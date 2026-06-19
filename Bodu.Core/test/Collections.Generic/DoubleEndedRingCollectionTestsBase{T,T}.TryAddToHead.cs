// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleEndedRingCollectionTestsBase{T,T}.TryAddToHead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        TCollection collection = CreateCollection(3);
        Assert.IsTrue(TryAddToHead(collection, 1));
        Assert.AreEqual(1, PeekHead(collection));
        Assert.AreEqual(1, GetCount(collection));
    }

}
