// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that removing a first-layer key that shadowed a deeper entry unshadows the deeper value: the key
    /// remains visible with the next layer's value.
    /// </summary>
    [TestMethod]
    public void Remove_WhenFirstLayerEntryShadowedDeeperEntry_ShouldUnshadowDeeperValue()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out _);

        Assert.IsTrue(view.Remove("a"));

        Assert.IsFalse(first.ContainsKey("a"));
        Assert.IsTrue(view.ContainsKey("a"));
        Assert.AreEqual(100, view["a"]);
    }

    /// <summary>
    /// Verifies that removing a key that exists only in deeper layers returns <see langword="false" /> and leaves the
    /// key visible through the view.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyOnlyInDeeperLayer_ShouldReturnFalseAndKeepKeyVisible()
    {
        var view = CreatePopulated(out _, out Dictionary<string, int> second);

        Assert.IsFalse(view.Remove("b"));

        Assert.IsTrue(second.ContainsKey("b"));
        Assert.IsTrue(view.ContainsKey("b"));
        Assert.AreEqual(2, view["b"]);
    }

    /// <summary>
    /// Verifies that removing a key absent from every layer returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyMissingFromAllLayers_ShouldReturnFalse()
    {
        var view = CreatePopulated(out _, out _);

        Assert.IsFalse(view.Remove("missing"));
    }

    /// <summary>
    /// Verifies that removing a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var view = CreatePopulated(out _, out _);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = view.Remove(null!);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the pair overload removes only a first-layer pair matched by the view and never mutates deeper
    /// layers.
    /// </summary>
    [TestMethod]
    public void Remove_WhenPairMatchesViewAndFirstLayer_ShouldRemoveFromFirstLayerOnly()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out Dictionary<string, int> second);

        Assert.IsTrue(view.Remove(new KeyValuePair<string, int>("a", 1)));
        Assert.IsFalse(first.ContainsKey("a"));
        Assert.AreEqual(100, second["a"]);

        Assert.IsFalse(view.Remove(new KeyValuePair<string, int>("b", 2)));
        Assert.AreEqual(2, second["b"]);
    }

    /// <summary>
    /// Verifies that the pair overload returns <see langword="false" /> when the pair's value matches only a shadowed
    /// deeper entry rather than the view's first-wins value.
    /// </summary>
    [TestMethod]
    public void Remove_WhenPairValueMatchesShadowedEntry_ShouldReturnFalse()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out _);

        Assert.IsFalse(view.Remove(new KeyValuePair<string, int>("a", 100)));
        Assert.AreEqual(1, first["a"]);
    }
}
