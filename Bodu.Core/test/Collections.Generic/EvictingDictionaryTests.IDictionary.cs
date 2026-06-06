// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Add" /> inserts a new key-value pair when types are valid.
    /// </summary>
    [TestMethod]
    public void IDictionary_Add_WhenKeyAndValueAreValid_ShouldAddItem()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("foo", 7);
        Assert.AreEqual(7, dictionary["foo"]);
    }

    /// <summary>
    /// Verifies that adding an item with an invalid key type throws ArgumentException.
    /// </summary>
    [TestMethod]
    public void IDictionary_Add_WhenKeyIsInvalidType_ShouldThrowExactly()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add(123, 1);
        });
    }

    /// <summary>
    /// Verifies that adding an item with an invalid value type throws ArgumentException.
    /// </summary>
    [TestMethod]
    public void IDictionary_Add_WhenValueIsInvalidType_ShouldThrowExactly()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add("key", "invalid-value");
        });
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Contains" /> returns false for a non-existent key.
    /// </summary>
    [TestMethod]
    public void IDictionary_Contains_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        Assert.DoesNotContain("nope", dictionary);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Contains" /> returns true for an existing key.
    /// </summary>
    [TestMethod]
    public void IDictionary_Contains_WhenKeyExists_ShouldReturnTrue()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("key", 99);
        Assert.Contains("key", dictionary);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Contains" /> returns false when the key is of the wrong type.
    /// </summary>
    [TestMethod]
    public void IDictionary_Contains_WhenKeyIsWrongType_ShouldReturnFalse()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("exists", 1);
        Assert.DoesNotContain(123, dictionary);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.GetEnumerator" /> returns all key-value entries as DictionaryEntry.
    /// </summary>
    [TestMethod]
    public void IDictionary_GetEnumerator_WhenCalled_ShouldReturnAllEntries()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        var entries = new List<string>();
        foreach (DictionaryEntry entry in dictionary)
        {
            entries.Add($"{entry.Key}:{entry.Value}");
        }

        CollectionAssert.Contains(entries, "a:1");
        CollectionAssert.Contains(entries, "b:2");
    }

    /// <summary>
    /// Verifies that the indexer returns null when the key is not present.
    /// </summary>
    [TestMethod]
    public void IDictionary_IndexerGet_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        Assert.IsNull(dictionary["missing"]);
    }

    /// <summary>
    /// Verifies that the indexer returns the value when the key exists.
    /// </summary>
    [TestMethod]
    public void IDictionary_IndexerGet_WhenKeyExists_ShouldReturnCorrectValue()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("one", 1);
        Assert.AreEqual(1, dictionary["one"]);
    }

    /// <summary>
    /// Verifies that a value can be assigned using the indexer with a valid key and value type.
    /// </summary>
    [TestMethod]
    public void IDictionary_IndexerSet_WhenKeyAndValueAreValid_ShouldSetValue()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        dictionary["hello"] = 42;
        Assert.AreEqual(42, dictionary["hello"]);
    }
    /// <summary>
    /// Verifies that setting a value using an invalid key type throws ArgumentException.
    /// </summary>
    [TestMethod]
    public void IDictionary_IndexerSet_WhenKeyIsInvalidType_ShouldThrowExactly()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary[123] = 42;
        });
    }

    /// <summary>
    /// Verifies that setting a value using an invalid value type throws ArgumentException.
    /// </summary>
    [TestMethod]
    public void IDictionary_IndexerSet_WhenValueIsInvalidType_ShouldThrowExactly()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary["hello"] = "not-an-int";
        });
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.IsFixedSize" /> returns false indicating the dictionary is resizable.
    /// </summary>
    [TestMethod]
    public void IDictionary_IsFixedSize_WhenAccessed_ShouldReturnFalse()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        Assert.IsFalse(dictionary.IsFixedSize);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.IsReadOnly" /> returns false indicating the dictionary allows modification.
    /// </summary>
    [TestMethod]
    public void IDictionary_IsReadOnly_WhenAccessed_ShouldReturnFalse()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        Assert.IsFalse(dictionary.IsReadOnly);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Keys" /> returns a collection matching the generic Keys property.
    /// </summary>
    [TestMethod]
    public void IDictionary_Keys_WhenAccessed_ShouldMatchGenericKeys()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        ICollection<string> genericKeys = dictionary.Keys;
        ICollection keys = ((IDictionary)dictionary).Keys;

        CollectionAssert.AreEquivalent(genericKeys.Cast<object>().ToList(), keys.Cast<object>().ToList());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Remove" /> deletes an existing entry when a valid key is provided.
    /// </summary>
    [TestMethod]
    public void IDictionary_Remove_WhenKeyExists_ShouldRemoveEntry()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("key", 1);
        dictionary.Remove("key");
        Assert.DoesNotContain("key", dictionary);
    }

    /// <summary>
    /// Verifies that removing with an invalid key type throws ArgumentException.
    /// </summary>
    [TestMethod]
    public void IDictionary_Remove_WhenKeyIsInvalidType_ShouldThrowExactly()
    {
        IDictionary dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("key", 1);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Remove(100);
        });
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Values" /> returns a collection matching the generic Values property.
    /// </summary>
    [TestMethod]
    public void IDictionary_Values_WhenAccessed_ShouldMatchGenericValues()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        ICollection<int> genericValues = dictionary.Values;
        ICollection values = ((IDictionary)dictionary).Values;

        CollectionAssert.AreEquivalent(genericValues.Cast<object>().ToList(), values.Cast<object>().ToList());
    }

}
