// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.PeekEvictionCandidate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> returns the correct candidate based on recent access.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_AfterMultipleAccessPatterns_ShouldReturnCorrectCandidate()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("one", 1);
        dictionary.Add("two", 2);
        dictionary.Add("three", 3);
        dictionary.Touch("two");

        // LRU: "one" is least recently used
        Assert.AreEqual("one", dictionary.PeekEvictionCandidate());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> returns null after all items are removed.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenAllItemsRemoved_ShouldReturnNull()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("x", 1);
        dictionary.Remove("x");

        Assert.IsNull(dictionary.PeekEvictionCandidate());
    }
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> returns null when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenDictionaryIsEmpty_ShouldReturnNull()
    {
        var dictionary = new EvictingDictionary<string, int>(3);

        Assert.IsNull(dictionary.PeekEvictionCandidate());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> returns the only item when one has been added.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenItemRecentlyAdded_ShouldReturnIt()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("a", 100);

        Assert.AreEqual("a", dictionary.PeekEvictionCandidate());
    }

}
