// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies that the struct enumerator walks every snapshot entry and then reports completion.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenAdvanced_ShouldYieldEveryEntryThenComplete()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);
        cache.Add("b", 2);

        ConcurrentLruCache<string, int>.Enumerator enumerator = cache.GetEnumerator();
        var seen = new List<string>();
        while (enumerator.MoveNext())
            seen.Add(enumerator.Current.Key);

        Assert.HasCount(2, seen);
        Assert.IsFalse(enumerator.MoveNext(), "MoveNext must keep returning false after completion.");
    }

    /// <summary>
    /// Verifies that a default-constructed enumerator behaves as an empty sequence instead of throwing.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDefaultValued_ShouldBehaveAsEmptySequence()
    {
        ConcurrentLruCache<string, int>.Enumerator enumerator = default;

        Assert.IsFalse(enumerator.MoveNext());
        Assert.AreEqual(default, enumerator.Current);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentLruCache{TKey, TValue}.Enumerator.Reset" /> rewinds the enumerator to before
    /// the first snapshot entry.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenReset_ShouldRewindToStart()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        ConcurrentLruCache<string, int>.Enumerator enumerator = cache.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());

        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("a", enumerator.Current.Key);
    }

    /// <summary>
    /// Verifies that disposing the enumerator is a no-op and leaves it usable.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDisposed_ShouldBeNoOp()
    {
        var cache = new ConcurrentLruCache<string, int>();
        cache.Add("a", 1);

        ConcurrentLruCache<string, int>.Enumerator enumerator = cache.GetEnumerator();
        enumerator.Dispose();

        Assert.IsTrue(enumerator.MoveNext());
    }
}
