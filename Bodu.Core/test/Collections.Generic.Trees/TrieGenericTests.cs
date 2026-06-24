// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TrieGenericTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Contains unit tests for the <see cref="Trie{TValue}" /> (string-keyed map) type.
/// </summary>
[TestClass]
public sealed class TrieGenericTests
{
    /// <summary>
    /// Verifies that an added key/value pair can be retrieved.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Add_WhenKeyIsNew_ShouldStoreValue()
    {
        var sut = new Trie<int>();
        sut.Add("one", 1);

        Assert.IsTrue(sut.TryGetValue("one", out var value));
        Assert.AreEqual(1, value);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that adding a duplicate key throws <see cref="ArgumentException" /> naming <c>key</c>.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldThrowForKey()
    {
        var sut = new Trie<int>();
        sut.Add("a", 1);

        Assert.AreEqual(
            "key",
            Assert.ThrowsExactly<ArgumentException>(() => sut.Add("a", 2)).ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Trie{TValue}.TryAdd(string, TValue)" /> adds new keys and rejects duplicates.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenKeyIsNewOrDuplicate_ShouldReportOutcome()
    {
        var sut = new Trie<int>();

        Assert.IsTrue(sut.TryAdd("a", 1));
        Assert.IsFalse(sut.TryAdd("a", 2));
        Assert.AreEqual(1, sut["a"]);
    }

    /// <summary>
    /// Verifies that the indexer reads and writes values, upserting on assignment.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAssigned_ShouldUpsertValue()
    {
        var sut = new Trie<string>();
        sut["k"] = "first";
        sut["k"] = "second";

        Assert.AreEqual("second", sut["k"]);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that reading a missing key through the indexer throws <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyMissing_ShouldThrowKeyNotFoundException()
    {
        var sut = new Trie<int>();

        Assert.ThrowsExactly<KeyNotFoundException>(() => _ = sut["missing"]);
    }

    /// <summary>
    /// Verifies that <see cref="Trie{TValue}.Set(string, TValue)" /> updates an existing key without changing the count.
    /// </summary>
    [TestMethod]
    public void Set_WhenKeyExists_ShouldOverwriteAndKeepCount()
    {
        var sut = new Trie<int>();
        sut.Add("a", 1);

        sut.Set("a", 9);

        Assert.AreEqual(9, sut["a"]);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that prefix queries return the matching key/value pairs.
    /// </summary>
    [TestMethod]
    public void ItemsWithPrefix_WhenQueried_ShouldReturnMatchingPairs()
    {
        var sut = new Trie<int>
        {
            ["car"] = 1,
            ["card"] = 2,
            ["dog"] = 3,
        };

        var matches = sut.ItemsWithPrefix("car").ToDictionary(p => p.Key, p => p.Value);

        Assert.AreEqual(2, matches.Count);
        Assert.AreEqual(1, matches["car"]);
        Assert.AreEqual(2, matches["card"]);
    }

    /// <summary>
    /// Verifies that removing a key deletes its entry.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyExists_ShouldDeleteEntry()
    {
        var sut = new Trie<int>();
        sut.Add("a", 1);

        Assert.IsTrue(sut.Remove("a"));
        Assert.IsFalse(sut.ContainsKey("a"));
        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key sequence throws <see cref="ArgumentNullException" /> naming <c>items</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsIsNull_ShouldThrowForItems()
    {
        Assert.AreEqual(
            "items",
            Assert.ThrowsExactly<ArgumentNullException>(() => new Trie<int>((IEnumerable<KeyValuePair<string, int>>)null!)).ParamName);
    }

    /// <summary>
    /// Verifies that the value-checking members reject a <see langword="null" /> key with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Members_WhenKeyIsNull_ShouldThrowForKey()
    {
        var sut = new Trie<int>();

        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Add(null!, 1)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.TryAdd(null!, 1)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.ContainsKey((string)null!)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.TryGetValue((string)null!, out _)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Remove(null!)).ParamName);
    }

    /// <summary>
    /// Verifies that <see langword="null" /> values are storable and retrievable for reference-typed values.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsNull_ShouldStoreAndRetrieveNull()
    {
        var sut = new Trie<string?>();
        sut.Add("k", null);

        Assert.IsTrue(sut.TryGetValue("k", out var value));
        Assert.IsNull(value);
        Assert.IsTrue(sut.ContainsKey("k"));
    }

    /// <summary>
    /// Verifies that enumeration yields every stored key/value pair.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenIterated_ShouldYieldAllPairs()
    {
        var sut = new Trie<int> { ["a"] = 1, ["ab"] = 2, ["abc"] = 3 };

        var seen = sut.ToDictionary(p => p.Key, p => p.Value);

        CollectionAssert.AreEquivalent(new[] { "a", "ab", "abc" }, seen.Keys);
        Assert.AreEqual(2, seen["ab"]);
    }

    /// <summary>
    /// Verifies that mutating the trie during enumeration throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenModifiedDuringIteration_ShouldThrowInvalidOperationException()
    {
        var sut = new Trie<int> { ["a"] = 1, ["b"] = 2 };

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (var _ in sut)
                sut.Set("c", 3);
        });
    }

    /// <summary>
    /// Verifies that a case-insensitive comparer governs key identity.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCaseInsensitive_ShouldMatchAcrossCase()
    {
        var sut = new Trie<int>(new CaseInsensitiveCharComparer());
        sut.Add("Key", 1);

        Assert.IsTrue(sut.ContainsKey("key"));
        Assert.AreEqual(1, sut["KEY"]);
    }
}
