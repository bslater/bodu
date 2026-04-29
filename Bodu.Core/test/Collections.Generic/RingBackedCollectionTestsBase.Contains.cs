// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.Contains.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{
    /// <summary>
    /// Verifies that <see cref="Contains(TCollection, int)"/> returns <see langword="true"/> when the item is present.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsPresent_ShouldReturnTrue()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);

        Assert.IsTrue(Contains(collection, 2));
    }

    /// <summary>
    /// Verifies that <see cref="Contains(TCollection, int)"/> returns <see langword="false"/> when the item is absent.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsAbsent_ShouldReturnFalse()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);

        Assert.IsFalse(Contains(collection, 99));
    }

    /// <summary>
    /// Verifies that <see cref="Contains(TCollection, int)"/> returns <see langword="false"/> for an empty collection.
    /// </summary>
    [TestMethod]
    public void Contains_WhenCollectionIsEmpty_ShouldReturnFalse()
    {
        var collection = CreateCollection(3);
        Assert.IsFalse(Contains(collection, 0));
    }

    /// <summary>
    /// Verifies that <see cref="Contains(TCollection, int)"/> finds elements in the wrapped second segment.
    /// </summary>
    [TestMethod]
    public void Contains_WhenStorageWrapped_ShouldFindItemInSecondSegment()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);
        _ = RemoveFromHead(collection);
        AddToTail(collection, 4); // wraps

        Assert.IsTrue(Contains(collection, 4));
        Assert.IsTrue(Contains(collection, 2));
        Assert.IsFalse(Contains(collection, 1));
    }
}
