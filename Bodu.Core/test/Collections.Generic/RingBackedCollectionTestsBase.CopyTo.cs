// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.CopyTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{
    /// <summary>
    /// Verifies that <see cref="CopyTo(TCollection, int[], int)"/> copies elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCollectionHasElements_ShouldCopyInOrder()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        var target = new int[3];
        CopyTo(collection, target, 0);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="CopyTo(TCollection, int[], int)"/> writes at the specified offset.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsNonZero_ShouldStartAtOffset()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);

        var target = new int[5];
        CopyTo(collection, target, 2);

        CollectionAssert.AreEqual(new[] { 0, 0, 1, 2, 0 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="CopyTo(TCollection, int[], int)"/> handles wrapped storage correctly.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenStorageWrapped_ShouldCopyInLogicalOrder()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);
        _ = RemoveFromHead(collection);
        AddToTail(collection, 4);

        var target = new int[3];
        CopyTo(collection, target, 0);

        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="CopyTo(TCollection, int[], int)"/> throws when given a <see langword="null"/> array.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CopyTo(collection, null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CopyTo(TCollection, int[], int)"/> throws when the index is negative.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsNegative_ShouldThrowExactly()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);

        var target = new int[3];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CopyTo(collection, target, -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CopyTo(TCollection, int[], int)"/> throws when the destination is too small.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayTooSmall_ShouldThrowExactly()
    {
        var collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        var target = new int[2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CopyTo(collection, target, 0);
        });
    }
}
