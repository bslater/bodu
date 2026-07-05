// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public partial class BiDictionaryTests
{
    /// <summary>
    /// Creates a populated dictionary with the keys <c>"a"</c>, <c>"b"</c>, <c>"c"</c> mapped to <c>1</c>, <c>2</c>,
    /// <c>3</c>, using the default <see cref="BiDictionaryDuplicateValuePolicy.Throw" /> policy.
    /// </summary>
    /// <returns>A populated <see cref="BiDictionary{TKey, TValue}" />.</returns>
    private static BiDictionary<string, int> CreatePopulated()
    {
        var dictionary = new BiDictionary<string, int>();
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);
        return dictionary;
    }

    /// <summary>
    /// Verifies that added pairs can be looked up in both directions — value by key and key by value.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void BiDictionary_WhenEntriesAdded_ShouldLookUpBothDirections()
    {
        var dictionary = new BiDictionary<string, int>();
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        Assert.AreEqual(2, dictionary.Count);
        Assert.AreEqual(1, dictionary["a"]);
        Assert.IsTrue(dictionary.TryGetKey(2, out string? key));
        Assert.AreEqual("b", key);
    }
}
