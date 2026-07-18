// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieGenericTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieGenericTests
{
    /// <summary>
    /// Verifies that enumeration yields every stored key/value pair.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenIterated_ShouldYieldAllPairs()
    {
        var sut = new RadixTrie<int> { ["a"] = 1, ["ab"] = 2, ["abc"] = 3 };

        var seen = sut.ToDictionary(p => p.Key, p => p.Value);

        CollectionAssert.AreEquivalent(new[] { "a", "ab", "abc" }, seen.Keys);
        Assert.AreEqual(2, seen["ab"]);
    }

    /// <summary>
    /// Verifies that enumeration yields the entries in exactly the order produced by the internal snapshot walk,
    /// pinning the enumeration order of <see cref="RadixTrie{TValue}.GetEnumerator" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenIterated_ShouldMatchSnapshotWalkOrder()
    {
        var sut = new RadixTrie<int> { [""] = 0, ["team"] = 1, ["tea"] = 2, ["teapot"] = 3, ["ten"] = 4, ["a"] = 5, ["ab"] = 6 };

        KeyValuePair<string, int>[] expected = sut.ToArrayInternal();
        var actual = new List<KeyValuePair<string, int>>();
        foreach (KeyValuePair<string, int> item in sut)
            actual.Add(item);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that mutating the trie during enumeration throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenModifiedDuringIteration_ShouldThrowInvalidOperationException()
    {
        var sut = new RadixTrie<int> { ["a"] = 1, ["b"] = 2 };

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> _ in sut)
                sut.Set("c", 3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="RadixTrie{TValue}.Clear" /> removes every entry.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPopulated_ShouldEmptyTheTrie()
    {
        var sut = new RadixTrie<int> { ["a"] = 1, ["ab"] = 2 };

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.ContainsKey("a"));
    }
}
