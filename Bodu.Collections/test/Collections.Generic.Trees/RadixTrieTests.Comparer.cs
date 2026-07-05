// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieTests.Comparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieTests
{
    /// <summary>
    /// Verifies that a case-insensitive comparer treats keys differing only in case as equal.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCaseInsensitive_ShouldMatchAcrossCase()
    {
        var sut = new RadixTrie(new CaseInsensitiveCharComparer());
        sut.Add("Apple");

        Assert.IsTrue(sut.Contains("apple"));
        Assert.IsFalse(sut.Add("APPLE"));
        Assert.IsTrue(sut.StartsWith("App"));
    }

    /// <summary>
    /// Verifies that a case-insensitive comparer governs edge-label matching across a split — differently cased
    /// inserts split and traverse the same compressed edges.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCaseInsensitiveSplit_ShouldMatchLabelsAcrossCase()
    {
        var sut = new RadixTrie(new CaseInsensitiveCharComparer());
        sut.Add("TEAM");

        Assert.IsTrue(sut.Add("tea"));

        Assert.AreEqual(2, sut.Count);
        Assert.IsTrue(sut.Contains("Team"));
        Assert.IsTrue(sut.Contains("TEA"));
        Assert.IsTrue(sut.StartsWith("tE"));
        Assert.IsTrue(sut.KeysWithPrefix("te").ToHashSet().SetEquals(new[] { "TEAM", "tea" }));
    }

    /// <summary>
    /// Verifies that the default comparer is exposed when none is supplied and a supplied comparer is surfaced
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenConstructed_ShouldExposeEffectiveComparer()
    {
        var custom = new CaseInsensitiveCharComparer();

        Assert.AreEqual(EqualityComparer<char>.Default, new RadixTrie().Comparer);
        Assert.AreSame(custom, new RadixTrie(custom).Comparer);
    }
}
