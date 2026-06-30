// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Growth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that adding many distinct elements grows the underlying table while preserving every element, and that
    /// the reported count matches the number of distinct elements added.
    /// </summary>
    [TestMethod]
    public void Add_WhenManyElementsExceedBudget_ShouldGrowTableAndRetainAllElements()
    {
        var set = new ConcurrentHashSet<int>(capacity: 4);

        const int count = 2000;
        for (int i = 0; i < count; i++)
            Assert.IsTrue(set.Add(i));

        Assert.AreEqual(count, set.Count);
        for (int i = 0; i < count; i++)
            Assert.IsTrue(set.Contains(i), $"Element {i} must survive table growth.");
    }
}
