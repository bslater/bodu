// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that a newly constructed dictionary reports a count of zero.
    /// </summary>
    [TestMethod]
    public void Count_WhenEmpty_ShouldBeZero()
    {
        var dictionary = new SequencedDictionary<string, int>();

        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Count" /> increases by one for each distinct key added.
    /// </summary>
    [TestMethod]
    public void Count_WhenKeysAdded_ShouldReflectNumberOfEntries()
    {
        var dictionary = new SequencedDictionary<string, int>();

        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        Assert.AreEqual(2, dictionary.Count);
    }

    /// <summary>
    /// Verifies that updating an existing key through the indexer does not change the count.
    /// </summary>
    [TestMethod]
    public void Count_WhenExistingKeyUpdated_ShouldNotChange()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        dictionary["a"] = 99;

        Assert.AreEqual(3, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Count" /> decreases when an entry is removed.
    /// </summary>
    [TestMethod]
    public void Count_WhenEntryRemoved_ShouldDecrease()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        dictionary.Remove("b");

        Assert.AreEqual(2, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Clear" /> resets the count to zero.
    /// </summary>
    [TestMethod]
    public void Count_WhenCleared_ShouldBeZero()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.Count);
    }
}
