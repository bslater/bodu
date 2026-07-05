// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> layer collection throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenLayersIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new LayeredDictionary<string, int>((IEnumerable<IDictionary<string, int>>)null!);
        });

        Assert.AreEqual("layers", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> params array throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenLayersArrayIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new LayeredDictionary<string, int>((IDictionary<string, int>[])null!);
        });

        Assert.AreEqual("layers", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty layer list throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenLayersIsEmpty_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new LayeredDictionary<string, int>();
        });

        Assert.AreEqual("layers", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a layer list containing a <see langword="null" /> element throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenLayersContainsNullElement_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new LayeredDictionary<string, int>(new Dictionary<string, int>(), null!);
        });

        Assert.AreEqual("layers", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a single-layer view is valid and reads through to that layer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSingleLayer_ShouldCreateView()
    {
        var layer = new Dictionary<string, int> { ["a"] = 1 };

        var view = new LayeredDictionary<string, int>(layer);

        Assert.AreEqual(1, view.Layers.Count);
        Assert.AreEqual(1, view["a"]);
    }

    /// <summary>
    /// Verifies that the layer sequence is materialized defensively: mutating the source collection after
    /// construction does not change the view's layer list.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceListMutatedAfterConstruction_ShouldNotAffectLayerList()
    {
        var list = new List<IDictionary<string, int>> { new Dictionary<string, int> { ["a"] = 1 } };
        var view = new LayeredDictionary<string, int>(list);

        list.Add(new Dictionary<string, int> { ["b"] = 2 });

        Assert.AreEqual(1, view.Layers.Count);
        Assert.IsFalse(view.ContainsKey("b"));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> comparer falls back to the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        var view = new LayeredDictionary<string, int>([new Dictionary<string, int>()], null);

        Assert.AreSame(EqualityComparer<string>.Default, view.Comparer);
    }

    /// <summary>
    /// Verifies that a supplied comparer is stored and exposed through
    /// <see cref="LayeredDictionary{TKey, TValue}.Comparer" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerSupplied_ShouldExposeComparer()
    {
        var view = new LayeredDictionary<string, int>(
            [new Dictionary<string, int>()], StringComparer.OrdinalIgnoreCase);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, view.Comparer);
    }
}
