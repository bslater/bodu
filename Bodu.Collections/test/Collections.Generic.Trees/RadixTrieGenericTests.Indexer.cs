// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieGenericTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieGenericTests
{
    /// <summary>
    /// Verifies that the indexer reads and writes values, upserting on assignment.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAssigned_ShouldUpsertValue()
    {
        var sut = new RadixTrie<string>();
        sut["k"] = "first";
        sut["k"] = "second";

        Assert.AreEqual("second", sut["k"]);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that reading a missing key through the indexer throws <see cref="KeyNotFoundException" />, including
    /// for a key ending inside a compressed edge.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyMissing_ShouldThrowKeyNotFoundException()
    {
        var sut = new RadixTrie<int> { ["team"] = 1 };

        Assert.ThrowsExactly<KeyNotFoundException>(() => _ = sut["missing"]);
        Assert.ThrowsExactly<KeyNotFoundException>(() => _ = sut["tea"]);
    }
}
