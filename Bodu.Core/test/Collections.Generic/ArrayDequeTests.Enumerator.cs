// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that iterating produces elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDequeHasItems_ShouldIterateInHeadToTailOrder()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);

        var collected = new List<int>();
        foreach (var v in deque)
            collected.Add(v);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, collected);
    }

    /// <summary>
    /// Verifies that the enumerator throws when the deque is mutated during enumeration.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenMutatedDuringIteration_ShouldThrowOnMoveNext()
    {
        var deque = new ArrayDeque<int>(3);
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
    /// Verifies that the enumerator throws on <c>Reset</c> after mutation.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenMutatedThenReset_ShouldThrowOnReset()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        var enumerator = deque.GetEnumerator();
        deque.AddLast(2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            enumerator.Reset();
        });
    }

    /// <summary>
    /// Verifies that <c>Current</c> throws before <c>MoveNext</c> is called.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCurrentAccessedBeforeMoveNext_ShouldThrow()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        var enumerator = deque.GetEnumerator();
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.Current;
        });
    }

    /// <summary>
    /// Verifies that the non-generic IEnumerable.GetEnumerator integrates correctly.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_NonGeneric_ShouldIterateCorrectly()
    {
        var deque = new ArrayDeque<int>(3);
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
