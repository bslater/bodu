// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.Comparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies the documented comparer-mismatch behaviour: lookups honour each layer's own comparer while the view's
    /// comparer governs only deduplication, so a key the view considers "the same" can still resolve per-casing
    /// through the layers.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenViewAndLayerComparersDisagree_ShouldLookUpPerLayerButDeduplicatePerView()
    {
        var first = new Dictionary<string, int>(StringComparer.Ordinal) { ["key"] = 1 };
        var second = new Dictionary<string, int>(StringComparer.Ordinal) { ["KEY"] = 2 };
        var view = new LayeredDictionary<string, int>([first, second], StringComparer.OrdinalIgnoreCase);

        // Lookups walk the layers with each layer's own (case-sensitive) comparer.
        Assert.AreEqual(1, view["key"]);
        Assert.AreEqual(2, view["KEY"]);
        Assert.IsFalse(view.TryGetValue("Key", out _));

        // The distinct-key view deduplicates with the case-insensitive comparer: "KEY" is shadowed by "key".
        Assert.AreEqual(1, view.Count);
        CollectionAssert.AreEquivalent(new[] { "key" }, view.Keys.ToList());
    }

    /// <summary>
    /// Verifies that a case-insensitive first layer resolves keys the view's default comparer considers distinct,
    /// pinning the documented shadowing surprise.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenFirstLayerIsCaseInsensitive_ShouldResolveEitherCasingFromFirstLayer()
    {
        var first = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["key"] = 1 };
        var second = new Dictionary<string, int> { ["KEY"] = 2 };
        var view = new LayeredDictionary<string, int>(first, second);

        // The first layer answers for both casings, so the deeper entry is never reached.
        Assert.AreEqual(1, view["key"]);
        Assert.AreEqual(1, view["KEY"]);

        // The view's default (case-sensitive) comparer still sees two distinct keys during deduplication.
        Assert.AreEqual(2, view.Count);
    }
}
