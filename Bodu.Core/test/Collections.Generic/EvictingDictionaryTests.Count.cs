// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Count.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Count" /> becomes zero after calling Clear on the dictionary.
    /// </summary>
    [TestMethod]
    public void Count_WhenCleared_ShouldBeZero()
    {
        var dictionary = new EvictingDictionary<string, int>(4);
        dictionary.Add("one", 1);
        dictionary.Add("two", 2);
        dictionary.Clear();

        Assert.AreEqual(0, dictionary.Count);
    }
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Count" /> is zero for a newly constructed dictionary.
    /// </summary>
    [TestMethod]
    public void Count_WhenConstructed_ShouldBeZero()
    {
        var dictionary = new EvictingDictionary<string, int>(4);
        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Count" /> decreases when an item is removed from the dictionary.
    /// </summary>
    [TestMethod]
    public void Count_WhenItemIsRemoved_ShouldDecrease()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("one", 1);
        dictionary.Add("two", 2);
        dictionary.Add("three", 3);
        dictionary.Remove("two");

        Assert.AreEqual(2, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Count" /> increases to reflect the number of items added.
    /// </summary>
    [TestMethod]
    public void Count_WhenItemsAreAdded_ShouldReflectCorrectCount()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("one", 1);
        dictionary.Add("two", 2);
        dictionary.Add("three", 3);

        Assert.AreEqual(3, dictionary.Count);
    }

}
