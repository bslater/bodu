// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.MultipleInstances.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that multiple <see cref="ArrayDeque{T}"/> instances maintain independent state and contents.
    /// </summary>
    [TestMethod]
    public void MultipleInstances_ShouldMaintainIndependentState()
    {
        var deque1 = new ArrayDeque<string>(3);
        var deque2 = new ArrayDeque<string>(3);

        deque1.AddLast("A");
        deque1.AddLast("B");

        deque2.AddFirst("X");
        deque2.AddFirst("Y");

        CollectionAssert.AreEqual(new[] { "A", "B" }, deque1.ToArray());
        CollectionAssert.AreEqual(new[] { "Y", "X" }, deque2.ToArray());
    }

    /// <summary>
    /// Verifies that mutating one deque does not invalidate enumerators of another.
    /// </summary>
    [TestMethod]
    public void MultipleInstances_ShouldHaveIsolatedVersionTokens()
    {
        var deque1 = new ArrayDeque<int>(3);
        var deque2 = new ArrayDeque<int>(3);

        deque1.AddLast(1);
        deque2.AddLast(10);

        var enumerator2 = deque2.GetEnumerator();
        deque1.AddLast(2); // mutate the other deque

        // enumerator2 should remain valid because deque2 has not been mutated
        Assert.IsTrue(enumerator2.MoveNext());
        Assert.AreEqual(10, enumerator2.Current);
        Assert.IsFalse(enumerator2.MoveNext());
    }
}
