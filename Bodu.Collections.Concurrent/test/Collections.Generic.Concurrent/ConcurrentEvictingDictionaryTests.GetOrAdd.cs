// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.GetOrAdd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that a hit returns the existing value without replacing it.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenKeyExists_ShouldReturnExistingValue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);

        int result = dictionary.GetOrAdd("a", 99);

        Assert.AreEqual(1, result);
        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that a miss adds the supplied value and returns it.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenKeyMissing_ShouldAddAndReturnValue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        int result = dictionary.GetOrAdd("a", 42);

        Assert.AreEqual(42, result);
        Assert.AreEqual(42, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that a miss invokes the value factory exactly once and stores its result.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenKeyMissing_ForFactory_ShouldInvokeFactoryOnceAndStoreResult()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        int invocations = 0;

        int result = dictionary.GetOrAdd("a", key =>
        {
            invocations++;
            return key.Length;
        });

        Assert.AreEqual(1, result);
        Assert.AreEqual(1, invocations);
        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that a hit does not invoke the value factory.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenKeyExists_ForFactory_ShouldNotInvokeFactory()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 1);
        int invocations = 0;

        int result = dictionary.GetOrAdd("a", _ =>
        {
            invocations++;
            return 99;
        });

        Assert.AreEqual(1, result);
        Assert.AreEqual(0, invocations);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key or factory throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenArgumentIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        var keyEx = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.GetOrAdd(null!, 1);
        });

        var factoryEx = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.GetOrAdd("a", (Func<string, int>)null!);
        });

        Assert.AreEqual("key", keyEx.ParamName);
        Assert.AreEqual("valueFactory", factoryEx.ParamName);
    }

    /// <summary>
    /// Verifies that a throwing value factory propagates its exception and leaves the dictionary unchanged.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenFactoryThrows_ShouldPropagateAndAddNothing()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = dictionary.GetOrAdd("a", _ => throw new InvalidOperationException("factory failure"));
        });

        Assert.IsTrue(ex.Message.Contains("factory failure", StringComparison.Ordinal));
        Assert.AreEqual(0, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that a hit counts as a policy access, protecting the read key from eviction like
    /// <see cref="ConcurrentEvictingDictionary{TKey, TValue}.TryGetValue" />.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenHitForLeastRecentlyUsed_ShouldRefreshRecency()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        _ = dictionary.GetOrAdd("a", 99);

        dictionary.Add("c", 3);

        Assert.IsFalse(dictionary.ContainsKey("b"), "The GetOrAdd hit on 'a' should have made 'b' the eviction candidate.");
    }

    /// <summary>
    /// Verifies that adding through <see cref="ConcurrentEvictingDictionary{TKey, TValue}.GetOrAdd(TKey, TValue)" />
    /// on a full dictionary evicts per the policy.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenFull_ShouldEvictPerPolicy()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        int result = dictionary.GetOrAdd("c", 3);

        Assert.AreEqual(3, result);
        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that when many threads race a factory-based miss on the same key, the factory runs exactly once and
    /// every caller observes the same value.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenThreadsRaceOnSameKey_ShouldInvokeFactoryExactlyOnce()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 8);
        int invocations = 0;
        int[] results = new int[8];

        Parallel.For(0, results.Length, i =>
        {
            results[i] = dictionary.GetOrAdd("shared", _ =>
            {
                return Interlocked.Increment(ref invocations);
            });
        });

        Assert.AreEqual(1, invocations, "The single-flight guarantee requires exactly one factory invocation.");
        foreach (int result in results)
            Assert.AreEqual(1, result, "Every racing caller must observe the single created value.");
    }
}
