// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TrieSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Contains unit tests for the <see cref="Trie" /> (string set) type.
/// </summary>
[TestClass]
public sealed partial class TrieSetTests
{
    /// <summary>
    /// Provides known-answer prefix-query scenarios.
    /// </summary>
    public static IEnumerable<object[]> PrefixScenarios =>
    [
        [new TriePrefixKat("shared prefixes", ["car", "card", "care", "dog"], "car", ["car", "card", "care"])],
        [new TriePrefixKat("partial prefix", ["car", "card", "care", "dog"], "ca", ["car", "card", "care"])],
        [new TriePrefixKat("single match", ["car", "card", "care", "dog"], "do", ["dog"])],
        [new TriePrefixKat("no match", ["car", "dog"], "x", [])],
        [new TriePrefixKat("prefix is key", ["test", "testing"], "test", ["test", "testing"])],
        [new TriePrefixKat("empty prefix returns all", ["a", "b", ""], "", ["a", "b", ""])],
        [new TriePrefixKat("unicode prefix", ["café", "caffeine", "dog"], "caf", ["café", "caffeine"])],
        [new TriePrefixKat("case-insensitive", ["Apple", "apricot", "Banana"], "ap", ["Apple", "apricot"], OrdinalIgnoreCase: true)],
        [new TriePrefixKat("case-sensitive default", ["Apple", "apricot"], "ap", ["apricot"])],
    ];

    /// <summary>
    /// Verifies that an added key is reported as present and as a known prefix.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Add_WhenKeyIsNew_ShouldBeContainedAndPrefixed()
    {
        var sut = new Trie();

        Assert.IsTrue(sut.Add("hello"));
        Assert.IsTrue(sut.Contains("hello"));
        Assert.IsTrue(sut.StartsWith("hel"));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that prefix queries return exactly the expected keys.
    /// </summary>
    /// <param name="kat">The scenario supplying keys, a prefix, and expected matches.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(PrefixScenarios),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void KeysWithPrefix_WhenQueried_ShouldReturnExpectedKeys(TriePrefixKat kat)
    {
        var sut = new Trie(kat.Keys, kat.OrdinalIgnoreCase ? new CaseInsensitiveCharComparer() : null);

        var matches = sut.KeysWithPrefix(kat.Prefix).ToHashSet();

        Assert.IsTrue(matches.SetEquals(kat.Expected), $"Expected [{string.Join(",", kat.Expected)}] but got [{string.Join(",", matches)}]");
        Assert.AreEqual(kat.Expected.Length > 0, sut.StartsWith(kat.Prefix));
    }

    /// <summary>
    /// Verifies that mutating the trie while a <see cref="Trie.KeysWithPrefix(string)" /> sequence is being
    /// enumerated causes the next iteration step to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void KeysWithPrefix_WhenMutatedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var sut = new Trie(["car", "card", "care", "dog"]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (string key in sut.KeysWithPrefix("car"))
                sut.Add("carrot");
        });
    }

    /// <summary>
    /// Verifies that removing a key while a <see cref="Trie.KeysWithPrefix(string)" /> sequence is being enumerated
    /// causes the next iteration step to throw <see cref="InvalidOperationException" /> rather than silently yielding
    /// stale results.
    /// </summary>
    [TestMethod]
    public void KeysWithPrefix_WhenKeyRemovedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var sut = new Trie(["car", "card", "care", "dog"]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (string key in sut.KeysWithPrefix("car"))
                sut.Remove("dog");
        });
    }

    /// <summary>
    /// Verifies that adding a duplicate key returns <see langword="false" /> and leaves the count unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldReturnFalse()
    {
        var sut = new Trie();
        sut.Add("a");

        Assert.IsFalse(sut.Add("a"));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that the empty string is a valid, storable key.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsEmptyString_ShouldStoreIt()
    {
        var sut = new Trie();

        Assert.IsTrue(sut.Add(string.Empty));
        Assert.IsTrue(sut.Contains(string.Empty));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Trie.StartsWith(string)" /> on an empty trie returns <see langword="false" /> even for the empty prefix.
    /// </summary>
    [TestMethod]
    public void StartsWith_WhenTrieIsEmpty_ShouldReturnFalse()
    {
        var sut = new Trie();

        Assert.IsFalse(sut.StartsWith(string.Empty));
        Assert.IsFalse(sut.StartsWith("a"));
    }

    /// <summary>
    /// Verifies that removing a key prunes it while preserving keys that share its prefix.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeySharesPrefix_ShouldRemoveOnlyThatKey()
    {
        var sut = new Trie(["car", "card"]);

        Assert.IsTrue(sut.Remove("car"));
        Assert.IsFalse(sut.Contains("car"));
        Assert.IsTrue(sut.Contains("card"));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that removing an absent key returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyAbsent_ShouldReturnFalse()
    {
        var sut = new Trie(["car"]);

        Assert.IsFalse(sut.Remove("dog"));
        Assert.IsFalse(sut.Remove("ca"));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that the value-checking members reject a <see langword="null" /> key with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Members_WhenKeyIsNull_ShouldThrowForKey()
    {
        var sut = new Trie();

        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Add(null!)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Contains((string)null!)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Remove(null!)).ParamName);
        Assert.AreEqual("prefix", Assert.ThrowsExactly<ArgumentNullException>(() => sut.StartsWith((string)null!)).ParamName);
        Assert.AreEqual("prefix", Assert.ThrowsExactly<ArgumentNullException>(() => sut.KeysWithPrefix(null!)).ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key sequence throws <see cref="ArgumentNullException" /> naming <c>keys</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeysIsNull_ShouldThrowForKeys()
    {
        Assert.AreEqual(
            "keys",
            Assert.ThrowsExactly<ArgumentNullException>(() => new Trie((IEnumerable<string>)null!)).ParamName);
    }

    /// <summary>
    /// Verifies that a case-insensitive comparer treats keys differing only in case as equal.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCaseInsensitive_ShouldMatchAcrossCase()
    {
        var sut = new Trie(new CaseInsensitiveCharComparer());
        sut.Add("Apple");

        Assert.IsTrue(sut.Contains("apple"));
        Assert.IsFalse(sut.Add("APPLE"));
        Assert.IsTrue(sut.StartsWith("App"));
    }

    /// <summary>
    /// Verifies that enumeration yields every stored key.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenIterated_ShouldYieldAllKeys()
    {
        var sut = new Trie(["a", "ab", "abc", ""]);

        var seen = sut.ToHashSet();

        Assert.IsTrue(seen.SetEquals(new[] { "a", "ab", "abc", "" }));
    }

    /// <summary>
    /// Verifies that enumeration yields the keys in exactly the order produced by the internal snapshot walk,
    /// pinning the enumeration order of <see cref="Trie.GetEnumerator" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenIterated_ShouldMatchSnapshotWalkOrder()
    {
        var sut = new Trie(["", "car", "card", "care", "cat", "do", "dog", "done"]);

        string[] expected = sut.ToArrayInternal();
        var actual = new List<string>();
        foreach (string key in sut)
            actual.Add(key);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that mutating the trie during enumeration throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenModifiedDuringIteration_ShouldThrowInvalidOperationException()
    {
        var sut = new Trie(["a", "b"]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (string _ in sut)
                sut.Add("c");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Trie.Clear" /> removes every key.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPopulated_ShouldEmptyTheTrie()
    {
        var sut = new Trie(["a", "b", "c"]);

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.Contains("a"));
        Assert.IsFalse(sut.StartsWith("a"));
    }

    /// <summary>
    /// Verifies that a large number of inserted keys are all retained and queryable.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Add_WhenManyKeys_ShouldRetainAll()
    {
        var sut = new Trie();
        const int n = 20_000;

        for (int i = 0; i < n; i++)
            Assert.IsTrue(sut.Add($"key-{i:D6}"));

        Assert.AreEqual(n, sut.Count);
        Assert.IsTrue(sut.Contains("key-019999"));
        Assert.IsTrue(sut.StartsWith("key-01"));
    }

    /// <summary>
    /// Verifies that the <see cref="ReadOnlySpan{Char}" /> <see cref="Trie.Add(ReadOnlySpan{char})" /> overload adds a
    /// new key and reports the insertion, mirroring the string overload.
    /// </summary>
    [TestMethod]
    public void Add_WhenSpanKeyIsNew_ShouldAddAndReturnTrue()
    {
        var sut = new Trie();

        bool added = sut.Add("team".AsSpan());

        Assert.IsTrue(added);
        Assert.AreEqual(1, sut.Count);
        Assert.IsTrue(sut.Contains("team"));
    }

    /// <summary>
    /// Verifies that the span <see cref="Trie.Add(ReadOnlySpan{char})" /> overload returns <see langword="false" />
    /// for a duplicate key without changing the count.
    /// </summary>
    [TestMethod]
    public void Add_WhenSpanKeyDuplicate_ShouldReturnFalse()
    {
        var sut = new Trie();
        sut.Add("team");

        bool added = sut.Add("team".AsSpan());

        Assert.IsFalse(added);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that the span <see cref="Trie.Remove(ReadOnlySpan{char})" /> overload removes a present key and
    /// reports success, mirroring the string overload.
    /// </summary>
    [TestMethod]
    public void Remove_WhenSpanKeyPresent_ShouldRemoveAndReturnTrue()
    {
        var sut = new Trie(["tea", "team"]);

        bool removed = sut.Remove("tea".AsSpan());

        Assert.IsTrue(removed);
        Assert.IsFalse(sut.Contains("tea"));
        Assert.IsTrue(sut.Contains("team"));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that the span <see cref="Trie.Remove(ReadOnlySpan{char})" /> overload returns <see langword="false" />
    /// for an absent key.
    /// </summary>
    [TestMethod]
    public void Remove_WhenSpanKeyAbsent_ShouldReturnFalse()
    {
        var sut = new Trie(["tea"]);

        bool removed = sut.Remove("dog".AsSpan());

        Assert.IsFalse(removed);
        Assert.AreEqual(1, sut.Count);
    }
}
