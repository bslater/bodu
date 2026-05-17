// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Keys.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that mutating the dictionary through the Keys collection is not supported and throws.
    /// </summary>
    [TestMethod]
    public void Keys_Add_ShouldThrowException()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        ICollection<string> keys = dictionary.Keys;

        Assert.ThrowsExactly<NotSupportedException>(() => keys.Add("Z"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Keys" /> returns an empty collection after the dictionary is cleared.
    /// </summary>
    [TestMethod]
    public void Keys_Get_WhenDictionaryIsCleared_ShouldBeEmpty()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        dictionary.Clear();

        Assert.IsEmpty(dictionary.Keys);
    }
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Keys" /> returns an empty collection when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void Keys_Get_WhenEmpty_ShouldReturnEmptyCollection()
    {
        var dictionary = new EvictingDictionary<string, int>(3);

        Assert.IsEmpty(dictionary.Keys);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Keys" /> does not contain a key that was evicted when capacity was exceeded.
    /// </summary>
    [TestMethod]
    public void Keys_Get_WhenItemIsEvicted_ShouldNotContainEvictedKey()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3); // evicts A

        CollectionAssert.DoesNotContain(dictionary.Keys.ToList(), "A");
        CollectionAssert.Contains(dictionary.Keys.ToList(), "B");
        CollectionAssert.Contains(dictionary.Keys.ToList(), "C");
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Keys" /> does not contain a key that has been explicitly removed.
    /// </summary>
    [TestMethod]
    public void Keys_Get_WhenItemIsRemoved_ShouldNotContainRemovedKey()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        dictionary.Remove("A");

        CollectionAssert.DoesNotContain(dictionary.Keys.ToList(), "A");
        CollectionAssert.Contains(dictionary.Keys.ToList(), "B");
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Keys" /> contains all inserted keys when the dictionary has not exceeded capacity.
    /// </summary>
    [TestMethod]
    public void Keys_Get_WhenItemsAdded_ShouldContainAllInsertedKeys()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        ICollection<string> keys = dictionary.Keys;

        Assert.HasCount(3, keys);
        CollectionAssert.Contains(keys.ToList(), "A");
        CollectionAssert.Contains(keys.ToList(), "B");
        CollectionAssert.Contains(keys.ToList(), "C");
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Keys" /> reflects the count of unique keys when a key is re-inserted.
    /// </summary>
    [TestMethod]
    public void Keys_Get_WhenKeyIsReinserted_ShouldNotContainDuplicates()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        dictionary.Add("A", 2); // replace

        Assert.HasCount(1, dictionary.Keys);
    }

    /// <summary>
    /// Verifies that the Keys property returns the same cached instance on successive reads, so callers
    /// observing only the reference do not pay an allocation per access.
    /// </summary>
    [TestMethod]
    public void Keys_Get_WhenReadTwice_ShouldReturnSameInstance()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);

        ICollection<string> first = dictionary.Keys;
        ICollection<string> second = dictionary.Keys;

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that the Keys view reflects mutations to the owning dictionary without requiring a fresh property read.
    /// </summary>
    [TestMethod]
    public void Keys_WhenDictionaryIsMutatedAfterRead_ShouldReflectLiveState()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("A", 1);

        ICollection<string> keys = dictionary.Keys;
        dictionary.Add("B", 2);

        Assert.HasCount(2, keys);
        CollectionAssert.Contains(keys.ToList(), "B");
    }

}
