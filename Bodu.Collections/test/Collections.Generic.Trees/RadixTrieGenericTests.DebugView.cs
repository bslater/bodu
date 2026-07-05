// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieGenericTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieGenericTests
{
    /// <summary>
    /// Verifies that <see cref="RadixTrieDebugView{TValue}.Items" /> exposes every stored key/value pair.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldExposeEveryPair()
    {
        var sut = new RadixTrie<int> { ["tea"] = 1, ["team"] = 2 };

        var view = new RadixTrieDebugView<int>(sut);

        CollectionAssert.AreEquivalent(
            new[]
            {
                new KeyValuePair<string, int>("tea", 1),
                new KeyValuePair<string, int>("team", 2),
            },
            view.Items);
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
            _ = new RadixTrieDebugView<int>(null!);
        });

        Assert.AreEqual("trie", ex.ParamName);
    }
}
