// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Count.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Count"/> is correctly decremented after <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/>.
    /// </summary>
    [TestMethod]
    public void Count_AfterRemoveAll_ShouldReflectRemovedValues()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("a", 2);
        mvd.Add("a", 3);
        mvd.Add("b", 4);

        mvd.RemoveAll("a");

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
    }
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Count"/> reflects the total value entries.
    /// </summary>
    [TestMethod]
    public void Count_WhenItemsAdded_ShouldReflectTotalValueEntries()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("a", 2);
        mvd.Add("b", 3);

        Assert.AreEqual(3, mvd.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Count"/> decrements when the last value is removed from a key, eliminating the key entry.
    /// </summary>
    [TestMethod]
    public void Count_WhenLastValueRemovedFromKey_ShouldDecrementByOne()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 42);

        mvd.Remove("k", 42);

        Assert.AreEqual(0, mvd.Count);
    }

}
