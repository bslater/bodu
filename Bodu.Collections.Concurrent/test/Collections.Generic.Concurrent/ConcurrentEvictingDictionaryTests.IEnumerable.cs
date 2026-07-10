// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.IEnumerable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that enumeration yields exactly the stored entries.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenEnumerated_ShouldYieldExactlyStoredEntries()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        var seen = new Dictionary<string, int>();
        foreach (KeyValuePair<string, int> pair in dictionary)
            seen.Add(pair.Key, pair.Value);

        Assert.HasCount(2, seen);
        Assert.AreEqual(1, seen["a"]);
        Assert.AreEqual(2, seen["b"]);
    }

    /// <summary>
    /// Verifies that mutating the dictionary during enumeration does not throw: the enumerator iterates its snapshot.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenMutatedDuringEnumeration_ShouldNotThrow()
    {
        var dictionary = new ConcurrentEvictingDictionary<int, int>(capacity: 32);
        for (int i = 0; i < 10; i++)
            dictionary.Add(i, i);

        int enumerated = 0;
        foreach (KeyValuePair<int, int> pair in dictionary)
        {
            dictionary.Add(100 + pair.Key, pair.Key);
            Assert.IsTrue(dictionary.TryRemove(pair.Key, out _));
            enumerated++;
        }

        Assert.AreEqual(10, enumerated, "Every snapshot entry must be yielded despite the concurrent mutations.");
    }

    /// <summary>
    /// Verifies that the explicit non-generic enumerator yields the same entries as the generic one.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenEnumeratedNonGenerically_ShouldYieldSameEntries()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();
        dictionary.Add("a", 1);

        IEnumerable enumerable = dictionary;
        int count = 0;
        foreach (object? item in enumerable)
        {
            Assert.IsInstanceOfType<KeyValuePair<string, int>>(item);
            count++;
        }

        Assert.AreEqual(1, count);
    }

    /// <summary>
    /// Verifies that enumerating an empty dictionary yields nothing.
    /// </summary>
    [TestMethod]
    public void IEnumerable_WhenEmpty_ShouldYieldNothing()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();

        foreach (KeyValuePair<string, int> pair in dictionary)
            Assert.Fail($"Unexpected entry: {pair.Key}.");
    }
}
