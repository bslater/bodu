// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.Enumerator.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that iterating produces elements in head-to-tail order.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDequeHasItems_ShouldIterateInHeadToTailOrder()
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
    /// Verifies that the enumerator throws when the deque is mutated during enumeration via <see cref="ArrayDeque{T}.AddLast(T)"/>.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenAddLastCalledDuringIteration_ShouldThrowOnMoveNext()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);

        var enumerator = deque.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        deque.AddLast(3);

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that the enumerator throws when the deque is mutated during enumeration via <see cref="ArrayDeque{T}.AddFirst(T)"/>.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenAddFirstCalledDuringIteration_ShouldThrowOnMoveNext()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);

        var enumerator = deque.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        deque.AddFirst(0);

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.RemoveFirst"/> during iteration invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenRemoveFirstCalledDuringIteration_ShouldThrowOnMoveNext()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);

        var enumerator = deque.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = deque.RemoveFirst();

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.RemoveLast"/> during iteration invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenRemoveLastCalledDuringIteration_ShouldThrowOnMoveNext()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        deque.AddLast(3);

        var enumerator = deque.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = deque.RemoveLast();

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayDeque{T}.Clear"/> during iteration invalidates the enumerator.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenClearCalledDuringIteration_ShouldThrowOnMoveNext()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);

        var enumerator = deque.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        deque.Clear();

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that the enumerator throws on <c>Reset</c> after mutation.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenMutatedThenReset_ShouldThrowOnReset()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        var enumerator = deque.GetEnumerator();
        deque.AddLast(2);

        Assert.ThrowsExactly<InvalidOperationException>(() => enumerator.Reset());
    }

    /// <summary>
    /// Verifies that <c>Current</c> throws before <c>MoveNext</c> is called.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCurrentAccessedBeforeMoveNext_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(1);
        var enumerator = deque.GetEnumerator();
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = enumerator.Current; });
    }

    /// <summary>
    /// Verifies that <c>Current</c> throws after iteration is exhausted.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCurrentAccessedAfterExhaustion_ShouldThrowExactly()
    {
        var deque = new ArrayDeque<int>(2);
        deque.AddLast(1);

        var enumerator = deque.GetEnumerator();
        while (enumerator.MoveNext()) { }

        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = enumerator.Current; });
    }

    /// <summary>
    /// Verifies that <c>Reset</c> on an unmodified enumerator allows iteration to restart from the beginning.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetCalledWithoutModification_ShouldRestartIteration()
    {
        var deque = new ArrayDeque<int>(3);
        deque.AddLast(10);
        deque.AddLast(20);
        deque.AddLast(30);

        var enumerator = deque.GetEnumerator();
        var firstPass = new List<int>();
        while (enumerator.MoveNext())
            firstPass.Add(enumerator.Current);

        enumerator.Reset();

        var secondPass = new List<int>();
        while (enumerator.MoveNext())
            secondPass.Add(enumerator.Current);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, firstPass);
        CollectionAssert.AreEqual(firstPass, secondPass);
    }
}
