// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Ctor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    private readonly Dictionary<string, int> _source = new()
    {
        ["a"] = 1,
        ["b"] = 2,
        ["c"] = 3,
        ["d"] = 4,
        ["e"] = 5,
    };

    /// <summary>
    /// Verifies that the constructor throws if capacity is negative.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new EvictingDictionary<string, int>(-5);
        });
    }

    /// <summary>
    /// Verifies that the constructor throws if capacity is zero.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsZero_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new EvictingDictionary<string, int>(0);
        });
    }

    /// <summary>
    /// Verifies that the constructor uses default equality comparer when null is passed.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        var dictionary = new EvictingDictionary<string, int>(5, comparer: null);
        dictionary.Add("KEY", 123);

        Assert.IsFalse(dictionary.ContainsKey("key")); // default is case-sensitive
    }

    /// <summary>
    /// Verifies that the constructor uses the provided equality comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsProvided_ShouldUseComparer()
    {
        var dictionary = new EvictingDictionary<string, int>(5, StringComparer.OrdinalIgnoreCase);
        dictionary.Add("KEY", 123);

        Assert.IsTrue(dictionary.ContainsKey("key"));
    }

    /// <summary>
    /// Verifies that the constructor throws when capacity is negative and source is empty.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEmptySourceAndNegativeCapacity_ShouldThrowExactly()
    {
        var empty = new Dictionary<string, int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new EvictingDictionary<string, int>(-1, empty);
        });
    }

    /// <summary>
    /// Verifies that the constructor sets the specified capacity and defaults to LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGivenCapacity_ShouldUseDefaultLRUPolicy()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        Assert.AreEqual(5, dictionary.Capacity);
        Assert.AreEqual(0, dictionary.Count);
        Assert.AreEqual(EvictingDictionaryPolicy.LeastRecentlyUsed, dictionary.Policy);
    }

    /// <summary>
    /// Verifies that the constructor applies the specified eviction policy and capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGivenCapacityAndPolicy_ShouldUseSpecifiedPolicy()
    {
        var dictionary = new EvictingDictionary<string, int>(5, EvictingDictionaryPolicy.FirstInFirstOut);
        Assert.AreEqual(5, dictionary.Capacity);
        Assert.AreEqual(0, dictionary.Count);
        Assert.AreEqual(EvictingDictionaryPolicy.FirstInFirstOut, dictionary.Policy);
    }

    /// <summary>
    /// Verifies that the constructor adopts contents from a dictionary and applies default capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGivenDictionary_ShouldAdoptContentsAndUseDefaultCapacity()
    {
        var dictionary = new EvictingDictionary<string, int>(_source);
        CollectionAssert.AreEquivalent(_source, dictionary.ToArray());
        Assert.AreEqual(_source.Count, dictionary.Count);
        Assert.AreEqual(EvictingDictionaryTests.DefaultCapacity, dictionary.Capacity);
    }

    /// <summary>
    /// Verifies that the constructor applies dictionary contents, capacity, and policy correctly.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGivenDictionaryCapacityAndPolicy_ShouldRespectAllParameters()
    {
        var dictionary = new EvictingDictionary<string, int>(4, _source, EvictingDictionaryPolicy.FirstInFirstOut);
        var expected = _source.Skip(1).ToDictionary(p => p.Key, p => p.Value);

        Assert.AreEqual(4, dictionary.Capacity);
        Assert.AreEqual(4, dictionary.Count);
        CollectionAssert.AreEquivalent(expected, dictionary.ToArray());
    }

    /// <summary>
    /// Verifies that the constructor adopts dictionary contents using default policy and capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGivenDictionaryOnly_ShouldUseDefaults()
    {
        var buffer = new EvictingDictionary<string, int>(_source);
        CollectionAssert.AreEqual(_source, buffer.ToArray());
        Assert.AreEqual(EvictingDictionaryPolicy.LeastRecentlyUsed, buffer.Policy);
        Assert.AreEqual(EvictingDictionaryTests.DefaultCapacity, buffer.Capacity);
    }

    /// <summary>
    /// Verifies that the constructor applies the specified policy and default capacity from IEnumerable.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGivenSourceAndPolicy_ShouldApplyDefaultsAndPolicy()
    {
        IEnumerable<KeyValuePair<string, int>> source = Enumerable.Range(1, 20).Select(i => new KeyValuePair<string, int>($"Key{i}", i));
        var expected = source.Skip(source.Count() - EvictingDictionaryTests.DefaultCapacity).ToDictionary(p => p.Key, p => p.Value);
        var dictionary = new EvictingDictionary<string, int>(source, EvictingDictionaryPolicy.FirstInFirstOut);

        Assert.AreEqual(EvictingDictionaryPolicy.FirstInFirstOut, dictionary.Policy);
        Assert.AreEqual(EvictingDictionaryTests.DefaultCapacity, dictionary.Capacity);
        CollectionAssert.AreEqual(expected, dictionary.ToArray());
    }

    /// <summary>
    /// Verifies that entries are evicted when the source exceeds the specified capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceExceedsCapacity_ShouldEvictOldest()
    {
        var dictionary = new EvictingDictionary<string, int>(3, _source, EvictingDictionaryPolicy.LeastRecentlyUsed);
        var expected = _source.Skip(2).ToDictionary(p => p.Key, p => p.Value);

        Assert.AreEqual(3, dictionary.Capacity);
        Assert.AreEqual(3, dictionary.Count);
        CollectionAssert.AreEquivalent(expected, dictionary.ToArray());
    }

    /// <summary>
    /// Verifies that the constructor copies contents directly from an array.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsArray_ShouldUseDirectCopy()
    {
        KeyValuePair<int, int>[] array =
        [
            new KeyValuePair<int, int>(1, 100),
            new KeyValuePair<int, int>(2, 200),
        ];
        var dictionary = new EvictingDictionary<int, int>(5, array);
        CollectionAssert.AreEqual(array, dictionary.ToArray());
    }

    /// <summary>
    /// Verifies that the constructor materializes an IEnumerable into a dictionary.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsEnumerable_ShouldMaterializeIntoDictionary()
    {
        IEnumerable<KeyValuePair<int, int>> enumerable = Enumerable.Range(1, 3).Select(i => new KeyValuePair<int, int>(i, i * 10));
        var dictionary = new EvictingDictionary<int, int>(5, enumerable);

        CollectionAssert.AreEqual(new[]
        {
            new KeyValuePair<int, int>(1, 10),
            new KeyValuePair<int, int>(2, 20),
            new KeyValuePair<int, int>(3, 30)
        }, dictionary.ToArray());
    }

    /// <summary>
    /// Verifies that the constructor throws when a null source is provided.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new EvictingDictionary<string, int>(5, (IEnumerable<KeyValuePair<string, int>>)null!);
        });
    }

    /// <summary>
    /// Verifies that the parameterless constructor uses default capacity and LeastRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenUsingDefaultCtor_ShouldUseDefaults()
    {
        var dictionary = new EvictingDictionary<string, int>();
        Assert.AreEqual(16, dictionary.Capacity);
        Assert.AreEqual(EvictingDictionaryPolicy.LeastRecentlyUsed, dictionary.Policy);
        Assert.AreEqual(0, dictionary.Count);
    }

}
