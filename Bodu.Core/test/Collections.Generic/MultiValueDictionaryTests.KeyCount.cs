// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.KeyCount.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.KeyCount"/> reflects only the number of distinct keys.
    /// </summary>
    [TestMethod]
    public void KeyCount_WhenItemsAdded_ShouldReflectDistinctKeyCount()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("a", 2);
        mvd.Add("b", 3);

        Assert.AreEqual(2, mvd.KeyCount);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.KeyCount"/> decrements to zero after the last value is removed from the only key.
    /// </summary>
    [TestMethod]
    public void KeyCount_WhenLastValueRemovedFromKey_ShouldDecrementToZero()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 42);

        mvd.Remove("k", 42);

        Assert.AreEqual(0, mvd.KeyCount);
    }
}
