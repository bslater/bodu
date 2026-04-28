// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.ICollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo"/> copies elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenDequeHasElements_ShouldCopyInOrder()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);
        var target = new int[3];
        ((ICollection)deque).CopyTo(target, 0);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, target);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.IsSynchronized"/> is always <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void ICollection_IsSynchronized_ShouldBeFalse()
    {
        var deque = new ArrayDeque<int>(2);
        Assert.IsFalse(((ICollection)deque).IsSynchronized);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.SyncRoot"/> is non-null and stable across calls.
    /// </summary>
    [TestMethod]
    public void ICollection_SyncRoot_ShouldBeStable()
    {
        var deque = new ArrayDeque<int>(2);
        object first = ((ICollection)deque).SyncRoot;
        object second = ((ICollection)deque).SyncRoot;
        Assert.IsNotNull(first);
        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo"/> throws when the destination is multidimensional.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayIsMultidimensional_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        var multiDim = new int[2, 2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)deque).CopyTo(multiDim, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo"/> throws when the destination element type is incompatible.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenWrongElementType_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);
        var wrongType = new string[2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ((ICollection)deque).CopyTo(wrongType, 0);
        });
    }
}
