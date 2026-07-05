// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.Clear" /> removes all entries from both directions.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPopulated_ShouldRemoveAllEntriesBothDirections()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("a"));
        Assert.IsFalse(dictionary.ContainsValue(1));
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.Clear" /> also empties the <see cref="BiDictionary{TKey,
    /// TValue}.Inverse" /> view.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPopulated_ShouldEmptyInverseView()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();
        BiDictionary<int, string> inverse = dictionary.Inverse;

        dictionary.Clear();

        Assert.AreEqual(0, inverse.Count);
        Assert.IsFalse(inverse.ContainsKey(1));
    }

    /// <summary>
    /// Verifies that <see cref="BiDictionary{TKey, TValue}.Clear" /> on an empty dictionary is a no-op.
    /// </summary>
    [TestMethod]
    public void Clear_WhenEmpty_ShouldRemainEmpty()
    {
        var dictionary = new BiDictionary<string, int>();

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that entries can be re-added after <see cref="BiDictionary{TKey, TValue}.Clear" /> without conflicts.
    /// </summary>
    [TestMethod]
    public void Clear_WhenEntriesReAdded_ShouldAcceptPreviousBindings()
    {
        BiDictionary<string, int> dictionary = CreatePopulated();

        dictionary.Clear();
        dictionary.Add("a", 1);

        Assert.AreEqual(1, dictionary.Count);
        Assert.AreEqual(1, dictionary["a"]);
    }
}
