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
    /// Verifies that the default-capacity sequence constructor copies the source entry.
    /// </summary>
    /// <remarks>
    /// A single entry is used deliberately: at the default capacity the per-segment slice can be one slot, so two
    /// string keys routed to the same segment by the process's randomized string hashing would evict one another. The
    /// multi-entry copy path is covered below with a collision-safe explicit capacity.
    /// </remarks>
    [TestMethod]
    public void Ctor_WhenSourceSupplied_ShouldCopyEntries()
    {
        KeyValuePair<string, int>[] source = [new("a", 1)];

        var dictionary = new ConcurrentEvictingDictionary<string, int>(source);

        AssertContainsExactly(dictionary, ("a", 1));
    }

    /// <summary>
    /// Verifies that the capacity-taking sequence constructor copies every entry from the source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceSuppliedWithCapacity_ShouldCopyEveryEntry()
    {
        KeyValuePair<string, int>[] source =
        [
            new("a", 1),
            new("b", 2),
            new("c", 3),
        ];

        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, source);

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
    /// Verifies that a source sequence larger than the capacity leaves exactly <c>Capacity</c> entries stored.
    /// </summary>
    /// <remarks>
    /// Sequential <see cref="int" /> keys are used deliberately: their hash codes cover every segment for any segment
    /// count the default concurrency level can produce at this capacity, so each segment fills its slice exactly.
    /// String keys would make the segment routing depend on the process's randomized string hashing.
    /// </remarks>
    [TestMethod]
    public void Ctor_WhenSourceExceedsCapacity_ShouldEvictDownToCapacity()
    {
        KeyValuePair<int, int>[] source = Enumerable.Range(0, 10)
            .Select(i => new KeyValuePair<int, int>(i, i))
            .ToArray();

        var dictionary = new ConcurrentEvictingDictionary<int, int>(capacity: 4, source);

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

    /// <summary>
    /// Verifies that the capacity, policy and comparer constructor stores each of the three settings and leaves
    /// expiration disabled.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityPolicyAndComparerSupplied_ShouldStoreEachSetting()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 4, EvictingDictionaryPolicy.FirstInFirstOut, StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(4, dictionary.Capacity);
        Assert.AreEqual(EvictingDictionaryPolicy.FirstInFirstOut, dictionary.Policy);
        Assert.AreSame(StringComparer.OrdinalIgnoreCase, dictionary.Comparer);
        Assert.IsNull(dictionary.Expiration);
    }

    /// <summary>
    /// Verifies that the capacity, policy and expiration constructor stores each of the three settings and uses the
    /// default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityPolicyAndExpirationSupplied_ShouldStoreEachSetting()
    {
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(1));

        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 4, EvictingDictionaryPolicy.MostRecentlyUsed, expiration);

        Assert.AreEqual(4, dictionary.Capacity);
        Assert.AreEqual(EvictingDictionaryPolicy.MostRecentlyUsed, dictionary.Policy);
        Assert.AreSame(expiration, dictionary.Expiration);
        Assert.AreSame(EqualityComparer<string>.Default, dictionary.Comparer);
    }

    /// <summary>
    /// Verifies that the capacity, policy, comparer and expiration constructor stores each of the four settings.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityPolicyComparerAndExpirationSupplied_ShouldStoreEachSetting()
    {
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(1));

        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 4, EvictingDictionaryPolicy.LeastFrequentlyUsed, StringComparer.Ordinal, expiration);

        Assert.AreEqual(4, dictionary.Capacity);
        Assert.AreEqual(EvictingDictionaryPolicy.LeastFrequentlyUsed, dictionary.Policy);
        Assert.AreSame(StringComparer.Ordinal, dictionary.Comparer);
        Assert.AreSame(expiration, dictionary.Expiration);
    }

    /// <summary>
    /// Verifies that the source and policy constructor copies every entry, applies the default capacity and stores the
    /// policy.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceAndPolicySupplied_ShouldCopyEntriesAndStorePolicy()
    {
        KeyValuePair<string, int>[] source = [new("a", 1)];

        var dictionary = new ConcurrentEvictingDictionary<string, int>(source, EvictingDictionaryPolicy.SecondChance);

        Assert.AreEqual(16, dictionary.Capacity);
        Assert.AreEqual(EvictingDictionaryPolicy.SecondChance, dictionary.Policy);
        Assert.IsNull(dictionary.Expiration);
        AssertContainsExactly(dictionary, ("a", 1));
    }

    /// <summary>
    /// Verifies that the capacity, source and policy constructor copies every entry and stores the capacity and
    /// policy.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacitySourceAndPolicySupplied_ShouldCopyEntriesAndStoreEachSetting()
    {
        // The capacity is split across the segments, so it is kept generous enough that two keys landing in the
        // same segment cannot evict each other.
        KeyValuePair<string, int>[] source = [new("a", 1), new("b", 2)];

        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, source, EvictingDictionaryPolicy.MostRecentlyUsed);

        Assert.AreEqual(128, dictionary.Capacity);
        Assert.AreEqual(EvictingDictionaryPolicy.MostRecentlyUsed, dictionary.Policy);
        Assert.AreSame(EqualityComparer<string>.Default, dictionary.Comparer);
        AssertContainsExactly(dictionary, ("a", 1), ("b", 2));
    }

    /// <summary>
    /// Verifies that the capacity, source, policy and comparer constructor copies every entry through the supplied
    /// comparer and stores the capacity, policy and comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacitySourcePolicyAndComparerSupplied_ShouldCopyEntriesAndStoreEachSetting()
    {
        KeyValuePair<string, int>[] source = [new("a", 1), new("B", 2)];

        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128, source, EvictingDictionaryPolicy.FirstInFirstOut, StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(128, dictionary.Capacity);
        Assert.AreEqual(EvictingDictionaryPolicy.FirstInFirstOut, dictionary.Policy);
        Assert.AreSame(StringComparer.OrdinalIgnoreCase, dictionary.Comparer);
        Assert.IsNull(dictionary.Expiration);
        Assert.IsTrue(dictionary.ContainsKey("b"), "The supplied comparer must govern key lookups on the copied entries.");
        AssertContainsExactly(dictionary, ("a", 1), ("B", 2));
    }
}
