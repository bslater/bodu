// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.TryAddToTail.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="TryAddToTail(TCollection, int)"/> always succeeds when the collection has
    /// not yet reached its initial capacity, regardless of fixed-vs-growable semantics.
    /// </summary>
    [TestMethod]
    public void TryAddToTail_WhenBelowInitialCapacity_ShouldReturnTrue()
    {
        TCollection collection = CreateCollection(5);
        for (var i = 0; i < 5; i++)
            Assert.IsTrue(TryAddToTail(collection, i));
    }
    /// <summary>
    /// Verifies that <see cref="TryAddToTail(TCollection, int)"/> returns <see langword="true"/> and stores
    /// the element when capacity is available.
    /// </summary>
    [TestMethod]
    public void TryAddToTail_WhenSpaceAvailable_ShouldReturnTrueAndStore()
    {
        TCollection collection = CreateCollection(3);
        Assert.IsTrue(TryAddToTail(collection, 1));
        Assert.AreEqual(1, PeekHead(collection));
        Assert.AreEqual(1, GetCount(collection));
    }

}
