// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;

namespace Bodu.Smoke;

/// <summary>
/// Provides smoke-tier coverage for the primary public types in <c>Bodu.Core</c>. Each test exercises one
/// happy-path entry point so that catastrophic breakage (constructor regression, null reference, bad
/// dispatch) is caught by the smallest possible build run.
/// </summary>
[TestClass]
public sealed class SmokeTests
{

    /// <summary>
    /// Verifies that <see cref="CircularBuffer{T}.Enqueue" /> and <see cref="CircularBuffer{T}.Dequeue" /> round-trip a
    /// single value.
    /// </summary>
    [TestMethod]
    public void CircularBuffer_EnqueueDequeue_ShouldRoundTripValue()
    {
        CircularBuffer<int> buffer = new(capacity: 4);
        buffer.Enqueue(42);

        var actual = buffer.Dequeue();

        Assert.AreEqual(42, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Deque{T}.AddLast" /> followed by <see cref="Deque{T}.RemoveFirst" /> recovers the
    /// inserted value.
    /// </summary>
    [TestMethod]
    public void Deque_AddLastRemoveFirst_ShouldRecoverValue()
    {
        Deque<string> deque = new(capacity: 4);
        deque.AddLast("hello");

        var actual = deque.RemoveFirst();

        Assert.AreEqual("hello", actual);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}" /> evicts the least-recently-used entry once
    /// capacity is exceeded.
    /// </summary>
    [TestMethod]
    public void EvictingDictionary_AddBeyondCapacity_ShouldEvictOldestEntry()
    {
        EvictingDictionary<string, int> dictionary = new(capacity: 2);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}" /> stores and retrieves a key-value pair under
    /// the default LRU policy.
    /// </summary>
    [TestMethod]
    public void EvictingDictionary_AddIndexer_ShouldRoundTripValue()
    {
        EvictingDictionary<string, int> dictionary = new(capacity: 4);
        dictionary.Add("alpha", 1);

        var actual = dictionary["alpha"];

        Assert.AreEqual(1, actual);
    }

}
