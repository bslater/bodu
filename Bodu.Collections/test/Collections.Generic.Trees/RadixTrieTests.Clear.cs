// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieTests
{
    /// <summary>
    /// Verifies that <see cref="RadixTrie.Clear" /> removes every key.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPopulated_ShouldEmptyTheTrie()
    {
        var sut = new RadixTrie(["a", "b", "c"]);

        sut.Clear();

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.Contains("a"));
        Assert.IsFalse(sut.StartsWith("a"));
    }

    /// <summary>
    /// Verifies that the trie remains usable after <see cref="RadixTrie.Clear" />.
    /// </summary>
    [TestMethod]
    public void Clear_WhenFollowedByAdd_ShouldStoreNewKeys()
    {
        var sut = new RadixTrie(["team", "tea"]);
        sut.Clear();

        Assert.IsTrue(sut.Add("fresh"));
        Assert.AreEqual(1, sut.Count);
        Assert.IsTrue(sut.Contains("fresh"));
    }
}
