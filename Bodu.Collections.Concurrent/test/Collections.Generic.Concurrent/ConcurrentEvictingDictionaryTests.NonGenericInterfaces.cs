// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.NonGenericInterfaces.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="ICollection.IsSynchronized" /> reports <see langword="false" />: the dictionary manages
    /// its own synchronization.
    /// </summary>
    [TestMethod]
    public void ICollection_IsSynchronized_ShouldBeFalse()
    {
        ICollection collection = new ConcurrentEvictingDictionary<string, int>();

        Assert.IsFalse(collection.IsSynchronized);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.SyncRoot" /> throws <see cref="NotSupportedException" />, matching the BCL
    /// concurrent collections.
    /// </summary>
    [TestMethod]
    public void ICollection_SyncRoot_ShouldThrowNotSupportedException()
    {
        ICollection collection = new ConcurrentEvictingDictionary<string, int>();

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = collection.SyncRoot;
        });
    }

    /// <summary>
    /// Verifies that the non-generic copy writes the live entries into the destination array from the requested index.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayFits_ShouldCopyEntries()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        ICollection collection = dictionary;
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
        var dictionary = new ConcurrentEvictingDictionary<string, int>();
        dictionary.Add("a", 1);
        ICollection collection = dictionary;

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
    }

    /// <summary>
    /// Verifies that copying into an array of an incompatible element type throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ICollection_CopyTo_WhenArrayTypeIncompatible_ShouldThrowArgumentException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();
        dictionary.Add("a", 1);
        ICollection collection = dictionary;

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            collection.CopyTo(new string[4], 0);
        });
    }

    /// <summary>
    /// Verifies that the dictionary implements <see cref="IReadOnlyDictionary{TKey, TValue}" /> and answers reads
    /// through that interface.
    /// </summary>
    [TestMethod]
    public void IReadOnlyDictionary_WhenAccessed_ShouldAnswerReads()
    {
        IReadOnlyDictionary<string, int> dictionary = new ConcurrentEvictingDictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
        };

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsTrue(dictionary.ContainsKey("a"));
        Assert.IsTrue(dictionary.TryGetValue("b", out int value));
        Assert.AreEqual(2, value);
        Assert.AreEqual(1, dictionary["a"]);
        Assert.HasCount(2, dictionary.Keys.ToArray());
        Assert.HasCount(2, dictionary.Values.ToArray());
    }
}
