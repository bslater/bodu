// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.AddKeyValuePair.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Add(KeyValuePair{TKey, TValue})" /> adds the entry to the dictionary.
    /// </summary>
    [TestMethod]
    public void Add_KeyValuePair_ShouldAddEntry()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        var pair = new KeyValuePair<string, int>("alpha", 42);

        dictionary.Add(pair);

        Assert.AreEqual(1, dictionary.Count);
        Assert.AreEqual(42, dictionary["alpha"]);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Add(KeyValuePair{TKey, TValue})" /> participates in eviction when the
    /// dictionary's capacity is exceeded, producing the same result as <see cref="EvictingDictionary{TKey, TValue}.Add(TKey, TValue)" />.
    /// </summary>
    [TestMethod]
    public void Add_KeyValuePair_WhenCapacityExceeded_ShouldEvictAccordingToPolicy()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add(new KeyValuePair<string, int>("A", 1));
        dictionary.Add(new KeyValuePair<string, int>("B", 2));
        dictionary.Add(new KeyValuePair<string, int>("C", 3)); // evicts A

        Assert.IsFalse(dictionary.ContainsKey("A"));
        Assert.IsTrue(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

}
