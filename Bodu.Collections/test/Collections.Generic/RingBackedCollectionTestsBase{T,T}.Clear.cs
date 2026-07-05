// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase{T,T}.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that <see cref="Clear(TCollection)"/> on an already-empty collection is a no-op.
    /// </summary>
    [TestMethod]
    public void Clear_WhenAlreadyEmpty_ShouldRemainEmpty()
    {
        TCollection collection = CreateCollection(3);
        Clear(collection);

        Assert.AreEqual(0, GetCount(collection));
    }

    /// <summary>
    /// Verifies that <see cref="Clear(TCollection)"/> permits subsequent operations.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldAllowSubsequentOperations()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        Clear(collection);
        AddToTail(collection, 99);

        Assert.AreEqual(99, PeekHead(collection));
    }
    /// <summary>
    /// Verifies that <see cref="Clear(TCollection)"/> resets <see cref="GetCount(TCollection)"/> to zero.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCollectionHasItems_ShouldResetCount()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        Clear(collection);

        Assert.AreEqual(0, GetCount(collection));
        Assert.IsTrue(GetIsEmpty(collection));
    }

}
