// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase{T,T}.ToArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{

    /// <summary>
    /// Verifies that subsequent mutations do not affect the previously returned array.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ShouldReturnIndependentCopy()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);

        int[] copy = ToArray(collection);
        AddToTail(collection, 2);

        Assert.HasCount(1, copy);
    }
    /// <summary>
    /// Verifies that <see cref="ToArray(TCollection)"/> returns a new array of the correct length.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCollectionHasItems_ShouldReturnArrayOfCorrectLength()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);

        Assert.HasCount(2, ToArray(collection));
    }

    /// <summary>
    /// Verifies that <see cref="ToArray(TCollection)"/> reflects head-to-tail order.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCollectionHasItems_ShouldReturnHeadToTailOrder()
    {
        TCollection collection = CreateCollection(5);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, ToArray(collection));
    }

    /// <summary>
    /// Verifies that <see cref="ToArray(TCollection)"/> returns an empty array when the collection is empty.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCollectionIsEmpty_ShouldReturnEmptyArray()
    {
        TCollection collection = CreateCollection(3);

        Assert.IsEmpty(ToArray(collection));
    }

}
