// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieTests.KeysWithPrefix.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieTests
{
    /// <summary>
    /// Verifies that a prefix ending partway along a compressed edge still returns every key below the edge.
    /// </summary>
    [TestMethod]
    public void KeysWithPrefix_WhenPrefixEndsInsideEdge_ShouldReturnSubtreeKeys()
    {
        var sut = new RadixTrie(["teapot", "team", "tea"]);

        Assert.IsTrue(sut.KeysWithPrefix("te").ToHashSet().SetEquals(new[] { "tea", "team", "teapot" }));
        Assert.IsTrue(sut.KeysWithPrefix("teap").ToHashSet().SetEquals(new[] { "teapot" }));
    }

    /// <summary>
    /// Verifies that a prefix crossing a split boundary — matching an intermediate node exactly — returns only the
    /// keys below that node.
    /// </summary>
    [TestMethod]
    public void KeysWithPrefix_WhenPrefixCrossesSplitBoundary_ShouldReturnMatchingKeys()
    {
        var sut = new RadixTrie(["romane", "romanus", "romulus"]);

        Assert.IsTrue(sut.KeysWithPrefix("roman").ToHashSet().SetEquals(new[] { "romane", "romanus" }));
        Assert.IsTrue(sut.KeysWithPrefix("romu").ToHashSet().SetEquals(new[] { "romulus" }));
    }

    /// <summary>
    /// Verifies that a prefix diverging inside an edge returns an empty sequence.
    /// </summary>
    [TestMethod]
    public void KeysWithPrefix_WhenPrefixDivergesInsideEdge_ShouldReturnEmpty()
    {
        var sut = new RadixTrie(["team"]);

        Assert.AreEqual(0, sut.KeysWithPrefix("ten").Count());
        Assert.AreEqual(0, sut.KeysWithPrefix("teamx").Count());
    }

    /// <summary>
    /// Verifies that the empty prefix returns every stored key, including the empty-string key.
    /// </summary>
    [TestMethod]
    public void KeysWithPrefix_WhenPrefixIsEmpty_ShouldReturnAllKeys()
    {
        var sut = new RadixTrie(["", "a", "ab"]);

        Assert.IsTrue(sut.KeysWithPrefix(string.Empty).ToHashSet().SetEquals(new[] { "", "a", "ab" }));
    }
}
