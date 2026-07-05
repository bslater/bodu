// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that enumeration yields distinct keys with first-wins values, skipping shadowed deeper entries.
    /// </summary>
    [TestMethod]
    public void Enumeration_WhenKeysShadowed_ShouldYieldDistinctKeysWithFirstWinsValues()
    {
        var view = CreatePopulated(out _, out _);

        var merged = view.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Assert.AreEqual(3, merged.Count);
        Assert.AreEqual(1, merged["a"]);
        Assert.AreEqual(2, merged["b"]);
        Assert.AreEqual(3, merged["c"]);
    }

    /// <summary>
    /// Verifies that enumeration walks the layers in order: first-layer entries appear before deeper-only entries.
    /// </summary>
    [TestMethod]
    public void Enumeration_WhenMultipleLayers_ShouldWalkLayersInOrder()
    {
        var view = CreatePopulated(out _, out _);

        var pairs = view.ToList();

        Assert.AreEqual(new KeyValuePair<string, int>("a", 1), pairs[0]);
        CollectionAssert.AreEquivalent(
            new[]
            {
                new KeyValuePair<string, int>("b", 2),
                new KeyValuePair<string, int>("c", 3),
            },
            pairs.Skip(1).ToList());
    }

    /// <summary>
    /// Verifies that the view is live: entries added directly to an underlying layer after construction are visible
    /// in a subsequent enumeration.
    /// </summary>
    [TestMethod]
    public void Enumeration_WhenUnderlyingLayerMutatedDirectly_ShouldReflectChange()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out Dictionary<string, int> second);

        first["d"] = 4;
        second["e"] = 5;

        var merged = view.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Assert.AreEqual(5, merged.Count);
        Assert.AreEqual(4, merged["d"]);
        Assert.AreEqual(5, merged["e"]);
    }

    /// <summary>
    /// Verifies that copying to an array writes the merged first-wins view at the requested offset.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArraySufficient_ShouldCopyMergedView()
    {
        var view = CreatePopulated(out _, out _);
        var array = new KeyValuePair<string, int>[4];

        view.CopyTo(array, 1);

        Assert.AreEqual(default, array[0]);
        CollectionAssert.AreEquivalent(
            new[]
            {
                new KeyValuePair<string, int>("a", 1),
                new KeyValuePair<string, int>("b", 2),
                new KeyValuePair<string, int>("c", 3),
            },
            array.Skip(1).ToList());
    }
}
