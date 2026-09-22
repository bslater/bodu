// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.NonGenericInterfaces.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that <see cref="ICollection.IsSynchronized" /> reports <see langword="false" />: the cache manages its
    /// own synchronization.
    /// </summary>
    [TestMethod]
    public void ICollection_IsSynchronized_ShouldBeFalse()
    {
        ICollection collection = new ConcurrentLruCache<string, int>();

        Assert.IsFalse(collection.IsSynchronized);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.SyncRoot" /> throws <see cref="NotSupportedException" />, matching the BCL
    /// concurrent collections.
    /// </summary>
    [TestMethod]
    public void ICollection_SyncRoot_ShouldThrowNotSupportedException()
    {
        ICollection collection = new ConcurrentLruCache<string, int>();

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = collection.SyncRoot;
        });
    }

    /// <summary>
    /// Verifies that the non-generic copy writes the entries into the destination array from the requested index.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayFits_ShouldCopyEntries()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);
        cache.Add("b", 2);
        ICollection collection = cache;
        var target = new KeyValuePair<string, int>[4];

        collection.CopyTo(target, 1);

        KeyValuePair<string, int>[] copied = target.Skip(1).Take(2).OrderBy(p => p.Key, StringComparer.Ordinal).ToArray();
        Assert.AreEqual(new KeyValuePair<string, int>("a", 1), copied[0]);
        Assert.AreEqual(new KeyValuePair<string, int>("b", 2), copied[1]);
    }

    /// <summary>
    /// Verifies that the non-generic copy validates its destination array and index.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArgumentsInvalid_ShouldThrow()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);
        ICollection collection = cache;

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            collection.CopyTo(null!, 0);
        });

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            collection.CopyTo(new KeyValuePair<string, int>[2], -1);
        });

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new KeyValuePair<string, int>[1], 1);
        });

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new string[4], 0);
        });
    }

    /// <summary>
    /// Verifies that the cache answers reads through <see cref="IReadOnlyDictionary{TKey, TValue}" />.
    /// </summary>
    [TestMethod]
    public void IReadOnlyDictionary_WhenAccessed_ShouldAnswerReads()
    {
        IReadOnlyDictionary<string, int> cache = new ConcurrentLruCache<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
        };

        Assert.AreEqual(2, cache.Count);
        Assert.IsTrue(cache.ContainsKey("a"));
        Assert.IsTrue(cache.TryGetValue("b", out int value));
        Assert.AreEqual(2, value);
        Assert.AreEqual(1, cache["a"]);
        Assert.HasCount(2, cache.Keys.ToArray());
        Assert.HasCount(2, cache.Values.ToArray());
    }
}
