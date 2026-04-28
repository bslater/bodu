// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.Contains.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{
    /// <summary>
    /// Verifies that <see cref="Deque{T}.Contains(T)"/> returns <see langword="true"/> when an item is present.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsPresent_ShouldReturnTrue()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        deque.AddLast(2);
        Assert.IsTrue(deque.Contains(2));
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.Contains(T)"/> returns <see langword="false"/> when an item is absent.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsAbsent_ShouldReturnFalse()
    {
        var deque = new Deque<int>(3);
        deque.AddLast(1);
        Assert.IsFalse(deque.Contains(99));
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.Contains(T)"/> finds elements in wrapped, post-grow storage.
    /// </summary>
    [TestMethod]
    public void Contains_WhenStorageWrappedAndGrown_ShouldStillFind()
    {
        var deque = new Deque<int>(2);
        for (int i = 0; i < 10; i++)
            deque.AddLast(i);

        Assert.IsTrue(deque.Contains(5));
        Assert.IsTrue(deque.Contains(0));
        Assert.IsTrue(deque.Contains(9));
    }
}
