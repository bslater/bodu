// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Values.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that mutating the dictionary through the Values collection is not supported.
    /// </summary>
    [TestMethod]
    public void Values_Add_ShouldThrowException()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        ICollection<int> values = dictionary.Values;

        Assert.ThrowsExactly<NotSupportedException>(() => values.Add(42));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Values" />.Contains uses the default equality comparer for the value type.
    /// </summary>
    [TestMethod]
    public void Values_Contains_WhenValuePresent_ShouldReturnTrue()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 10);
        dictionary.Add("B", 20);

        Assert.IsTrue(dictionary.Values.Contains(20));
        Assert.IsFalse(dictionary.Values.Contains(99));
    }

    /// <summary>
    /// Verifies that the Values property returns the same cached instance on successive reads.
    /// </summary>
    [TestMethod]
    public void Values_Get_WhenReadTwice_ShouldReturnSameInstance()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);

        ICollection<int> first = dictionary.Values;
        ICollection<int> second = dictionary.Values;

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Values" /> returns an empty collection after the dictionary is cleared.
    /// </summary>
    [TestMethod]
    public void Values_WhenDictionaryIsCleared_ShouldBeEmpty()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 10);
        dictionary.Add("B", 20);

        dictionary.Clear();

        Assert.AreEqual(0, dictionary.Values.Count);
    }
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Values" /> returns an empty collection when the dictionary has no entries.
    /// </summary>
    [TestMethod]
    public void Values_WhenDictionaryIsEmpty_ShouldReturnEmptyCollection()
    {
        var dictionary = new EvictingDictionary<string, int>(3);

        Assert.AreEqual(0, dictionary.Values.Count);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Values" /> does not contain the value of a key that was evicted when capacity was exceeded.
    /// </summary>
    [TestMethod]
    public void Values_WhenItemIsEvicted_ShouldNotContainEvictedValue()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("A", 10);
        dictionary.Add("B", 20);
        dictionary.Add("C", 30); // evicts A

        CollectionAssert.DoesNotContain(dictionary.Values.ToList(), 10);
        CollectionAssert.Contains(dictionary.Values.ToList(), 20);
        CollectionAssert.Contains(dictionary.Values.ToList(), 30);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Values" /> does not contain the value of an explicitly removed key.
    /// </summary>
    [TestMethod]
    public void Values_WhenItemIsRemoved_ShouldNotContainRemovedValue()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 10);
        dictionary.Add("B", 20);

        dictionary.Remove("A");

        CollectionAssert.DoesNotContain(dictionary.Values.ToList(), 10);
        CollectionAssert.Contains(dictionary.Values.ToList(), 20);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Values" /> contains all values for inserted items when capacity is not exceeded.
    /// </summary>
    [TestMethod]
    public void Values_WhenItemsAdded_ShouldContainAllValues()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("A", 10);
        dictionary.Add("B", 20);
        dictionary.Add("C", 30);

        ICollection<int> values = dictionary.Values;

        Assert.AreEqual(3, values.Count);
        CollectionAssert.Contains(values.ToList(), 10);
        CollectionAssert.Contains(values.ToList(), 20);
        CollectionAssert.Contains(values.ToList(), 30);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Values" /> can contain duplicate values across different keys.
    /// </summary>
    [TestMethod]
    public void Values_WhenMultipleKeysHaveSameValue_ShouldContainDuplicateValues()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("A", 42);
        dictionary.Add("B", 42);

        var values = dictionary.Values.ToList();

        Assert.AreEqual(2, values.Count(v => v == 42));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Values" /> reflects the updated value when a key is replaced via the indexer.
    /// </summary>
    [TestMethod]
    public void Values_WhenValueIsUpdatedViaIndexer_ShouldReflectNewValue()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 10);
        dictionary["A"] = 99;

        CollectionAssert.Contains(dictionary.Values.ToList(), 99);
        CollectionAssert.DoesNotContain(dictionary.Values.ToList(), 10);
    }

}
