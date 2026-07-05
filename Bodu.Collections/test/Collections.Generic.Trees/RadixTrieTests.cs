// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Contains unit tests for the <see cref="RadixTrie" /> (path-compressed string set) type.
/// </summary>
[TestClass]
public sealed partial class RadixTrieTests
{
    /// <summary>
    /// Provides known-answer prefix-query scenarios — the same catalogue the uncompressed <see cref="Trie" /> is
    /// pinned by, because the compressed representation must answer identically.
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
        [new TriePrefixKat("prefix ends mid-edge", ["romane", "romanus", "romulus"], "rom", ["romane", "romanus", "romulus"])],
        [new TriePrefixKat("prefix crosses split", ["romane", "romanus", "romulus"], "roman", ["romane", "romanus"])],
    ];

    /// <summary>
    /// Verifies that an added key is reported as present and as a known prefix.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Add_WhenKeyIsNew_ShouldBeContainedAndPrefixed()
    {
        var sut = new RadixTrie();

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
        var sut = new RadixTrie(kat.Keys, kat.OrdinalIgnoreCase ? new CaseInsensitiveCharComparer() : null);

        var matches = sut.KeysWithPrefix(kat.Prefix).ToHashSet();

        Assert.IsTrue(matches.SetEquals(kat.Expected), $"Expected [{string.Join(",", kat.Expected)}] but got [{string.Join(",", matches)}]");
        Assert.AreEqual(kat.Expected.Length > 0, sut.StartsWith(kat.Prefix));
    }

    /// <summary>
    /// Verifies that the value-checking members reject a <see langword="null" /> key with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Members_WhenKeyIsNull_ShouldThrowForKey()
    {
        var sut = new RadixTrie();

        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Add(null!)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Contains((string)null!)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Remove(null!)).ParamName);
        Assert.AreEqual("prefix", Assert.ThrowsExactly<ArgumentNullException>(() => sut.StartsWith((string)null!)).ParamName);
        Assert.AreEqual("prefix", Assert.ThrowsExactly<ArgumentNullException>(() => sut.KeysWithPrefix(null!)).ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key sequence throws <see cref="ArgumentNullException" /> naming
    /// <c>keys</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeysIsNull_ShouldThrowForKeys()
    {
        Assert.AreEqual(
            "keys",
            Assert.ThrowsExactly<ArgumentNullException>(() => new RadixTrie((IEnumerable<string>)null!)).ParamName);
    }
}
