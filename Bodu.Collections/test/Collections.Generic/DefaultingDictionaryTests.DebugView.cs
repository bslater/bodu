// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionaryTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DefaultingDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="DefaultingDictionaryDebugView{TKey, TValue}.Items" /> exposes every
    /// materialized entry.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldExposeEveryMaterializedEntry()
    {
        var sut = new DefaultingDictionary<string, int>(_ => -1)
        {
            ["a"] = 1,
            ["b"] = 2,
        };

        var view = new DefaultingDictionaryDebugView<string, int>(sut);

        Assert.AreEqual(2, view.Items.Length);
        CollectionAssert.AreEquivalent(
            new[] { new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("b", 2) },
            view.Items);
    }

    /// <summary>
    /// Verifies that <see cref="DefaultingDictionaryDebugView{TKey, TValue}.Items" /> shows only entries that
    /// have actually been materialized, not values the factory could still synthesize on demand.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_WhenNothingMaterialized_ShouldBeEmpty()
    {
        var view = new DefaultingDictionaryDebugView<string, int>(new DefaultingDictionary<string, int>(_ => -1));

        Assert.AreEqual(0, view.Items.Length);
    }

    /// <summary>
    /// Verifies that constructing <see cref="DefaultingDictionaryDebugView{TKey, TValue}" /> with a
    /// <see langword="null" /> dictionary throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenDictionaryIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DefaultingDictionaryDebugView<string, int>(null!);
        });

        Assert.AreEqual("dictionary", ex.ParamName);
    }
}
