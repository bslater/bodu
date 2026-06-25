// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.TryRemoveFirst.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.TryRemoveFirst" /> removes and returns the head entry.
    /// </summary>
    [TestMethod]
    public void TryRemoveFirst_WhenPopulated_ShouldRemoveAndReturnHeadEntry()
    {
        var dictionary = CreatePopulated();

        bool removed = dictionary.TryRemoveFirst(out KeyValuePair<string, int> entry);

        Assert.IsTrue(removed);
        Assert.AreEqual(new KeyValuePair<string, int>("a", 1), entry);
        Assert.IsFalse(dictionary.ContainsKey("a"));
        CollectionAssert.AreEqual(new[] { "b", "c" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.TryRemoveFirst" /> returns <see langword="false" /> when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void TryRemoveFirst_WhenEmpty_ShouldReturnFalse()
    {
        var dictionary = new SequencedDictionary<string, int>();

        bool removed = dictionary.TryRemoveFirst(out KeyValuePair<string, int> entry);

        Assert.IsFalse(removed);
        Assert.AreEqual(default, entry);
    }

    /// <summary>
    /// Verifies that repeatedly removing the first entry drains the dictionary in iteration order.
    /// </summary>
    [TestMethod]
    public void TryRemoveFirst_WhenCalledRepeatedly_ShouldDrainInOrder()
    {
        var dictionary = CreatePopulated();

        var drained = new List<string>();
        while (dictionary.TryRemoveFirst(out KeyValuePair<string, int> entry))
            drained.Add(entry.Key);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, drained);
        Assert.AreEqual(0, dictionary.Count);
    }
}
