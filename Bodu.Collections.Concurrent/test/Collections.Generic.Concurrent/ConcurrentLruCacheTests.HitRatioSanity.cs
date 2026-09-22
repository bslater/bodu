// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCacheTests.HitRatioSanity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentLruCacheTests
{
    /// <summary>
    /// Verifies scan resistance deterministically: a hot working set that is re-read every round survives ten rounds
    /// of one-pass scan traffic, every hot read after placement is a hit, and the aggregate hit ratio equals its
    /// exactly computable value.
    /// </summary>
    [TestMethod]
    public void HitRatio_WhenHotSetCompetesWithScans_ShouldRetainHotSetAndMatchExactRatio()
    {
        const int hotKeys = 20;
        const int scansPerRound = 1_000;
        const int rounds = 10;

        // Capacity 60 partitions as hot 20 / warm 20 / cold 20: the hot working set fits warm exactly.
        var cache = new ConcurrentLruCache<string, int>(capacity: 60);
        var evictedHotKeys = new List<string>();
        cache.ItemEvicted += (key, _) =>
        {
            if (key.StartsWith("hot", StringComparison.Ordinal))
                evictedHotKeys.Add(key);
        };

        // Placement: adds are telemetry-neutral, so the arithmetic below stays exact.
        for (int i = 0; i < hotKeys; i++)
            cache.Add($"hot{i}", i);

        for (int round = 0; round < rounds; round++)
        {
            for (int i = 0; i < hotKeys; i++)
                Assert.IsTrue(cache.TryGetValue($"hot{i}", out _), $"Hot key hot{i} was lost in round {round}.");

            for (int i = 0; i < scansPerRound; i++)
            {
                string scanKey = $"scan{round}-{i}";
                Assert.IsFalse(cache.TryGetValue(scanKey, out _), "Every scan key is unique and must miss.");
                cache.Add(scanKey, i);
            }

            cache.RunMaintenance();
        }

        const long expectedHits = (long)hotKeys * rounds;
        const long expectedMisses = (long)scansPerRound * rounds;

        Assert.AreEqual(expectedHits, cache.HitCount);
        Assert.AreEqual(expectedMisses, cache.MissCount);
        Assert.AreEqual((double)expectedHits / (expectedHits + expectedMisses), cache.HitRatio, 1e-12);
        Assert.IsEmpty(evictedHotKeys, "The re-read hot working set must never be evicted by scan traffic.");

        for (int i = 0; i < hotKeys; i++)
            Assert.IsTrue(cache.ContainsKey($"hot{i}"), $"Hot key hot{i} must still be resident after the final round.");
    }

    /// <summary>
    /// Verifies that the pseudo-LRU beats pure flow-through on a skewed workload: with a re-read working set the
    /// resident set converges on the hot keys rather than the most recent scan keys.
    /// </summary>
    [TestMethod]
    public void HitRatio_WhenWorkloadIsSkewed_ShouldConvergeResidencyOnHotKeys()
    {
        var cache = new ConcurrentLruCache<string, int>(capacity: 9);

        for (int i = 0; i < 3; i++)
            cache.Add($"hot{i}", i);

        for (int round = 0; round < 5; round++)
        {
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(cache.TryGetValue($"hot{i}", out _));

            for (int i = 0; i < 50; i++)
                cache.Add($"scan{round}-{i}", i);

            cache.RunMaintenance();
        }

        int residentHotKeys = Enumerable.Range(0, 3).Count(i => cache.ContainsKey($"hot{i}"));
        Assert.AreEqual(3, residentHotKeys, "All three re-read keys must remain resident despite 250 scan insertions.");
    }
}
