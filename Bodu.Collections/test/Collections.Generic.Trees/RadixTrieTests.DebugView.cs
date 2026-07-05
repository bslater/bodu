// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieTests
{
    /// <summary>
    /// Verifies that <see cref="RadixTrieDebugView.Items" /> exposes every stored key.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldExposeEveryKey()
    {
        var sut = new RadixTrie(["tea", "team", "dog"]);

        var view = new RadixTrieDebugView(sut);

        CollectionAssert.AreEquivalent(new[] { "tea", "team", "dog" }, view.Items);
    }

    /// <summary>
    /// Verifies that <see cref="RadixTrieDebugView.Items" /> is empty for an empty trie.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_WhenTrieIsEmpty_ShouldBeEmpty()
    {
        var view = new RadixTrieDebugView(new RadixTrie());

        Assert.HasCount(0, view.Items);
    }

    /// <summary>
    /// Verifies that the debug view constructor throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> trie.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenTrieIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new RadixTrieDebugView(null!);
        });

        Assert.AreEqual("trie", ex.ParamName);
    }
}
