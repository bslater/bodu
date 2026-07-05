// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieGenericTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieGenericTests
{
    /// <summary>
    /// Verifies that a stored key yields its value and a missing key yields the default value with a
    /// <see langword="false" /> result.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyPresentOrAbsent_ShouldReportOutcome()
    {
        var sut = new RadixTrie<int> { ["team"] = 42 };

        Assert.IsTrue(sut.TryGetValue("team", out int found));
        Assert.AreEqual(42, found);

        Assert.IsFalse(sut.TryGetValue("tea", out int missing));
        Assert.AreEqual(0, missing);
    }

    /// <summary>
    /// Verifies that the span overload agrees with the string overload.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenCalledWithSpan_ShouldAgreeWithStringOverload()
    {
        var sut = new RadixTrie<int> { ["car"] = 3 };

        Assert.IsTrue(sut.TryGetValue("car".AsSpan(), out int value));
        Assert.AreEqual(3, value);
        Assert.IsFalse(sut.TryGetValue("card".AsSpan(), out _));
    }
}
