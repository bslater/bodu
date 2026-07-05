// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.ContainsKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that a key present in the first layer is reported as contained.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyInFirstLayer_ShouldReturnTrue()
    {
        var view = CreatePopulated(out _, out _);

        Assert.IsTrue(view.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that a key present only in a deeper layer is reported as contained.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyOnlyInDeeperLayer_ShouldReturnTrue()
    {
        var view = CreatePopulated(out _, out _);

        Assert.IsTrue(view.ContainsKey("b"));
    }

    /// <summary>
    /// Verifies that a key absent from every layer is not reported as contained.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyMissingFromAllLayers_ShouldReturnFalse()
    {
        var view = CreatePopulated(out _, out _);

        Assert.IsFalse(view.ContainsKey("missing"));
    }
}
