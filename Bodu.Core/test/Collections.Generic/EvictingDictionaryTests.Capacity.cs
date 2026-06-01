// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Capacity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that constructing from a Dictionary uses the default capacity.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenConstructedFromDictionary_ShouldUseDefaultCapacity()
    {
        var source = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        };

        var dictionary = new EvictingDictionary<string, int>(source);
        Assert.AreEqual(EvictingDictionaryTests.DefaultCapacity, dictionary.Capacity);
    }

    /// <summary>
    /// Verifies that constructing from a dictionary with an explicit capacity uses the provided capacity.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenConstructedFromDictionaryWithExplicitCapacity_ShouldRespectProvidedValue()
    {
        var source = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        };

        var dictionary = new EvictingDictionary<string, int>(2, source, EvictingDictionaryPolicy.FirstInFirstOut);
        Assert.AreEqual(2, dictionary.Capacity);
    }

    /// <summary>
    /// Verifies that constructing from an IEnumerable and eviction policy uses the default capacity.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenConstructedFromIEnumerableAndPolicy_ShouldUseDefaultCapacity()
    {
        var source = new List<KeyValuePair<string, int>>
        {
            new("a", 1),
            new("b", 2),
        };

        var dictionary = new EvictingDictionary<string, int>(source, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        Assert.AreEqual(EvictingDictionaryTests.DefaultCapacity, dictionary.Capacity);
    }
    /// <summary>
    /// Verifies that the Capacity property matches the value passed to the constructor.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenConstructedWithExplicitValue_ShouldMatchConstructorParameter()
    {
        var dictionary = new EvictingDictionary<string, int>(42);
        Assert.AreEqual(42, dictionary.Capacity);
    }

    /// <summary>
    /// Verifies that the parameterless constructor sets the default capacity.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenUsingParameterlessCtor_ShouldUseDefaultCapacity()
    {
        var dictionary = new EvictingDictionary<string, int>();
        Assert.AreEqual(EvictingDictionaryTests.DefaultCapacity, dictionary.Capacity);
    }

}
