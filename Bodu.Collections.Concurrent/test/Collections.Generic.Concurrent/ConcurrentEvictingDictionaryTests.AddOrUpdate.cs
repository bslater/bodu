// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.AddOrUpdate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that the value overload adds the supplied value and returns it when the key is absent.
    /// </summary>
    [TestMethod]
    public void AddOrUpdate_WhenKeyMissing_ForValue_ShouldAddAndReturnAddValue()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        int result = dictionary.AddOrUpdate("a", addValue: 1, (_, existing) => existing + 10);

        Assert.AreEqual(1, result);
        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that the value overload applies the update factory to the existing value and returns the result when
    /// the key is present.
    /// </summary>
    [TestMethod]
    public void AddOrUpdate_WhenKeyExists_ForValue_ShouldApplyUpdateFactory()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 5);

        int result = dictionary.AddOrUpdate("a", addValue: 1, (_, existing) => existing + 10);

        Assert.AreEqual(15, result);
        Assert.AreEqual(15, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that the factory overload invokes the add-value factory exactly once and stores its result when the
    /// key is absent.
    /// </summary>
    [TestMethod]
    public void AddOrUpdate_WhenKeyMissing_ForFactory_ShouldInvokeAddFactoryOnce()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        int addInvocations = 0;
        int updateInvocations = 0;

        int result = dictionary.AddOrUpdate(
            "a",
            key =>
            {
                addInvocations++;
                return key.Length;
            },
            (_, existing) =>
            {
                updateInvocations++;
                return existing + 10;
            });

        Assert.AreEqual(1, result);
        Assert.AreEqual(1, addInvocations);
        Assert.AreEqual(0, updateInvocations, "The update factory must not run on the add path.");
        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that the factory overload invokes the update factory exactly once with the existing value when the key
    /// is present.
    /// </summary>
    [TestMethod]
    public void AddOrUpdate_WhenKeyExists_ForFactory_ShouldInvokeUpdateFactoryOnce()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 5);
        int addInvocations = 0;
        int updateInvocations = 0;

        int result = dictionary.AddOrUpdate(
            "a",
            _ =>
            {
                addInvocations++;
                return 99;
            },
            (_, existing) =>
            {
                updateInvocations++;
                return existing + 10;
            });

        Assert.AreEqual(15, result);
        Assert.AreEqual(0, addInvocations, "The add factory must not run on the update path.");
        Assert.AreEqual(1, updateInvocations);
        Assert.AreEqual(15, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key or factory throws <see cref="ArgumentNullException" /> with the
    /// offending parameter name.
    /// </summary>
    [TestMethod]
    public void AddOrUpdate_WhenArgumentIsNull_ShouldThrowArgumentNullException()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);

        var keyEx = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.AddOrUpdate(null!, 1, (_, v) => v);
        });

        var updateEx = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.AddOrUpdate("a", 1, (Func<string, int, int>)null!);
        });

        var addFactoryEx = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.AddOrUpdate("a", (Func<string, int>)null!, (_, v) => v);
        });

        Assert.AreEqual("key", keyEx.ParamName);
        Assert.AreEqual("updateValueFactory", updateEx.ParamName);
        Assert.AreEqual("addValueFactory", addFactoryEx.ParamName);
    }

    /// <summary>
    /// Verifies that a throwing update factory propagates its exception and leaves the existing entry unchanged.
    /// </summary>
    [TestMethod]
    public void AddOrUpdate_WhenUpdateFactoryThrows_ShouldPropagateAndKeepEntry()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 128);
        dictionary.Add("a", 5);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = dictionary.AddOrUpdate("a", 1, (_, _) => throw new InvalidOperationException("update failure"));
        });

        Assert.IsTrue(ex.Message.Contains("update failure", StringComparison.Ordinal));
        Assert.AreEqual(5, dictionary["a"], "The failed update must leave the existing value intact.");
    }

    /// <summary>
    /// Verifies that the add path evicts per the policy when the owning segment is full.
    /// </summary>
    [TestMethod]
    public void AddOrUpdate_WhenFull_ShouldEvictPerPolicy()
    {
        ConcurrentEvictingDictionary<string, int> dictionary = CreateSingleSegment(capacity: 2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        int result = dictionary.AddOrUpdate("c", addValue: 3, (_, existing) => existing);

        Assert.AreEqual(3, result);
        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("a"), "The FIFO victim 'a' should have been evicted by the AddOrUpdate add path.");
    }

    /// <summary>
    /// Verifies that many threads racing an increment-style <see cref="ConcurrentEvictingDictionary{TKey, TValue}.AddOrUpdate(TKey, System.Func{TKey, TValue}, System.Func{TKey, TValue, TValue})" />
    /// on the same key converge to a total equal to the number of callers.
    /// </summary>
    [TestMethod]
    public void AddOrUpdate_WhenThreadsRaceOnSameKey_ShouldConvergeToCallerCount()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>(capacity: 8);
        const int callers = 256;

        Parallel.For(0, callers, _ =>
        {
            _ = dictionary.AddOrUpdate("shared", addValue: 1, (_, existing) => existing + 1);
        });

        Assert.IsTrue(dictionary.TryGetValue("shared", out int total));
        Assert.AreEqual(callers, total, "Atomic AddOrUpdate under contention must not lose any increment.");
    }
}
