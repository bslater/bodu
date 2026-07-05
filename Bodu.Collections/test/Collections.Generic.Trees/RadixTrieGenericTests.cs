// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieGenericTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Contains unit tests for the <see cref="RadixTrie{TValue}" /> (path-compressed string-keyed map) type.
/// </summary>
[TestClass]
public sealed partial class RadixTrieGenericTests
{
    /// <summary>
    /// Verifies that an added key/value pair can be retrieved.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Add_WhenKeyIsNew_ShouldStoreValue()
    {
        var sut = new RadixTrie<int>();
        sut.Add("one", 1);

        Assert.IsTrue(sut.TryGetValue("one", out int value));
        Assert.AreEqual(1, value);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key sequence throws <see cref="ArgumentNullException" /> naming
    /// <c>items</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsIsNull_ShouldThrowForItems()
    {
        Assert.AreEqual(
            "items",
            Assert.ThrowsExactly<ArgumentNullException>(() => new RadixTrie<int>((IEnumerable<KeyValuePair<string, int>>)null!)).ParamName);
    }

    /// <summary>
    /// Verifies that the value-checking members reject a <see langword="null" /> key with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Members_WhenKeyIsNull_ShouldThrowForKey()
    {
        var sut = new RadixTrie<int>();

        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Add(null!, 1)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.TryAdd(null!, 1)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.ContainsKey((string)null!)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.TryGetValue((string)null!, out _)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Remove(null!)).ParamName);
        Assert.AreEqual("key", Assert.ThrowsExactly<ArgumentNullException>(() => sut.Set(null!, 1)).ParamName);
        Assert.AreEqual("prefix", Assert.ThrowsExactly<ArgumentNullException>(() => sut.KeysWithPrefix(null!)).ParamName);
        Assert.AreEqual("prefix", Assert.ThrowsExactly<ArgumentNullException>(() => sut.ItemsWithPrefix(null!)).ParamName);
        Assert.AreEqual("prefix", Assert.ThrowsExactly<ArgumentNullException>(() => sut.StartsWith((string)null!)).ParamName);
    }

    /// <summary>
    /// Verifies that a case-insensitive comparer governs key identity.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCaseInsensitive_ShouldMatchAcrossCase()
    {
        var sut = new RadixTrie<int>(new CaseInsensitiveCharComparer());
        sut.Add("Key", 1);

        Assert.IsTrue(sut.ContainsKey("key"));
        Assert.AreEqual(1, sut["KEY"]);
    }
}
