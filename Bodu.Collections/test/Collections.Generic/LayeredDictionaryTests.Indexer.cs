// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that the getter returns the first layer's value when the key exists in multiple layers.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyShadowed_ShouldReturnFirstLayerValue()
    {
        var view = CreatePopulated(out _, out _);

        Assert.AreEqual(1, view["a"]);
    }

    /// <summary>
    /// Verifies that the getter falls through to a deeper layer when the first layer does not contain the key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyOnlyInDeeperLayer_ShouldReturnDeeperValue()
    {
        var view = CreatePopulated(out _, out _);

        Assert.AreEqual(2, view["b"]);
    }

    /// <summary>
    /// Verifies that reading a key absent from every layer throws <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyMissingFromAllLayers_ShouldThrowKeyNotFoundException()
    {
        var view = CreatePopulated(out _, out _);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = view["missing"];
        });
    }

    /// <summary>
    /// Verifies that setting a key writes to the first layer and shadows the deeper layer's entry without mutating it.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetForDeeperKey_ShouldWriteFirstLayerAndShadowDeeper()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out Dictionary<string, int> second);

        view["b"] = 20;

        Assert.AreEqual(20, view["b"]);
        Assert.AreEqual(20, first["b"]);
        Assert.AreEqual(2, second["b"]);
    }

    /// <summary>
    /// Verifies that setting an existing first-layer key updates the first layer in place.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetForExistingFirstLayerKey_ShouldUpdateFirstLayer()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out _);

        view["a"] = 10;

        Assert.AreEqual(10, view["a"]);
        Assert.AreEqual(10, first["a"]);
    }
}
