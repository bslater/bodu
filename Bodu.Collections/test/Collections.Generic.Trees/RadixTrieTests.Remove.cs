// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieTests
{
    /// <summary>
    /// Verifies that removing a key prunes it while preserving keys that share its prefix.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeySharesPrefix_ShouldRemoveOnlyThatKey()
    {
        var sut = new RadixTrie(["car", "card"]);

        Assert.IsTrue(sut.Remove("car"));
        Assert.IsFalse(sut.Contains("car"));
        Assert.IsTrue(sut.Contains("card"));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that removing an absent key — including one ending inside a compressed edge — returns
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyAbsent_ShouldReturnFalse()
    {
        var sut = new RadixTrie(["card"]);

        Assert.IsFalse(sut.Remove("dog"));
        Assert.IsFalse(sut.Remove("ca"));
        Assert.IsFalse(sut.Remove("cards"));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that removing one branch of a two-way split merges the surviving branch back into its parent edge,
    /// leaving every remaining key and prefix answer intact.
    /// </summary>
    [TestMethod]
    public void Remove_WhenBranchDeleted_ShouldMergePassThroughAndPreserveAnswers()
    {
        var sut = new RadixTrie(["romane", "romanus", "romulus"]);

        Assert.IsTrue(sut.Remove("romulus"));

        Assert.AreEqual(2, sut.Count);
        Assert.IsTrue(sut.Contains("romane"));
        Assert.IsTrue(sut.Contains("romanus"));
        Assert.IsFalse(sut.Contains("romulus"));
        Assert.IsTrue(sut.StartsWith("roman"));
        Assert.IsFalse(sut.StartsWith("romu"));
        Assert.IsTrue(sut.KeysWithPrefix("rom").ToHashSet().SetEquals(new[] { "romane", "romanus" }));
    }

    /// <summary>
    /// Verifies that clearing an intermediate terminal — the shorter of two nested keys — re-fuses the pass-through
    /// node with its child while the longer key survives.
    /// </summary>
    [TestMethod]
    public void Remove_WhenIntermediateTerminalCleared_ShouldMergeWithChild()
    {
        var sut = new RadixTrie(["tea", "team"]);

        Assert.IsTrue(sut.Remove("tea"));

        Assert.AreEqual(1, sut.Count);
        Assert.IsFalse(sut.Contains("tea"));
        Assert.IsTrue(sut.Contains("team"));
        Assert.IsTrue(sut.StartsWith("tea"));
        Assert.IsTrue(sut.KeysWithPrefix("te").ToHashSet().SetEquals(new[] { "team" }));
    }

    /// <summary>
    /// Verifies that a remove-then-re-add cycle across a merged edge restores the original answers, pinning the
    /// split → merge → split round trip.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyReAddedAfterMerge_ShouldRestoreOriginalAnswers()
    {
        var sut = new RadixTrie(["tea", "team"]);
        sut.Remove("tea");

        Assert.IsTrue(sut.Add("tea"));

        Assert.AreEqual(2, sut.Count);
        Assert.IsTrue(sut.Contains("tea"));
        Assert.IsTrue(sut.Contains("team"));
    }

    /// <summary>
    /// Verifies that removing every key one by one empties the trie and each removal preserves the remainder.
    /// </summary>
    [TestMethod]
    public void Remove_WhenAllKeysRemoved_ShouldEmptyTheTrie()
    {
        string[] keys = ["romane", "romanus", "romulus", "rubens", "ruber", "rubicon", "rubicundus"];
        var sut = new RadixTrie(keys);

        foreach (string key in keys)
        {
            Assert.IsTrue(sut.Remove(key), $"Failed to remove '{key}'.");
            Assert.IsFalse(sut.Contains(key));
        }

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.StartsWith("r"));
    }

    /// <summary>
    /// Verifies that the empty-string key is removable without disturbing other keys.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyIsEmptyString_ShouldRemoveOnlyThatKey()
    {
        var sut = new RadixTrie(["", "a"]);

        Assert.IsTrue(sut.Remove(string.Empty));
        Assert.IsFalse(sut.Contains(string.Empty));
        Assert.IsTrue(sut.Contains("a"));
        Assert.AreEqual(1, sut.Count);
    }
}
