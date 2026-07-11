// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that a new dictionary reports a count of zero and is empty.
    /// </summary>
    [TestMethod]
    public void Count_WhenEmpty_ShouldBeZero()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        Assert.AreEqual(0, dictionary.Count);
        Assert.IsTrue(dictionary.IsEmpty);
    }

    /// <summary>
    /// Verifies that the count tracks additions and removals across segments.
    /// </summary>
    [TestMethod]
    public void Count_WhenMutated_ShouldTrackAddsAndRemoves()
    {
        var dictionary = new ConcurrentEvictingDictionary<int, int>(capacity: 64);

        for (int i = 0; i < 20; i++)
            dictionary.Add(i, i);

        Assert.AreEqual(20, dictionary.Count);
        Assert.IsFalse(dictionary.IsEmpty);

        for (int i = 0; i < 10; i++)
            Assert.IsTrue(dictionary.TryRemove(i, out _));

        Assert.AreEqual(10, dictionary.Count);
    }

    /// <summary>
    /// Verifies that the count never exceeds the capacity, even when many more distinct keys are added.
    /// </summary>
    [TestMethod]
    public void Count_WhenManyKeysAdded_ShouldNeverExceedCapacity()
    {
        var dictionary = new ConcurrentEvictingDictionary<int, int>(capacity: 10);

        for (int i = 0; i < 100; i++)
        {
            dictionary.Add(i, i);
            Assert.IsTrue(dictionary.Count <= 10, $"Count exceeded capacity after adding key {i}.");
        }

        Assert.AreEqual(10, dictionary.Count);
        Assert.AreEqual(90, dictionary.EvictionCount);
    }
}
