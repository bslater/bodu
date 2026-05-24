// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.TryAdd.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryAddFirst(T)" /> auto-grows the deque when it is full and <c>AllowGrow</c> is
    /// <see langword="true" />, and reports success.
    /// </summary>
    [TestMethod]
    public void TryAddFirst_WhenFullAndAllowGrow_ShouldGrowAndAddToHead()
    {
        var deque = new Deque<int>(capacity: 2);
        Assert.IsTrue(deque.AllowGrow, "Default Deque should allow growth.");

        deque.AddFirst(1);
        deque.AddFirst(2);

        Assert.IsTrue(deque.TryAddFirst(3), "TryAddFirst should auto-grow and succeed.");
        Assert.AreEqual(3, deque.PeekFirst());
        Assert.AreEqual(3, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryAddFirst(T)" /> adds successfully to a non-full deque without growing.
    /// </summary>
    [TestMethod]
    public void TryAddFirst_WhenNotFull_ShouldAddAndReturnTrue()
    {
        var deque = new Deque<int>(capacity: 4);
        deque.AddFirst(1);

        Assert.IsTrue(deque.TryAddFirst(2));
        Assert.AreEqual(2, deque.PeekFirst());
        Assert.AreEqual(2, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryAddLast(T)" /> auto-grows the deque when it is full and <c>AllowGrow</c> is
    /// <see langword="true" />, and reports success.
    /// </summary>
    [TestMethod]
    public void TryAddLast_WhenFullAndAllowGrow_ShouldGrowAndAddToTail()
    {
        var deque = new Deque<int>(capacity: 2);
        Assert.IsTrue(deque.AllowGrow);

        deque.AddLast(1);
        deque.AddLast(2);

        Assert.IsTrue(deque.TryAddLast(3), "TryAddLast should auto-grow and succeed.");
        Assert.AreEqual(3, deque.PeekLast());
        Assert.AreEqual(3, deque.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.TryAddLast(T)" /> adds successfully to a non-full deque without growing.
    /// </summary>
    [TestMethod]
    public void TryAddLast_WhenNotFull_ShouldAddAndReturnTrue()
    {
        var deque = new Deque<int>(capacity: 4);
        deque.AddLast(1);

        Assert.IsTrue(deque.TryAddLast(2));
        Assert.AreEqual(2, deque.PeekLast());
        Assert.AreEqual(2, deque.Count);
    }

}
