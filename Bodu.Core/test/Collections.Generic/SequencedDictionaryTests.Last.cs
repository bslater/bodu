// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Last.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Last" /> returns the tail entry in iteration order.
    /// </summary>
    [TestMethod]
    public void Last_WhenPopulated_ShouldReturnTailEntry()
    {
        var dictionary = CreatePopulated();

        Assert.AreEqual(new KeyValuePair<string, int>("c", 3), dictionary.Last);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Last" /> throws when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void Last_WhenEmpty_ShouldThrowExactly()
    {
        var dictionary = new SequencedDictionary<string, int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = dictionary.Last;
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.TryGetLast" /> reports the tail entry without removing it.
    /// </summary>
    [TestMethod]
    public void TryGetLast_WhenPopulated_ShouldReturnTrueAndTailEntry()
    {
        var dictionary = CreatePopulated();

        bool found = dictionary.TryGetLast(out KeyValuePair<string, int> entry);

        Assert.IsTrue(found);
        Assert.AreEqual(new KeyValuePair<string, int>("c", 3), entry);
        Assert.AreEqual(3, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.TryGetLast" /> returns <see langword="false" /> when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void TryGetLast_WhenEmpty_ShouldReturnFalse()
    {
        var dictionary = new SequencedDictionary<string, int>();

        bool found = dictionary.TryGetLast(out KeyValuePair<string, int> entry);

        Assert.IsFalse(found);
        Assert.AreEqual(default, entry);
    }
}
