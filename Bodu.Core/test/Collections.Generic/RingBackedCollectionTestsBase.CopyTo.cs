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
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        var target = new int[3];
        CopyTo(collection, target, 0);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="CopyTo(TCollection, int[], int)"/> writes at the specified offset, across
    /// a range of starting positions.
    /// </summary>
    /// <param name="offset">The destination offset to start copying at.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void CopyTo_WhenIndexIsNonZero_ShouldStartAtOffset(int offset)
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);

        var target = new int[5];
        CopyTo(collection, target, offset);

        for (var i = 0; i < 5; i++)
        {
            var expected = i == offset ? 1 : i == offset + 1 ? 2 : 0;
            Assert.AreEqual(expected, target[i], $"target[{i}] mismatch (offset={offset})");
        }
    }

    /// <summary>
    /// Verifies that <see cref="CopyTo(TCollection, int[], int)"/> handles wrapped storage correctly.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenStorageWrapped_ShouldCopyInLogicalOrder()
    {
        TCollection collection = CreateCollection(3);
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
        TCollection collection = CreateCollection(3);
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
        TCollection collection = CreateCollection(3);
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
        TCollection collection = CreateCollection(3);
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
