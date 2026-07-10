// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.EvictionParity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that a single-segment dictionary reproduces the exact eviction sequence of the non-concurrent
    /// <see cref="EvictingDictionary{TKey, TValue}" /> oracle for every deterministic policy over a scripted workload
    /// of interleaved adds, reads, touches, replacements, and removals.
    /// </summary>
    [TestMethod]
    [DataRow(EvictingDictionaryPolicy.FirstInFirstOut)]
    [DataRow(EvictingDictionaryPolicy.LeastRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.LeastFrequentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.MostRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.SecondChance)]
    public void Eviction_WhenSingleSegment_ShouldMatchNonConcurrentOracleEvictionSequence(EvictingDictionaryPolicy policy)
    {
        const int capacity = 8;
        const int operations = 2_000;

        var oracle = new EvictingDictionary<int, int>(capacity, policy);
        var subject = new ConcurrentEvictingDictionary<int, int>(concurrencyLevel: 1, capacity, policy, null, null);

        var oracleEvictions = new List<int>();
        var subjectEvictions = new List<int>();
        oracle.ItemEvicted += (key, _) => oracleEvictions.Add(key);
        subject.ItemEvicted += (key, _) => subjectEvictions.Add(key);

        // A fixed seed keeps the workload identical across runs while still mixing operation kinds.
        var random = new Random(20260710);

        for (int i = 0; i < operations; i++)
        {
            int key = random.Next(24);
            int operation = random.Next(10);

            if (operation < 5)
            {
                oracle.Add(key, i);
                subject.Add(key, i);
            }
            else if (operation < 7)
            {
                bool oracleHit = oracle.TryGetValue(key, out int oracleValue);
                bool subjectHit = subject.TryGetValue(key, out int subjectValue);

                Assert.AreEqual(oracleHit, subjectHit, $"Lookup outcome diverged at operation {i} for key {key}.");
                Assert.AreEqual(oracleValue, subjectValue, $"Lookup value diverged at operation {i} for key {key}.");
            }
            else if (operation < 8)
            {
                Assert.AreEqual(oracle.Touch(key), subject.Touch(key), $"Touch outcome diverged at operation {i} for key {key}.");
            }
            else
            {
                bool oracleRemoved = oracle.Remove(key);
                bool subjectRemoved = subject.TryRemove(key, out _);

                Assert.AreEqual(oracleRemoved, subjectRemoved, $"Removal outcome diverged at operation {i} for key {key}.");
            }
        }

        CollectionAssert.AreEqual(oracleEvictions, subjectEvictions, "The eviction sequences must be identical.");
        Assert.AreEqual(oracle.Count, subject.Count, "The final counts must be identical.");

        foreach (KeyValuePair<int, int> pair in oracle.ToList())
        {
            Assert.IsTrue(subject.ContainsKey(pair.Key), $"Subject was missing key {pair.Key} present in the oracle.");
        }
    }

    /// <summary>
    /// Verifies that a single-segment RandomReplacement dictionary matches the oracle's count and capacity bound over
    /// a scripted workload; the random victim choice makes membership itself non-deterministic.
    /// </summary>
    [TestMethod]
    public void Eviction_WhenSingleSegmentForRandomReplacement_ShouldMatchOracleCountBound()
    {
        const int capacity = 8;

        var oracle = new EvictingDictionary<int, int>(capacity, EvictingDictionaryPolicy.RandomReplacement);
        var subject = new ConcurrentEvictingDictionary<int, int>(concurrencyLevel: 1, capacity, EvictingDictionaryPolicy.RandomReplacement, null, null);

        for (int i = 0; i < 500; i++)
        {
            oracle.Add(i, i);
            subject.Add(i, i);
        }

        Assert.AreEqual(oracle.Count, subject.Count);
        Assert.AreEqual(capacity, subject.Count);
        Assert.AreEqual(oracle.EvictionCount, subject.EvictionCount);
    }
}
