// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that the parameterless constructor creates an empty cache with the default capacity and comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParameterless_ShouldUseDefaults()
    {
        var cache = new ConcurrentLruCache<string, int>();

        Assert.AreEqual(16, cache.Capacity);
        Assert.AreSame(EqualityComparer<string>.Default, cache.Comparer);
        Assert.AreEqual(0, cache.Count);
        Assert.IsTrue(cache.IsEmpty);
        Assert.AreEqual(0, cache.HitCount);
        Assert.AreEqual(0, cache.MissCount);
        Assert.AreEqual(0.0, cache.HitRatio);
    }

    /// <summary>
    /// Verifies that the capacity constructor stores the requested capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacitySupplied_ShouldStoreCapacity()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 9);

        Assert.AreEqual(9, cache.Capacity);
    }

    /// <summary>
    /// Verifies that a zero or negative capacity throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsZeroOrNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var zeroEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentLruCache<string, int>(capacity: 0);
        });

        var negativeEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentLruCache<string, int>(capacity: -1);
        });

        Assert.AreEqual("capacity", zeroEx.ParamName);
        Assert.AreEqual("capacity", negativeEx.ParamName);
    }

    /// <summary>
    /// Verifies that a supplied key comparer is used for key identity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerSupplied_ShouldUseComparerForKeyIdentity()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 8, StringComparer.OrdinalIgnoreCase);

        cache.Add("Key", 1);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, cache.Comparer);
        Assert.IsTrue(cache.ContainsKey("KEY"));
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

        var cache = new ConcurrentLruCache<string, int>(capacity: 8, source);

        AssertContainsExactly(cache, ("a", 1), ("b", 2), ("c", 3));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> source sequence throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConcurrentLruCache<string, int>(capacity: 8, (IEnumerable<KeyValuePair<string, int>>)null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that seeding from a sequence larger than the capacity evicts flow-through entries so the count never
    /// exceeds the capacity.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceExceedsCapacity_ShouldEvictDownToAtMostCapacity()
    {
        KeyValuePair<string, int>[] source = Enumerable.Range(0, 50)
            .Select(i => new KeyValuePair<string, int>($"k{i}", i))
            .ToArray();

        var cache = new ConcurrentLruCache<string, int>(capacity: 6, source);
        cache.RunMaintenance();

        Assert.IsTrue(cache.Count <= 6, $"Count {cache.Count} exceeded the capacity after seeding.");
        Assert.IsTrue(cache.EvictionCount >= 44, "Most of the oversized seed must have been evicted.");
    }
}
