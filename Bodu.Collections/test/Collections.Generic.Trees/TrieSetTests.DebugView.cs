// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TrieSetTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class TrieSetTests
{
    /// <summary>
    /// Verifies that <see cref="TrieDebugView.Items" /> exposes every stored key.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldExposeEveryKey()
    {
        var sut = new Trie();
        sut.Add("hello");
        sut.Add("help");
        sut.Add("world");

        var view = new TrieDebugView(sut);

        Assert.AreEqual(3, view.Items.Length);
        CollectionAssert.AreEquivalent(new[] { "hello", "help", "world" }, view.Items);
    }

    /// <summary>
    /// Verifies that <see cref="TrieDebugView.Items" /> is empty for an empty trie.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_WhenTrieIsEmpty_ShouldBeEmpty()
    {
        var view = new TrieDebugView(new Trie());

        Assert.AreEqual(0, view.Items.Length);
    }

    /// <summary>
    /// Verifies that constructing <see cref="TrieDebugView" /> with a <see langword="null" /> trie throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenTrieIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TrieDebugView(null!);
        });

        Assert.AreEqual("trie", ex.ParamName);
    }
}
