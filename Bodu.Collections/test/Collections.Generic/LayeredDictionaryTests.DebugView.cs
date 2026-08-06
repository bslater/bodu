// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="LayeredDictionaryDebugView{TKey, TValue}.Items" /> exposes the flattened view
    /// across layers, with the higher-priority layer shadowing a duplicate key rather than reporting it twice.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldExposeFlattenedViewWithShadowingApplied()
    {
        var top = new Dictionary<string, int> { ["a"] = 1 };
        var bottom = new Dictionary<string, int> { ["a"] = 99, ["b"] = 2 };
        var sut = new LayeredDictionary<string, int>(top, bottom);

        var view = new LayeredDictionaryDebugView<string, int>(sut);

        Assert.AreEqual(2, view.Items.Length);
        CollectionAssert.AreEquivalent(
            new[] { new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("b", 2) },
            view.Items);
    }

    /// <summary>
    /// Verifies that <see cref="LayeredDictionaryDebugView{TKey, TValue}.Items" /> is empty when every layer is
    /// empty.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_WhenAllLayersEmpty_ShouldBeEmpty()
    {
        var sut = new LayeredDictionary<string, int>(new Dictionary<string, int>());

        var view = new LayeredDictionaryDebugView<string, int>(sut);

        Assert.AreEqual(0, view.Items.Length);
    }

    /// <summary>
    /// Verifies that constructing <see cref="LayeredDictionaryDebugView{TKey, TValue}" /> with a
    /// <see langword="null" /> dictionary throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenDictionaryIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new LayeredDictionaryDebugView<string, int>(null!);
        });

        Assert.AreEqual("dictionary", ex.ParamName);
    }
}
