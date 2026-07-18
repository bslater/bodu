// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that the dictionary answers reads and writes through the
    /// <see cref="IDictionary{TKey, TValue}" /> interface surface.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenAccessedThroughInterface_ShouldAnswerReadsAndWrites()
    {
        IDictionary<string, int> dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        dictionary["a"] = 1;
        dictionary["b"] = 2;

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.IsReadOnly);
        Assert.IsTrue(dictionary.ContainsKey("a"));
        Assert.IsTrue(dictionary.TryGetValue("b", out int value));
        Assert.AreEqual(2, value);
        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary{TKey, TValue}.Keys" /> and
    /// <see cref="IDictionary{TKey, TValue}.Values" /> return read-only collections containing the stored entries.
    /// </summary>
    [TestMethod]
    public void IDictionary_KeysAndValues_ShouldReturnReadOnlyCollections()
    {
        IDictionary<string, int> dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        ICollection<string> keys = dictionary.Keys;
        ICollection<int> values = dictionary.Values;

        Assert.HasCount(2, keys);
        Assert.HasCount(2, values);
        Assert.IsTrue(keys.IsReadOnly, "The IDictionary keys view must be read-only.");
        Assert.IsTrue(values.IsReadOnly, "The IDictionary values view must be read-only.");
        Assert.Contains("a", keys);
        Assert.Contains(2, values);
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary{TKey, TValue}.Add(TKey, TValue)" /> throws
    /// <see cref="ArgumentException" /> when a live entry for the key already exists.
    /// </summary>
    [TestMethod]
    public void IDictionaryAdd_WhenKeyAlreadyExists_ShouldThrowArgumentException()
    {
        IDictionary<string, int> dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add("a", 2);
        });

        Assert.AreEqual("key", ex.ParamName);
        Assert.AreEqual(1, dictionary["a"], "The rejected add must not overwrite the existing value.");
    }

    /// <summary>
    /// Verifies that <see cref="IDictionary{TKey, TValue}.Add(TKey, TValue)" /> adds a new key/value pair when the key
    /// is absent.
    /// </summary>
    [TestMethod]
    public void IDictionaryAdd_WhenKeyMissing_ShouldAddEntry()
    {
        IDictionary<string, int> dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        dictionary.Add("a", 1);

        Assert.AreEqual(1, dictionary["a"]);
        Assert.AreEqual(1, dictionary.Count);
    }

    /// <summary>
    /// Verifies that the public add-or-replace <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Add(TKey, TValue)" />
    /// remains distinct from the add-or-throw interface contract and overwrites an existing value.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldReplaceValue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        dictionary.Add("a", 2);

        Assert.AreEqual(2, dictionary["a"], "The public Add remains add-or-replace and must not throw on duplicates.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Remove(TKey)" /> removes an existing entry
    /// and reports success.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyExists_ShouldRemoveAndReturnTrue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        bool removed = dictionary.Remove("a");

        Assert.IsTrue(removed);
        Assert.IsFalse(dictionary.ContainsKey("a"));
        Assert.AreEqual(1, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Remove(TKey)" /> reports failure for a key
    /// that is not present.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyMissing_ShouldReturnFalse()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        bool removed = dictionary.Remove("z");

        Assert.IsFalse(removed);
        Assert.AreEqual(1, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Remove(TKey)" /> throws
    /// <see cref="ArgumentNullException" /> for a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.Remove(null!);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an explicit removal via <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Remove(TKey)" /> is
    /// not counted as an eviction.
    /// </summary>
    [TestMethod]
    public void Remove_WhenEntryRemoved_ShouldNotRaiseItemEvicted()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);
        int evictions = 0;
        dictionary.ItemEvicted += (_, _) => Interlocked.Increment(ref evictions);

        _ = dictionary.Remove("a");

        Assert.AreEqual(0, evictions);
        Assert.AreEqual(0, dictionary.EvictionCount);
    }
}
