// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.TrimExcess.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DequeTests
{

    /// <summary>
    /// Verifies that <see cref="Deque{T}"/> can grow again after <see cref="Deque{T}.TrimExcess"/> has
    /// shrunk it. This is unique to <see cref="Deque{T}"/> because growable collections must remain
    /// operational after a trim.
    /// </summary>
    [TestMethod]
    public void TrimExcess_WhenCalledThenAddLast_ShouldGrowAgain()
    {
        var deque = new Deque<int>(100);
        deque.AddLast(1);
        deque.TrimExcess();
        Assert.AreEqual(1, deque.Capacity);

        deque.AddLast(2);
        Assert.AreEqual(2, deque.Count);
        Assert.IsGreaterThanOrEqualTo(2, deque.Capacity);
    }

}
