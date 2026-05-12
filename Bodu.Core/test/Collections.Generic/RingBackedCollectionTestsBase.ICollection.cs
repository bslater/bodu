// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTestsBase.ICollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public abstract partial class RingBackedCollectionTestsBase<TTest, TCollection>
{
    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo"/> copies elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenCollectionHasElements_ShouldCopyInOrder()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        var target = new int[3];
        ((ICollection)collection).CopyTo(target, 0);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.IsSynchronized"/> is always <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void ICollection_IsSynchronized_ShouldBeFalse()
    {
        TCollection collection = CreateCollection(3);
        Assert.IsFalse(((ICollection)collection).IsSynchronized);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.SyncRoot"/> is non-null and stable across repeat calls.
    /// </summary>
    [TestMethod]
    public void ICollection_SyncRoot_ShouldBeStable()
    {
        TCollection collection = CreateCollection(3);
        var first = ((ICollection)collection).SyncRoot;
        var second = ((ICollection)collection).SyncRoot;

        Assert.IsNotNull(first);
        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo"/> throws when the destination is multidimensional.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsMultidimensional_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);

        var multiDim = new int[2, 2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)collection).CopyTo(multiDim, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo"/> throws when the destination element type is incompatible.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenWrongElementType_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);

        var wrongType = new string[2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)collection).CopyTo(wrongType, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo"/> throws when the destination is too small.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayTooSmall_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);
        AddToTail(collection, 2);
        AddToTail(collection, 3);

        var target = new int[2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)collection).CopyTo(target, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo"/> throws <see cref="ArgumentNullException"/> when the
    /// destination is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsNull_ShouldThrowExactly()
    {
        TCollection collection = CreateCollection(3);
        AddToTail(collection, 1);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ((ICollection)collection).CopyTo(null!, 0);
        });
    }
}
