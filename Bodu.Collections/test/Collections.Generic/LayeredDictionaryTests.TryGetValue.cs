// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that a shadowed key resolves to the first layer's value.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyShadowed_ShouldReturnFirstLayerValue()
    {
        var view = CreatePopulated(out _, out _);

        Assert.IsTrue(view.TryGetValue("a", out int value));
        Assert.AreEqual(1, value);
    }

    /// <summary>
    /// Verifies that a key present only in a deeper layer resolves to that layer's value.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyOnlyInDeeperLayer_ShouldReturnDeeperValue()
    {
        var view = CreatePopulated(out _, out _);

        Assert.IsTrue(view.TryGetValue("c", out int value));
        Assert.AreEqual(3, value);
    }

    /// <summary>
    /// Verifies that a key absent from every layer returns <see langword="false" /> with the default value.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyMissingFromAllLayers_ShouldReturnFalse()
    {
        var view = CreatePopulated(out _, out _);

        Assert.IsFalse(view.TryGetValue("missing", out int value));
        Assert.AreEqual(0, value);
    }
}
