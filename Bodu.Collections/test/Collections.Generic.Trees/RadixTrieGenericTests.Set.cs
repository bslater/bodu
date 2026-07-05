// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieGenericTests.Set.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieGenericTests
{
    /// <summary>
    /// Verifies that <see cref="RadixTrie{TValue}.Set(string, TValue)" /> updates an existing key without changing
    /// the count.
    /// </summary>
    [TestMethod]
    public void Set_WhenKeyExists_ShouldOverwriteAndKeepCount()
    {
        var sut = new RadixTrie<int>();
        sut.Add("a", 1);

        sut.Set("a", 9);

        Assert.AreEqual(9, sut["a"]);
        Assert.AreEqual(1, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="RadixTrie{TValue}.Set(string, TValue)" /> inserts a missing key, splitting a
    /// compressed edge when required.
    /// </summary>
    [TestMethod]
    public void Set_WhenKeyMissing_ShouldInsert()
    {
        var sut = new RadixTrie<int> { ["team"] = 1 };

        sut.Set("tea", 2);

        Assert.AreEqual(2, sut.Count);
        Assert.AreEqual(1, sut["team"]);
        Assert.AreEqual(2, sut["tea"]);
    }
}
