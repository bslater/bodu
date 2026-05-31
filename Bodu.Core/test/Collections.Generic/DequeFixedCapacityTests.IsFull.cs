// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeFixedCapacityTests.IsFull.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeFixedCapacityTests
{

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddFirst(T)"/> throws when fixed-capacity and full.
    /// </summary>
    [TestMethod]
    public void AddFirst_WhenFixedAndFull_ShouldThrowExactly()
    {
        var deque = new Deque<int>(2, allowGrow: false);
        deque.AddFirst(1);
        deque.AddFirst(2);

        Assert.ThrowsExactly<InvalidOperationException>(() => deque.AddFirst(3));
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddLast(T)"/> throws when fixed-capacity and full.
    /// </summary>
    [TestMethod]
    public void AddLast_WhenFixedAndFull_ShouldThrowExactly()
    {
        var deque = new Deque<int>(2, allowGrow: false);
        deque.AddLast(1);
        deque.AddLast(2);

        Assert.ThrowsExactly<InvalidOperationException>(() => deque.AddLast(3));
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.IsFull"/> reverts to <see langword="false"/> after a removal.
    /// </summary>
    [TestMethod]
    public void IsFull_AfterRemoval_ShouldBeFalse()
    {
        var deque = new Deque<int>(2, allowGrow: false);
        deque.AddLast(1);
        deque.AddLast(2);
        Assert.IsTrue(deque.IsFull);

        _ = deque.RemoveFirst();
        Assert.IsFalse(deque.IsFull);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.IsFull"/> becomes <see langword="true"/> when <see cref="RingBackedCollection{T}.Count"/>
    /// reaches <see cref="RingBackedCollection{T}.Capacity"/>.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenAtCapacity_ShouldBecomeTrue()
    {
        var deque = new Deque<int>(2, allowGrow: false);
        deque.AddLast(1);
        Assert.IsFalse(deque.IsFull);

        deque.AddLast(2);
        Assert.IsTrue(deque.IsFull);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.IsFull"/> reflects fullness reached via head-side adds.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenFilledViaAddFirst_ShouldBecomeTrue()
    {
        var deque = new Deque<int>(2, allowGrow: false);
        deque.AddFirst(1);
        deque.AddFirst(2);
        Assert.IsTrue(deque.IsFull);
    }
    /// <summary>
    /// Verifies that <see cref="Deque{T}.IsFull"/> is <see langword="false"/> for a freshly constructed deque.
    /// </summary>
    [TestMethod]
    public void IsFull_WhenNewlyConstructed_ShouldBeFalse()
    {
        var deque = new Deque<int>(3, allowGrow: false);
        Assert.IsFalse(deque.IsFull);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryAddFirst(T)"/> returns <see langword="false"/> without modifying state when fixed-capacity and full.
    /// </summary>
    [TestMethod]
    public void TryAddFirst_WhenFixedAndFull_ShouldReturnFalseAndPreserveState()
    {
        var deque = new Deque<int>(2, allowGrow: false);
        deque.AddFirst(1);
        deque.AddFirst(2);

        Assert.IsFalse(deque.TryAddFirst(3));
        Assert.AreEqual(2, deque.Count);
        CollectionAssert.AreEqual(new[] { 2, 1 }, deque.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryAddLast(T)"/> returns <see langword="false"/> without modifying state when fixed-capacity and full.
    /// </summary>
    [TestMethod]
    public void TryAddLast_WhenFixedAndFull_ShouldReturnFalseAndPreserveState()
    {
        var deque = new Deque<int>(2, allowGrow: false);
        deque.AddLast(1);
        deque.AddLast(2);

        Assert.IsFalse(deque.TryAddLast(3));
        Assert.AreEqual(2, deque.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, deque.ToArray());
    }

}
