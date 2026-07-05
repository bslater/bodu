// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Creates a two-layer view where the first layer holds <c>("a", 1)</c> and the second layer holds
    /// <c>("a", 100)</c> (shadowed), <c>("b", 2)</c>, and <c>("c", 3)</c>.
    /// </summary>
    /// <param name="first">When this method returns, contains the first (write) layer.</param>
    /// <param name="second">When this method returns, contains the second (fallback) layer.</param>
    /// <returns>A populated <see cref="LayeredDictionary{TKey, TValue}" /> over the two layers.</returns>
    private static LayeredDictionary<string, int> CreatePopulated(
        out Dictionary<string, int> first,
        out Dictionary<string, int> second)
    {
        first = new Dictionary<string, int> { ["a"] = 1 };
        second = new Dictionary<string, int> { ["a"] = 100, ["b"] = 2, ["c"] = 3 };
        return new LayeredDictionary<string, int>(first, second);
    }

    /// <summary>
    /// Verifies that reads resolve first-wins across the layers and that writes land in the first layer only.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void LayeredDictionary_WhenLayered_ShouldReadFirstWinsAndWriteFirstLayer()
    {
        var overrides = new Dictionary<string, int>();
        var defaults = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        var view = new LayeredDictionary<string, int>(overrides, defaults);

        Assert.AreEqual(1, view["a"]);

        view["a"] = 10;

        Assert.AreEqual(10, view["a"]);
        Assert.AreEqual(10, overrides["a"]);
        Assert.AreEqual(1, defaults["a"]);
        Assert.AreEqual(2, view.Count);
    }
}
