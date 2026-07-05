// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that removing an existing key removes the pair from both directions.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyExists_ShouldRemoveBothDirections()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        bool removed = dictionary.Remove("b");

        Assert.IsTrue(removed);
        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("b"));
        Assert.IsFalse(dictionary.ContainsValue(2));
    }

    /// <summary>
    /// Verifies that removing a missing key returns <see langword="false" /> without modifying state.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyMissing_ShouldReturnFalse()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        bool removed = dictionary.Remove("missing");

        Assert.IsFalse(removed);
        Assert.AreEqual(3, dictionary.Count);
    }

    /// <summary>
    /// Verifies that a value freed by <see cref="BiDictionary{TKey, TValue}.Remove(TKey)" /> can be re-bound to a new
    /// key without a conflict.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueFreed_ShouldAllowRebinding()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        dictionary.Remove("a");
        dictionary.Add("d", 1);

        Assert.AreEqual(1, dictionary["d"]);
        Assert.IsTrue(dictionary.TryGetKey(1, out string? key));
        Assert.AreEqual("d", key);
    }

    /// <summary>
    /// Verifies that the key/value pair overload removes a matching pair and rejects a pair whose value differs.
    /// </summary>
    [TestMethod]
    public void Remove_WhenPairOverloadUsed_ShouldRemoveOnlyMatchingPair()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        bool mismatch = dictionary.Remove(new KeyValuePair<string, int>("a", 99));
        bool match = dictionary.Remove(new KeyValuePair<string, int>("a", 1));

        Assert.IsFalse(mismatch);
        Assert.IsTrue(match);
        Assert.IsFalse(dictionary.ContainsKey("a"));
    }
}
