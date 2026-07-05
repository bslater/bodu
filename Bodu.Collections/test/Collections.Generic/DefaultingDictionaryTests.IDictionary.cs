// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionaryTests.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DefaultingDictionaryTests
{
    /// <summary>
    /// Verifies that <c>Add</c> delegates to the inner dictionary and throws on a duplicate key.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsDuplicate_ShouldThrowArgumentException()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => 0);
        dictionary.Add("a", 1);

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add("a", 2);
        });
    }

    /// <summary>
    /// Verifies that <c>Clear</c> removes every stored entry.
    /// </summary>
    [TestMethod]
    public void Clear_WhenEntriesStored_ShouldRemoveAllEntries()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => 0);
        _ = dictionary["a"];
        dictionary.Add("b", 2);

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that the key and value collections expose only actually-stored entries.
    /// </summary>
    [TestMethod]
    public void KeysAndValues_WhenEntriesStored_ShouldExposeStoredEntriesOnly()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => 0);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        _ = dictionary.TryGetValue("c", out _);

        CollectionAssert.AreEquivalent(new[] { "a", "b" }, dictionary.Keys.ToList());
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, dictionary.Values.ToList());
    }

    /// <summary>
    /// Verifies that pair containment delegates to the inner dictionary and matches exact stored pairs only.
    /// </summary>
    [TestMethod]
    public void Contains_WhenPairSupplied_ShouldMatchStoredPairsOnly()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => 0);
        dictionary.Add("a", 1);

        Assert.IsTrue(dictionary.Contains(new KeyValuePair<string, int>("a", 1)));
        Assert.IsFalse(dictionary.Contains(new KeyValuePair<string, int>("a", 2)));
        Assert.IsFalse(dictionary.Contains(new KeyValuePair<string, int>("b", 0)));
    }

    /// <summary>
    /// Verifies that <c>CopyTo</c> copies the stored entries at the requested offset.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArraySufficient_ShouldCopyStoredEntries()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => 0);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        var array = new KeyValuePair<string, int>[3];

        dictionary.CopyTo(array, 1);

        Assert.AreEqual(default, array[0]);
        CollectionAssert.AreEquivalent(
            new[]
            {
                new KeyValuePair<string, int>("a", 1),
                new KeyValuePair<string, int>("b", 2),
            },
            array.Skip(1).ToList());
    }

    /// <summary>
    /// Verifies that the dictionary reports itself as writable.
    /// </summary>
    [TestMethod]
    public void IsReadOnly_WhenQueried_ShouldReturnFalse()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => 0);

        Assert.IsFalse(dictionary.IsReadOnly);
    }

    /// <summary>
    /// Verifies that keys are resolved through the supplied comparer, mirroring the inner dictionary's behaviour.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenComparerIsCaseInsensitive_ShouldResolveEitherCasing()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => -1, StringComparer.OrdinalIgnoreCase);
        dictionary.Add("Key", 1);

        Assert.AreEqual(1, dictionary["key"]);
        Assert.AreEqual(1, dictionary.Count);
    }
}
