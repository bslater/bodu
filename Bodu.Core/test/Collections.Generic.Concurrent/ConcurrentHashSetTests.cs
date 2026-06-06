// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

[TestClass]
public partial class ConcurrentHashSetTests
{
    public TestContext TestContext { get; set; }

    /// <summary>
    /// Asserts that <paramref name="set" /> contains exactly the elements in <paramref name="expected" />, ignoring
    /// order and duplicates.
    /// </summary>
    private static void AssertContainsExactly<TItem>(ConcurrentHashSet<TItem> set, params TItem[] expected)
        where TItem : notnull
    {
        var expectedSet = new HashSet<TItem>(expected, set.Comparer);
        TItem[] actual = set.ToArray();

        Assert.HasCount(expectedSet.Count, actual, "Unexpected element count.");
        Assert.AreEqual(expectedSet.Count, set.Count, "Count disagreed with the expected element count.");

        foreach (TItem item in actual)
            Assert.Contains(item, expectedSet, $"Set contained an unexpected element: {item}.");

        foreach (TItem item in expectedSet)
            Assert.IsTrue(set.Contains(item), $"Set was missing an expected element: {item}.");
    }
}
