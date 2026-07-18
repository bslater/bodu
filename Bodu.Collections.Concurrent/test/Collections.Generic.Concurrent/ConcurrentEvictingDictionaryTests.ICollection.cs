// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.ICollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="ICollection{T}.IsReadOnly" /> reports <see langword="false" /> for the key/value pair
    /// collection surface.
    /// </summary>
    [TestMethod]
    public void ICollectionKvp_IsReadOnly_ShouldBeFalse()
    {
        ICollection<KeyValuePair<string, int>> collection = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        Assert.IsFalse(collection.IsReadOnly);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Add(T)" /> adds a new key/value pair when the key is absent.
    /// </summary>
    [TestMethod]
    public void ICollectionKvpAdd_WhenKeyMissing_ShouldAddEntry()
    {
        ICollection<KeyValuePair<string, int>> collection = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        collection.Add(new KeyValuePair<string, int>("a", 1));

        Assert.AreEqual(1, collection.Count);
        Assert.Contains(new KeyValuePair<string, int>("a", 1), collection);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Add(T)" /> throws <see cref="ArgumentException" /> when a live entry for
    /// the key already exists, following the dictionary add-or-throw contract.
    /// </summary>
    [TestMethod]
    public void ICollectionKvpAdd_WhenKeyAlreadyExists_ShouldThrowArgumentException()
    {
        ICollection<KeyValuePair<string, int>> collection = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        collection.Add(new KeyValuePair<string, int>("a", 1));

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.Add(new KeyValuePair<string, int>("a", 2));
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Contains(KeyValuePair{TKey, TValue})" />
    /// reports <see langword="true" /> only when both the key and value match.
    /// </summary>
    [TestMethod]
    public void ContainsKvp_WhenKeyAndValueMatch_ShouldReturnTrue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        Assert.IsTrue(dictionary.Contains(new KeyValuePair<string, int>("a", 1)));
        Assert.IsFalse(dictionary.Contains(new KeyValuePair<string, int>("a", 999)), "A value mismatch must report absent.");
        Assert.IsFalse(dictionary.Contains(new KeyValuePair<string, int>("z", 1)), "A missing key must report absent.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Contains(KeyValuePair{TKey, TValue})" /> is a
    /// pure read that does not count as a policy access.
    /// </summary>
    [TestMethod]
    public void ContainsKvp_WhenCalled_ShouldNotCountAsPolicyAccess()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        _ = dictionary.Contains(new KeyValuePair<string, int>("a", 1));
        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("a"), "Contains(KVP) must not refresh recency, so 'a' remains the LRU victim.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Remove(KeyValuePair{TKey, TValue})" /> removes
    /// the entry only when both the key and value match.
    /// </summary>
    [TestMethod]
    public void RemoveKvp_WhenKeyAndValueMatch_ShouldRemoveAndReturnTrue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        bool removed = dictionary.Remove(new KeyValuePair<string, int>("a", 1));

        Assert.IsTrue(removed);
        Assert.IsFalse(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Remove(KeyValuePair{TKey, TValue})" /> leaves
    /// the entry in place when the value does not match.
    /// </summary>
    [TestMethod]
    public void RemoveKvp_WhenValueDoesNotMatch_ShouldReturnFalseAndKeepEntry()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        bool removed = dictionary.Remove(new KeyValuePair<string, int>("a", 999));

        Assert.IsFalse(removed);
        Assert.IsTrue(dictionary.ContainsKey("a"));
        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.CopyTo(KeyValuePair{TKey, TValue}[], int)" />
    /// writes the live entries into the destination array from the requested index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArrayFits_ShouldCopyEntriesFromIndex()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        var target = new KeyValuePair<string, int>[4];

        dictionary.CopyTo(target, 1);

        KeyValuePair<string, int>[] copied = target.Skip(1).Take(2).OrderBy(p => p.Key, StringComparer.Ordinal).ToArray();
        Assert.AreEqual(new KeyValuePair<string, int>("a", 1), copied[0]);
        Assert.AreEqual(new KeyValuePair<string, int>("b", 2), copied[1]);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.CopyTo(KeyValuePair{TKey, TValue}[], int)" />
    /// validates its destination array and index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenArgumentsInvalid_ShouldThrow()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary.CopyTo(null!, 0);
        });

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            dictionary.CopyTo(new KeyValuePair<string, int>[2], -1);
        });

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.CopyTo(new KeyValuePair<string, int>[1], 1);
        });
    }
}
