// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that the count reports distinct keys across all layers, counting a shadowed key once.
    /// </summary>
    [TestMethod]
    public void Count_WhenKeysShadowed_ShouldCountDistinctKeysOnce()
    {
        var view = CreatePopulated(out _, out _);

        Assert.AreEqual(3, view.Count);
    }

    /// <summary>
    /// Verifies that the count is computed on demand and reflects direct mutations of the underlying layers.
    /// </summary>
    [TestMethod]
    public void Count_WhenUnderlyingLayerMutatedDirectly_ShouldReflectChange()
    {
        var view = CreatePopulated(out _, out Dictionary<string, int> second);

        second["d"] = 4;

        Assert.AreEqual(4, view.Count);
    }

    /// <summary>
    /// Verifies that the count deduplicates keys using the view's comparer rather than reference or default equality
    /// alone.
    /// </summary>
    [TestMethod]
    public void Count_WhenViewComparerIsCaseInsensitive_ShouldDeduplicateAcrossCasings()
    {
        var first = new Dictionary<string, int>(StringComparer.Ordinal) { ["key"] = 1 };
        var second = new Dictionary<string, int>(StringComparer.Ordinal) { ["KEY"] = 2 };
        var view = new LayeredDictionary<string, int>([first, second], StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(1, view.Count);
    }
}
