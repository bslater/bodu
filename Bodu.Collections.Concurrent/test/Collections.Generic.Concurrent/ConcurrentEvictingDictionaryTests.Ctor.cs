// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that the parameterless constructor creates an empty dictionary with the default capacity, the
    /// LeastRecentlyUsed policy, no expiration, and the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParameterless_ShouldUseDefaults()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();

        Assert.AreEqual(16, dictionary.Capacity);
        Assert.AreEqual(EvictingDictionaryPolicy.LeastRecentlyUsed, dictionary.Policy);
        Assert.IsNull(dictionary.Expiration);
        Assert.AreSame(EqualityComparer<string>.Default, dictionary.Comparer);
        Assert.AreEqual(0, dictionary.Count);
        Assert.IsTrue(dictionary.IsEmpty);
    }

    /// <summary>
    /// Verifies that the capacity constructor stores the requested capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacitySupplied_ShouldStoreCapacity()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 7);

        Assert.AreEqual(7, dictionary.Capacity);
    }

    /// <summary>
    /// Verifies that a zero capacity throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentEvictingDictionary<string, int>(capacity: 0);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a negative capacity throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentEvictingDictionary<string, int>(capacity: -1);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined eviction policy value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPolicyIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentEvictingDictionary<string, int>(capacity: 4, (EvictingDictionaryPolicy)999);
        });
    }

    /// <summary>
    /// Verifies that the policy constructor stores the requested policy.
    /// </summary>
    [TestMethod]
    [DataRow(EvictingDictionaryPolicy.FirstInFirstOut)]
    [DataRow(EvictingDictionaryPolicy.LeastRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.LeastFrequentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.MostRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.RandomReplacement)]
    [DataRow(EvictingDictionaryPolicy.SecondChance)]
    public void Ctor_WhenPolicySupplied_ShouldStorePolicy(EvictingDictionaryPolicy policy)
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 4, policy);

        Assert.AreEqual(policy, dictionary.Policy);
    }

    /// <summary>
    /// Verifies that a supplied key comparer is used for key identity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerSupplied_ShouldUseComparerForKeyIdentity()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 4, StringComparer.OrdinalIgnoreCase);

        dictionary.Add("Key", 1);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, dictionary.Comparer);
        Assert.IsTrue(dictionary.ContainsKey("KEY"));
    }

    /// <summary>
    /// Verifies that a supplied expiration configuration is exposed through the
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Expiration" /> property.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenExpirationSupplied_ShouldStoreExpiration()
    {
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(5));

        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 4, expiration);

        Assert.AreSame(expiration, dictionary.Expiration);
    }

    /// <summary>
    /// Verifies that the sequence constructor copies every entry from the source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceSupplied_ShouldCopyEntries()
    {
        KeyValuePair<string, int>[] source =
        [
            new("a", 1),
            new("b", 2),
            new("c", 3),
        ];

        var dictionary = new ConcurrentEvictingDictionary<string, int>(source);

        AssertContainsExactly(dictionary, ("a", 1), ("b", 2), ("c", 3));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> source sequence throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConcurrentEvictingDictionary<string, int>((IEnumerable<KeyValuePair<string, int>>)null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a source sequence larger than the capacity leaves at most <c>Capacity</c> entries stored.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceExceedsCapacity_ShouldEvictDownToCapacity()
    {
        KeyValuePair<string, int>[] source = Enumerable.Range(0, 10)
            .Select(i => new KeyValuePair<string, int>($"k{i}", i))
            .ToArray();

        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 4, source);

        Assert.AreEqual(4, dictionary.Count);
        Assert.AreEqual(6, dictionary.EvictionCount);
    }

    /// <summary>
    /// Verifies that a duplicate key in the source sequence replaces the earlier value rather than throwing.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceContainsDuplicateKeys_ShouldKeepLastValue()
    {
        KeyValuePair<string, int>[] source =
        [
            new("a", 1),
            new("a", 2),
        ];

        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 4, source);

        Assert.AreEqual(1, dictionary.Count);
        Assert.IsTrue(dictionary.TryGetValue("a", out int value));
        Assert.AreEqual(2, value);
    }
}
