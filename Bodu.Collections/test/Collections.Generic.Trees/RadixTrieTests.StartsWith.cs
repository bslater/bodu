// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieTests.StartsWith.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieTests
{
    /// <summary>
    /// Verifies that <see cref="RadixTrie.StartsWith(string)" /> on an empty trie returns <see langword="false" />
    /// even for the empty prefix.
    /// </summary>
    [TestMethod]
    public void StartsWith_WhenTrieIsEmpty_ShouldReturnFalse()
    {
        var sut = new RadixTrie();

        Assert.IsFalse(sut.StartsWith(string.Empty));
        Assert.IsFalse(sut.StartsWith("a"));
    }

    /// <summary>
    /// Verifies that a prefix ending partway along a compressed edge is still recognized, while a diverging prefix is
    /// not.
    /// </summary>
    [TestMethod]
    public void StartsWith_WhenPrefixEndsInsideEdge_ShouldMatchOnlyAgreeingPrefixes()
    {
        var sut = new RadixTrie(["team"]);

        Assert.IsTrue(sut.StartsWith("t"));
        Assert.IsTrue(sut.StartsWith("te"));
        Assert.IsTrue(sut.StartsWith("tea"));
        Assert.IsTrue(sut.StartsWith("team"));
        Assert.IsFalse(sut.StartsWith("teams"));
        Assert.IsFalse(sut.StartsWith("tex"));
        Assert.IsFalse(sut.StartsWith("x"));
    }

    /// <summary>
    /// Verifies that the span overload agrees with the string overload, including the empty prefix.
    /// </summary>
    [TestMethod]
    public void StartsWith_WhenCalledWithSpan_ShouldAgreeWithStringOverload()
    {
        var sut = new RadixTrie(["car"]);

        Assert.IsTrue(sut.StartsWith("ca".AsSpan()));
        Assert.IsFalse(sut.StartsWith("cx".AsSpan()));
        Assert.IsTrue(sut.StartsWith(ReadOnlySpan<char>.Empty));
    }
}
