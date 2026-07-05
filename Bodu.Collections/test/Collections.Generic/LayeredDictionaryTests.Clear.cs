// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LayeredDictionaryTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class LayeredDictionaryTests
{
    /// <summary>
    /// Verifies that clearing empties the first layer only, leaving deeper layers untouched and their entries visible
    /// — including previously shadowed ones.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldClearFirstLayerOnlyAndUnshadowDeeperEntries()
    {
        var view = CreatePopulated(out Dictionary<string, int> first, out Dictionary<string, int> second);

        view.Clear();

        Assert.AreEqual(0, first.Count);
        Assert.AreEqual(3, second.Count);
        Assert.AreEqual(3, view.Count);
        Assert.AreEqual(100, view["a"]);
    }
}
