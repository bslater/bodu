// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.GetOrAdd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that a hit returns the existing value without replacing it.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenKeyExists_ShouldReturnExistingValue()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        int result = cache.GetOrAdd("a", 99);

        Assert.AreEqual(1, result);
        Assert.IsTrue(cache.TryGetValue("a", out int stored));
        Assert.AreEqual(1, stored);
    }

    /// <summary>
    /// Verifies that a miss adds the supplied value and returns it.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenKeyMissing_ShouldAddAndReturnValue()
    {
        var cache = new ConcurrentLruCache<string, int>();

        int result = cache.GetOrAdd("a", 42);

        Assert.AreEqual(42, result);
        Assert.AreEqual(1, cache.Count);
    }

    /// <summary>
    /// Verifies that a miss invokes the value factory and stores its result, and a hit does not invoke it.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenFactorySupplied_ShouldInvokeOnlyOnMiss()
    {
        var cache = new ConcurrentLruCache<string, int>();
        int invocations = 0;

        int added = cache.GetOrAdd("a", key =>
        {
            invocations++;
            return key.Length;
        });
        int found = cache.GetOrAdd("a", _ =>
        {
            invocations++;
            return 99;
        });

        Assert.AreEqual(1, added);
        Assert.AreEqual(1, found);
        Assert.AreEqual(1, invocations);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key or factory throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenArgumentIsNull_ShouldThrowArgumentNullException()
    {
        var cache = new ConcurrentLruCache<string, int>();

        var keyEx = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cache.GetOrAdd(null!, 1);
        });

        var factoryEx = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cache.GetOrAdd("a", (Func<string, int>)null!);
        });

        Assert.AreEqual("key", keyEx.ParamName);
        Assert.AreEqual("valueFactory", factoryEx.ParamName);
    }

    /// <summary>
    /// Verifies that a throwing value factory propagates its exception and leaves the cache unchanged.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenFactoryThrows_ShouldPropagateAndAddNothing()
    {
        var cache = new ConcurrentLruCache<string, int>();

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = cache.GetOrAdd("a", _ => throw new InvalidOperationException("factory failure"));
        });

        Assert.IsTrue(ex.Message.Contains("factory failure", StringComparison.Ordinal));
        Assert.AreEqual(0, cache.Count);
    }

    /// <summary>
    /// Verifies that when callers race a factory miss on the same key, exactly one produced value is stored and every
    /// caller observes that stored value — even though the factory itself may run more than once.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenThreadsRaceOnSameKey_ShouldReturnSingleStoredValueToEveryCaller()
    {
        var cache = new ConcurrentLruCache<string, Guid>(capacity: 8);
        var results = new Guid[8];

        Parallel.For(0, results.Length, i =>
        {
            results[i] = cache.GetOrAdd("shared", _ => Guid.NewGuid());
        });

        Assert.IsTrue(cache.TryGetValue("shared", out Guid stored));
        foreach (Guid result in results)
            Assert.AreEqual(stored, result, "Every racing caller must observe the single stored value.");
    }

    /// <summary>
    /// Verifies that a hit counts toward <see cref="ConcurrentLruCache{TKey, TValue}.HitCount" /> and a successful add
    /// toward <see cref="ConcurrentLruCache{TKey, TValue}.MissCount" />.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenCalled_ShouldRecordHitsAndMisses()
    {
        var cache = new ConcurrentLruCache<string, int>();

        _ = cache.GetOrAdd("a", 1);   // miss + add
        _ = cache.GetOrAdd("a", 2);   // hit

        Assert.AreEqual(1, cache.MissCount);
        Assert.AreEqual(1, cache.HitCount);
    }
}
