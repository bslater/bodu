// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.ApproximateCount.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that the approximate count is zero for a new dictionary.
    /// </summary>
    [TestMethod]
    public void ApproximateCount_WhenEmpty_ShouldBeZero()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();

        Assert.AreEqual(0, dictionary.ApproximateCount);
    }

    /// <summary>
    /// Verifies that the approximate count matches the exact count when no concurrent mutation is in flight.
    /// </summary>
    [TestMethod]
    public void ApproximateCount_WhenQuiescent_ShouldMatchExactCount()
    {
        var dictionary = new ConcurrentEvictingDictionary<int, int>(capacity: 64);
        for (int i = 0; i < 40; i++)
            dictionary.Add(i, i);

        Assert.AreEqual(dictionary.Count, dictionary.ApproximateCount);
    }

    /// <summary>
    /// Verifies that the approximate count tracks removals when read after the mutation completes.
    /// </summary>
    [TestMethod]
    public void ApproximateCount_WhenEntriesRemoved_ShouldReflectRemovals()
    {
        var dictionary = new ConcurrentEvictingDictionary<int, int>(capacity: 16);
        for (int i = 0; i < 8; i++)
            dictionary.Add(i, i);

        for (int i = 0; i < 4; i++)
            Assert.IsTrue(dictionary.TryRemove(i, out _));

        Assert.AreEqual(4, dictionary.ApproximateCount);
    }
}
