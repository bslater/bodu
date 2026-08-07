// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TrieGenericTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class TrieGenericTests
{
    /// <summary>
    /// Verifies that <see cref="TrieDebugView{TValue}.Items" /> exposes every stored key paired with its value.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldExposeEveryKeyValuePair()
    {
        var sut = new Trie<int>();
        sut.Add("one", 1);
        sut.Add("two", 2);

        var view = new TrieDebugView<int>(sut);

        Assert.AreEqual(2, view.Items.Length);
        CollectionAssert.AreEquivalent(
            new[] { new KeyValuePair<string, int>("one", 1), new KeyValuePair<string, int>("two", 2) },
            view.Items);
    }

    /// <summary>
    /// Verifies that <see cref="TrieDebugView{TValue}.Items" /> is empty for an empty trie.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_WhenTrieIsEmpty_ShouldBeEmpty()
    {
        var view = new TrieDebugView<int>(new Trie<int>());

        Assert.AreEqual(0, view.Items.Length);
    }

    /// <summary>
    /// Verifies that constructing <see cref="TrieDebugView{TValue}" /> with a <see langword="null" /> trie
    /// throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenTrieIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TrieDebugView<int>(null!);
        });

        Assert.AreEqual("trie", ex.ParamName);
    }
}
