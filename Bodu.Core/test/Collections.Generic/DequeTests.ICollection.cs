// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.ICollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo"/> copies elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenDequeHasElements_ShouldCopyInOrder()
    {
        var deque = new Deque<int>(3);
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
        var deque = new Deque<int>();
        Assert.IsFalse(((ICollection)deque).IsSynchronized);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.SyncRoot"/> is non-null and stable across calls.
    /// </summary>
    [TestMethod]
    public void ICollection_SyncRoot_ShouldBeStable()
    {
        var deque = new Deque<int>();
        object first = ((ICollection)deque).SyncRoot;
        object second = ((ICollection)deque).SyncRoot;
        Assert.IsNotNull(first);
        Assert.AreSame(first, second);
    }
}
