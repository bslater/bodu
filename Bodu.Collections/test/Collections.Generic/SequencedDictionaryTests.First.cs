// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.First.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.First" /> returns the head entry in iteration order.
    /// </summary>
    [TestMethod]
    public void First_WhenPopulated_ShouldReturnHeadEntry()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.AreEqual(new KeyValuePair<string, int>("a", 1), dictionary.First);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.First" /> throws when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void First_WhenEmpty_ShouldThrowExactly()
    {
        var dictionary = new SequencedDictionary<string, int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = dictionary.First;
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.First" /> does not change the iteration order in access-order mode.
    /// </summary>
    [TestMethod]
    public void First_WhenAccessOrder_ShouldNotChangeOrder()
    {
        var dictionary = new SequencedDictionary<string, int>(accessOrder: true)
        {
            ["a"] = 1,
            ["b"] = 2,
        };

        _ = dictionary.First;

        CollectionAssert.AreEqual(new[] { "a", "b" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.TryGetFirst" /> reports the head entry without removing it.
    /// </summary>
    [TestMethod]
    public void TryGetFirst_WhenPopulated_ShouldReturnTrueAndHeadEntry()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        bool found = dictionary.TryGetFirst(out KeyValuePair<string, int> entry);

        Assert.IsTrue(found);
        Assert.AreEqual(new KeyValuePair<string, int>("a", 1), entry);
        Assert.AreEqual(3, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.TryGetFirst" /> returns <see langword="false" /> when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void TryGetFirst_WhenEmpty_ShouldReturnFalse()
    {
        var dictionary = new SequencedDictionary<string, int>();

        bool found = dictionary.TryGetFirst(out KeyValuePair<string, int> entry);

        Assert.IsFalse(found);
        Assert.AreEqual(default, entry);
    }
}
