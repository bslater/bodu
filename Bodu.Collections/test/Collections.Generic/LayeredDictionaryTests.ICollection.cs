// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.ICollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that the view reports itself as writable, because writes are routed to the first layer.
    /// </summary>
    [TestMethod]
    public void IsReadOnly_WhenQueried_ShouldReturnFalse()
    {
        var view = CreatePopulated(out _, out _);

        Assert.IsFalse(view.IsReadOnly);
    }

    /// <summary>
    /// Verifies that pair containment matches the view's first-wins resolution: a pair carrying a shadowed deeper
    /// value does not match.
    /// </summary>
    [TestMethod]
    public void Contains_WhenPairValueIsShadowed_ShouldReturnFalse()
    {
        var view = CreatePopulated(out _, out _);

        Assert.IsTrue(view.Contains(new KeyValuePair<string, int>("a", 1)));
        Assert.IsFalse(view.Contains(new KeyValuePair<string, int>("a", 100)));
        Assert.IsTrue(view.Contains(new KeyValuePair<string, int>("b", 2)));
        Assert.IsFalse(view.Contains(new KeyValuePair<string, int>("missing", 0)));
    }
}
