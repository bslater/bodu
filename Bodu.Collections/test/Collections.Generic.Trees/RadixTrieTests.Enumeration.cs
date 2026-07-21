// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieTests
{
    /// <summary>
    /// Verifies that enumeration yields every stored key verbatim, including keys stored across split edges.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenIterated_ShouldYieldAllKeys()
    {
        var sut = new RadixTrie(["a", "ab", "abc", "", "team", "tea"]);

        var seen = sut.ToHashSet();

        Assert.IsTrue(seen.SetEquals(new[] { "a", "ab", "abc", "", "team", "tea" }));
    }

    /// <summary>
    /// Verifies that enumeration yields the keys in exactly the order produced by the internal snapshot walk,
    /// pinning the enumeration order of <see cref="RadixTrie.GetEnumerator" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenIterated_ShouldMatchSnapshotWalkOrder()
    {
        var sut = new RadixTrie(["", "team", "tea", "teapot", "ten", "a", "ab", "abc"]);

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
        var sut = new RadixTrie(["a", "b"]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (string _ in sut)
                sut.Add("c");
        });
    }

    /// <summary>
    /// Verifies that <see cref="RadixTrie.Enumerator.Reset" /> rewinds the snapshot so it can be replayed.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenReset_ShouldReplaySnapshot()
    {
        var sut = new RadixTrie(["x", "y"]);

        var enumerator = sut.GetEnumerator();
        int first = 0;
        while (enumerator.MoveNext())
            first++;

        enumerator.Reset();
        int second = 0;
        while (enumerator.MoveNext())
            second++;

        Assert.AreEqual(2, first);
        Assert.AreEqual(2, second);
    }
}
