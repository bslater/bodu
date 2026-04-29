// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.MultipleInstances.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that multiple <see cref="Deque{T}"/> instances maintain independent state and contents.
    /// </summary>
    [TestMethod]
    public void MultipleInstances_ShouldMaintainIndependentState()
    {
        var deque1 = new Deque<string>(3);
        var deque2 = new Deque<string>(3);

        deque1.AddLast("A");
        deque1.AddLast("B");

        deque2.AddFirst("X");
        deque2.AddFirst("Y");

        CollectionAssert.AreEqual(new[] { "A", "B" }, deque1.ToArray());
        CollectionAssert.AreEqual(new[] { "Y", "X" }, deque2.ToArray());
    }

    /// <summary>
    /// Verifies that growing one deque does not invalidate enumerators of another.
    /// </summary>
    [TestMethod]
    public void MultipleInstances_ShouldHaveIsolatedVersionTokens()
    {
        var deque1 = new Deque<int>(2);
        var deque2 = new Deque<int>(2);

        deque1.AddLast(1);
        deque2.AddLast(10);

        var enumerator2 = deque2.GetEnumerator();

        // Trigger an auto-grow on the other deque.
        for (int i = 0; i < 50; i++)
            deque1.AddLast(i);

        Assert.IsTrue(enumerator2.MoveNext());
        Assert.AreEqual(10, enumerator2.Current);
        Assert.IsFalse(enumerator2.MoveNext());
    }
}
