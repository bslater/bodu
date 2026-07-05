// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that adding a new key writes to the first layer only.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNew_ShouldWriteFirstLayerOnly()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out Dictionary<string, int> second);

        view.Add("d", 4);

        Assert.AreEqual(4, view["d"]);
        Assert.AreEqual(4, first["d"]);
        Assert.IsFalse(second.ContainsKey("d"));
    }

    /// <summary>
    /// Verifies that adding a key already present in the first layer throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyInFirstLayer_ShouldThrowArgumentException()
    {
        var view = CreatePopulated(out _, out _);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            view.Add("a", 10);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that adding a key that exists only in deeper layers succeeds and shadows the deeper value — the
    /// duplicate check consults the first layer only.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyOnlyInDeeperLayer_ShouldSucceedAndShadowDeeperValue()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out Dictionary<string, int> second);

        view.Add("b", 20);

        Assert.AreEqual(20, view["b"]);
        Assert.AreEqual(20, first["b"]);
        Assert.AreEqual(2, second["b"]);
    }

    /// <summary>
    /// Verifies that the key/value pair overload writes to the first layer only.
    /// </summary>
    [TestMethod]
    public void Add_WhenPairSupplied_ShouldWriteFirstLayerOnly()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out _);

        view.Add(new KeyValuePair<string, int>("d", 4));

        Assert.AreEqual(4, first["d"]);
    }

    /// <summary>
    /// Verifies that adding a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var view = CreatePopulated(out _, out _);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            view.Add(null!, 1);
        });

        Assert.AreEqual("key", ex.ParamName);
    }
}
