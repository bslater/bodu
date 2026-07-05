// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieTests
{
    /// <summary>
    /// Verifies that only stored keys — not prefixes ending inside a compressed edge — are reported as contained.
    /// </summary>
    [TestMethod]
    public void Contains_WhenKeyEndsInsideEdge_ShouldReturnFalse()
    {
        var sut = new RadixTrie(["team"]);

        Assert.IsTrue(sut.Contains("team"));
        Assert.IsFalse(sut.Contains("tea"));
        Assert.IsFalse(sut.Contains("t"));
        Assert.IsFalse(sut.Contains("teams"));
    }

    /// <summary>
    /// Verifies that the span overload agrees with the string overload.
    /// </summary>
    [TestMethod]
    public void Contains_WhenCalledWithSpan_ShouldAgreeWithStringOverload()
    {
        var sut = new RadixTrie(["car", "card"]);

        Assert.IsTrue(sut.Contains("car".AsSpan()));
        Assert.IsTrue(sut.Contains("card".AsSpan()));
        Assert.IsFalse(sut.Contains("ca".AsSpan()));
        Assert.IsFalse(sut.Contains(ReadOnlySpan<char>.Empty));
    }
}
