// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieGenericTests.ContainsKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieGenericTests
{
    /// <summary>
    /// Verifies that only stored keys — not prefixes ending inside a compressed edge — are reported as present.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyEndsInsideEdge_ShouldReturnFalse()
    {
        var sut = new RadixTrie<int> { ["team"] = 1 };

        Assert.IsTrue(sut.ContainsKey("team"));
        Assert.IsFalse(sut.ContainsKey("tea"));
        Assert.IsFalse(sut.ContainsKey("teams"));
    }

    /// <summary>
    /// Verifies that the span overload agrees with the string overload.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenCalledWithSpan_ShouldAgreeWithStringOverload()
    {
        var sut = new RadixTrie<int> { ["car"] = 1 };

        Assert.IsTrue(sut.ContainsKey("car".AsSpan()));
        Assert.IsFalse(sut.ContainsKey("ca".AsSpan()));
    }
}
