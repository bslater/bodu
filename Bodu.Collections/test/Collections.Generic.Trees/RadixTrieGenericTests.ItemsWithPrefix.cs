// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieGenericTests.ItemsWithPrefix.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieGenericTests
{
    /// <summary>
    /// Verifies that prefix queries return the matching key/value pairs.
    /// </summary>
    [TestMethod]
    public void ItemsWithPrefix_WhenQueried_ShouldReturnMatchingPairs()
    {
        var sut = new RadixTrie<int>
        {
            ["car"] = 1,
            ["card"] = 2,
            ["dog"] = 3,
        };

        var matches = sut.ItemsWithPrefix("car").ToDictionary(p => p.Key, p => p.Value);

        Assert.HasCount(2, matches);
        Assert.AreEqual(1, matches["car"]);
        Assert.AreEqual(2, matches["card"]);
    }

    /// <summary>
    /// Verifies that a prefix ending partway along a compressed edge returns the subtree's pairs, and a diverging
    /// prefix returns nothing.
    /// </summary>
    [TestMethod]
    public void ItemsWithPrefix_WhenPrefixEndsInsideEdge_ShouldReturnSubtreePairs()
    {
        var sut = new RadixTrie<int>
        {
            ["romane"] = 1,
            ["romanus"] = 2,
            ["romulus"] = 3,
        };

        var matches = sut.ItemsWithPrefix("roman").ToDictionary(p => p.Key, p => p.Value);

        Assert.HasCount(2, matches);
        Assert.AreEqual(1, matches["romane"]);
        Assert.AreEqual(2, matches["romanus"]);
        Assert.AreEqual(0, sut.ItemsWithPrefix("romb").Count());
    }

    /// <summary>
    /// Verifies that <see cref="RadixTrie{TValue}.KeysWithPrefix(string)" /> projects the same matches onto keys.
    /// </summary>
    [TestMethod]
    public void KeysWithPrefix_WhenQueried_ShouldReturnMatchingKeys()
    {
        var sut = new RadixTrie<int>
        {
            ["tea"] = 1,
            ["team"] = 2,
            ["ten"] = 3,
        };

        Assert.IsTrue(sut.KeysWithPrefix("tea").ToHashSet().SetEquals(new[] { "tea", "team" }));
        Assert.IsTrue(sut.KeysWithPrefix("te").ToHashSet().SetEquals(new[] { "tea", "team", "ten" }));
    }

    /// <summary>
    /// Verifies that mutating the trie while a <see cref="RadixTrie{TValue}.KeysWithPrefix(string)" /> sequence is
    /// being enumerated causes the next iteration step to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void KeysWithPrefix_WhenMutatedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var sut = new RadixTrie<int>
        {
            ["tea"] = 1,
            ["team"] = 2,
            ["ten"] = 3,
        };

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (string key in sut.KeysWithPrefix("te"))
                sut.Remove("ten");
        });
    }

    /// <summary>
    /// Verifies that mutating the trie while a <see cref="RadixTrie{TValue}.ItemsWithPrefix(string)" /> sequence is
    /// being enumerated causes the next iteration step to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ItemsWithPrefix_WhenMutatedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var sut = new RadixTrie<int>
        {
            ["tea"] = 1,
            ["team"] = 2,
            ["ten"] = 3,
        };

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> item in sut.ItemsWithPrefix("te"))
                sut["teal"] = 4;
        });
    }
}
