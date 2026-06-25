// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Nulls.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.ContainsKey" /> throws when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenContainsKeyWithNullKey_ShouldThrowExactly()
    {
        var dictionary = new SequencedDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.ContainsKey(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.TryGetValue" /> throws when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenTryGetValueWithNullKey_ShouldThrowExactly()
    {
        var dictionary = new SequencedDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.TryGetValue(null!, out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Remove(TKey)" /> throws when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenRemoveWithNullKey_ShouldThrowExactly()
    {
        var dictionary = new SequencedDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.Remove(null!);
        });
    }

    /// <summary>
    /// Verifies that the indexer getter throws when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenIndexerGetWithNullKey_ShouldThrowExactly()
    {
        var dictionary = new SequencedDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary[null!];
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> value is accepted and preserved for a reference-type value.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenValueIsNull_ShouldStoreAndReturnNull()
    {
        var dictionary = new SequencedDictionary<string, string?>
        {
            ["a"] = null,
        };

        Assert.IsTrue(dictionary.TryGetValue("a", out string? value));
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Contains" /> matches an entry whose value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenContainsPairWithNullValue_ShouldReturnTrue()
    {
        var dictionary = new SequencedDictionary<string, string?>
        {
            ["a"] = null,
        };

        Assert.IsTrue(dictionary.Contains(new KeyValuePair<string, string?>("a", null)));
    }

    /// <summary>
    /// Verifies that an entry with a <see langword="null" /> value participates in iteration order like any other entry.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenValueIsNull_ShouldEnumerateInOrder()
    {
        var dictionary = new SequencedDictionary<string, string?>
        {
            ["a"] = "x",
            ["b"] = null,
            ["c"] = "z",
        };

        CollectionAssert.AreEqual(new[] { "x", null, "z" }, dictionary.Values.ToArray());
    }
}
