// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.TryRemoveLast.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.TryRemoveLast" /> removes and returns the tail entry.
    /// </summary>
    [TestMethod]
    public void TryRemoveLast_WhenPopulated_ShouldRemoveAndReturnTailEntry()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        bool removed = dictionary.TryRemoveLast(out KeyValuePair<string, int> entry);

        Assert.IsTrue(removed);
        Assert.AreEqual(new KeyValuePair<string, int>("c", 3), entry);
        Assert.IsFalse(dictionary.ContainsKey("c"));
        CollectionAssert.AreEqual(new[] { "a", "b" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.TryRemoveLast" /> returns <see langword="false" /> when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void TryRemoveLast_WhenEmpty_ShouldReturnFalse()
    {
        var dictionary = new SequencedDictionary<string, int>();

        bool removed = dictionary.TryRemoveLast(out KeyValuePair<string, int> entry);

        Assert.IsFalse(removed);
        Assert.AreEqual(default, entry);
    }

    /// <summary>
    /// Verifies that repeatedly removing the last entry drains the dictionary in reverse iteration order.
    /// </summary>
    [TestMethod]
    public void TryRemoveLast_WhenCalledRepeatedly_ShouldDrainInReverseOrder()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        var drained = new List<string>();
        while (dictionary.TryRemoveLast(out KeyValuePair<string, int> entry))
            drained.Add(entry.Key);

        CollectionAssert.AreEqual(new[] { "c", "b", "a" }, drained);
        Assert.AreEqual(0, dictionary.Count);
    }
}
