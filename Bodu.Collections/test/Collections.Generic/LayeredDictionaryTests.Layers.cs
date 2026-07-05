// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.Layers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that the layer list exposes the underlying dictionaries by reference, in construction order.
    /// </summary>
    [TestMethod]
    public void Layers_WhenConstructed_ShouldExposeLayersInOrderByReference()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out Dictionary<string, int> second);

        Assert.AreEqual(2, view.Layers.Count);
        Assert.AreSame(first, view.Layers[0]);
        Assert.AreSame(second, view.Layers[1]);
    }

    /// <summary>
    /// Verifies that mutating a layer obtained through <see cref="LayeredDictionary{TKey, TValue}.Layers" /> is
    /// visible through the view — the layers remain live references.
    /// </summary>
    [TestMethod]
    public void Layers_WhenLayerMutatedThroughProperty_ShouldBeVisibleThroughView()
    {
        var view = CreatePopulated(out _, out _);

        view.Layers[1]["d"] = 4;

        Assert.AreEqual(4, view["d"]);
    }
}
