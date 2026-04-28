// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that iteration produces elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDequeHasItems_ShouldIterateInOrder()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);

        var collected = new List<int>();
        foreach (var v in deque)
            collected.Add(v);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, collected);
    }

    /// <summary>
    /// Verifies that mutation during enumeration invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenMutatedDuringIteration_ShouldThrowOnMoveNext()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);

        var enumerator = deque.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        deque.AddLast(3);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that growing the deque (causing a Resize) invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAutoGrowOccurs_ShouldThrowOnMoveNext()
    {
        var deque = new Deque<int>(2);
        deque.AddLast(1);
        deque.AddLast(2);

        var enumerator = deque.GetEnumerator();
        deque.AddLast(3); // triggers grow

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that the non-generic IEnumerable.GetEnumerator integrates correctly.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_NonGeneric_ShouldIterateCorrectly()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);

        var nonGeneric = ((IEnumerable)deque).GetEnumerator();
        Assert.IsTrue(nonGeneric.MoveNext());
        Assert.AreEqual(1, nonGeneric.Current);
        Assert.IsTrue(nonGeneric.MoveNext());
        Assert.AreEqual(2, nonGeneric.Current);
        Assert.IsFalse(nonGeneric.MoveNext());
    }
}
