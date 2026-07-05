// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieTests
{
    /// <summary>
    /// Verifies that adding a duplicate key returns <see langword="false" /> and leaves the count unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldReturnFalse()
    {
        var sut = new RadixTrie();
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
        var sut = new RadixTrie();

        Assert.IsTrue(sut.Add(string.Empty));
        Assert.IsTrue(sut.Contains(string.Empty));
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that adding a strict prefix of an existing key forces an edge split — <c>"team"</c> then
    /// <c>"tea"</c> — while both keys stay individually resolvable.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsPrefixOfExistingKey_ShouldSplitEdgeAndKeepBoth()
    {
        var sut = new RadixTrie();
        sut.Add("team");

        Assert.IsTrue(sut.Add("tea"));

        Assert.AreEqual(2, sut.Count);
        Assert.IsTrue(sut.Contains("tea"));
        Assert.IsTrue(sut.Contains("team"));
        Assert.IsFalse(sut.Contains("te"));
        Assert.IsFalse(sut.Contains("teams"));
        Assert.IsTrue(sut.KeysWithPrefix("te").ToHashSet().SetEquals(new[] { "tea", "team" }));
    }

    /// <summary>
    /// Verifies that adding an extension of an existing key — <c>"tea"</c> then <c>"team"</c> — grows the compressed
    /// edge without disturbing the shorter key.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyExtendsExistingKey_ShouldKeepBoth()
    {
        var sut = new RadixTrie();
        sut.Add("tea");

        Assert.IsTrue(sut.Add("team"));

        Assert.AreEqual(2, sut.Count);
        Assert.IsTrue(sut.Contains("tea"));
        Assert.IsTrue(sut.Contains("team"));
    }

    /// <summary>
    /// Verifies the classic radix-tree example — <c>romane / romanus / romulus</c> — whose insertion order forces
    /// splits at two different depths, leaving every key and prefix answerable.
    /// </summary>
    [TestMethod]
    public void Add_WhenClassicRadixKeySet_ShouldSplitAtEveryDivergence()
    {
        var sut = new RadixTrie();

        Assert.IsTrue(sut.Add("romane"));
        Assert.IsTrue(sut.Add("romanus"));
        Assert.IsTrue(sut.Add("romulus"));

        Assert.AreEqual(3, sut.Count);
        Assert.IsTrue(sut.Contains("romane"));
        Assert.IsTrue(sut.Contains("romanus"));
        Assert.IsTrue(sut.Contains("romulus"));
        Assert.IsFalse(sut.Contains("rom"));
        Assert.IsFalse(sut.Contains("roman"));
        Assert.IsTrue(sut.StartsWith("rom"));
        Assert.IsTrue(sut.StartsWith("roman"));
        Assert.IsTrue(sut.StartsWith("romu"));
        Assert.IsFalse(sut.StartsWith("romb"));
    }

    /// <summary>
    /// Verifies that a key terminating exactly at an intermediate node created by an earlier split is storable.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyEndsAtSplitPoint_ShouldMarkIntermediateNodeTerminal()
    {
        var sut = new RadixTrie(["romane", "romanus", "romulus"]);

        Assert.IsTrue(sut.Add("roman"));

        Assert.AreEqual(4, sut.Count);
        Assert.IsTrue(sut.Contains("roman"));
        Assert.IsTrue(sut.KeysWithPrefix("roman").ToHashSet().SetEquals(new[] { "roman", "romane", "romanus" }));
    }
}
