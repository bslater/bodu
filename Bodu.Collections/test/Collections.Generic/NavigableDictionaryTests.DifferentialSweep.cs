// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.DifferentialSweep.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>The exclusive upper bound of the key universe exercised by the differential sweep.</summary>
    private const int SweepUniverse = 512;

    /// <summary>The number of mixed operations applied by the differential sweep.</summary>
    private const int SweepOperations = 20_000;

    /// <summary>The number of operations between full-oracle checkpoints.</summary>
    private const int SweepCheckpointInterval = 500;

    /// <summary>
    /// Verifies that 20,000 seeded mixed add/remove/upsert/lookup operations mirrored against a
    /// <see cref="SortedDictionary{TKey, TValue}" /> leave the <see cref="NavigableDictionary{TKey, TValue}" /> in
    /// exactly the mirrored state at every checkpoint — full ordered content, count, and min/max entries — and that a
    /// floor/ceiling/higher/lower/rank/select/count-in-range probe battery agrees with a sorted-array binary-search
    /// oracle. This sweep is the correctness gate for the key-value adaptation of the size-augmented red-black tree.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void NavigableDictionary_WhenRandomOperationSweepApplied_ShouldMatchSortedDictionaryAndArrayOracle()
    {
        var random = new Random(20260705);
        var sut = new NavigableDictionary<int, int>();
        var mirror = new SortedDictionary<int, int>();

        for (int op = 1; op <= SweepOperations; op++)
        {
            int key = random.Next(SweepUniverse);
            int value = random.Next();
            switch (random.Next(6))
            {
                case 0:
                case 1:
                    // Bias toward adds so the tree grows through every insertion fixup path.
                    Assert.AreEqual(mirror.TryAdd(key, value), sut.TryAdd(key, value), $"TryAdd({key}) disagrees at op {op}.");
                    break;

                case 2:
                case 3:
                    Assert.AreEqual(mirror.Remove(key, out int mirrorRemoved), sut.Remove(key, out int sutRemoved), $"Remove({key}) disagrees at op {op}.");
                    Assert.AreEqual(mirrorRemoved, sutRemoved, $"Remove({key}) value disagrees at op {op}.");
                    break;

                case 4:
                    // Upsert through the indexer so value overwrites interleave with structural mutations.
                    mirror[key] = value;
                    sut[key] = value;
                    break;

                default:
                    Assert.AreEqual(mirror.ContainsKey(key), sut.ContainsKey(key), $"ContainsKey({key}) disagrees at op {op}.");
                    break;
            }

            if (op % SweepCheckpointInterval == 0)
                AssertMatchesOracles(sut, mirror, op);
        }

        // Drain the survivors through Remove to sweep the deletion fixup paths once more.
        foreach (KeyValuePair<int, int> entry in mirror.ToArray())
        {
            Assert.IsTrue(sut.Remove(entry.Key, out int removed));
            Assert.AreEqual(entry.Value, removed);
        }

        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Asserts that the dictionary agrees with the <see cref="SortedDictionary{TKey, TValue}" /> mirror on content,
    /// count, values, and extremes, and with a sorted-array binary-search oracle on every navigation, rank, select,
    /// and range-count probe.
    /// </summary>
    /// <param name="sut">The dictionary under test.</param>
    /// <param name="mirror">The mirrored <see cref="SortedDictionary{TKey, TValue}" />.</param>
    /// <param name="op">The operation ordinal, for failure labelling.</param>
    private static void AssertMatchesOracles(NavigableDictionary<int, int> sut, SortedDictionary<int, int> mirror, int op)
    {
        KeyValuePair<int, int>[] oracleEntries = mirror.ToArray();
        int[] oracle = oracleEntries.Select(e => e.Key).ToArray();

        Assert.AreEqual(oracleEntries.Length, sut.Count, $"Count disagrees at op {op}.");
        CollectionAssert.AreEqual(oracleEntries, sut.ToList(), $"Ordered content disagrees at op {op}.");

        if (oracle.Length > 0)
        {
            Assert.AreEqual(oracleEntries[0], sut.MinEntry, $"MinEntry disagrees at op {op}.");
            Assert.AreEqual(oracleEntries[^1], sut.MaxEntry, $"MaxEntry disagrees at op {op}.");
        }
        else
        {
            Assert.IsFalse(sut.TryGetMinEntry(out _), $"TryGetMinEntry should fail on empty at op {op}.");
        }

        // Select must return exactly the sorted-array entry at every rank.
        for (int rank = 0; rank < oracleEntries.Length; rank++)
            Assert.AreEqual(oracleEntries[rank], sut.GetAt(rank), $"GetAt({rank}) disagrees at op {op}.");

        // Probe every universe boundary region plus a sampled interior battery.
        for (int probe = -1; probe <= SweepUniverse; probe += 7)
        {
            AssertNavigationMatchesOracle(sut, oracleEntries, oracle, probe, op);
        }

        // Range counts across sampled inclusive windows.
        for (int low = -1; low <= SweepUniverse; low += 61)
        {
            for (int span = 0; span <= SweepUniverse; span += 97)
            {
                int expected = CountInRangeOracle(oracle, low, low + span);
                Assert.AreEqual(expected, sut.CountInRange(low, low + span), $"CountInRange({low}, {low + span}) disagrees at op {op}.");
            }
        }
    }

    /// <summary>
    /// Asserts the four navigation queries (entry and key variants) and the rank query for
    /// <paramref name="probe" /> against the sorted-array oracle via binary search.
    /// </summary>
    /// <param name="sut">The dictionary under test.</param>
    /// <param name="oracleEntries">The key-sorted oracle entries.</param>
    /// <param name="oracle">The sorted, distinct oracle key array.</param>
    /// <param name="probe">The probe key.</param>
    /// <param name="op">The operation ordinal, for failure labelling.</param>
    private static void AssertNavigationMatchesOracle(NavigableDictionary<int, int> sut, KeyValuePair<int, int>[] oracleEntries, int[] oracle, int probe, int op)
    {
        int position = Array.BinarySearch(oracle, probe);
        int insertion = position >= 0 ? position : ~position;

        // Floor: greatest key <= probe.
        int floorIndex = position >= 0 ? position : insertion - 1;
        Assert.AreEqual(floorIndex >= 0, sut.TryGetFloorEntry(probe, out KeyValuePair<int, int> floor), $"TryGetFloorEntry({probe}) presence disagrees at op {op}.");
        Assert.AreEqual(floorIndex >= 0, sut.TryGetFloorKey(probe, out int floorKey), $"TryGetFloorKey({probe}) presence disagrees at op {op}.");
        if (floorIndex >= 0)
        {
            Assert.AreEqual(oracleEntries[floorIndex], floor, $"TryGetFloorEntry({probe}) disagrees at op {op}.");
            Assert.AreEqual(oracle[floorIndex], floorKey, $"TryGetFloorKey({probe}) disagrees at op {op}.");
        }

        // Ceiling: least key >= probe.
        int ceilingIndex = position >= 0 ? position : insertion;
        Assert.AreEqual(ceilingIndex < oracle.Length, sut.TryGetCeilingEntry(probe, out KeyValuePair<int, int> ceiling), $"TryGetCeilingEntry({probe}) presence disagrees at op {op}.");
        Assert.AreEqual(ceilingIndex < oracle.Length, sut.TryGetCeilingKey(probe, out int ceilingKey), $"TryGetCeilingKey({probe}) presence disagrees at op {op}.");
        if (ceilingIndex < oracle.Length)
        {
            Assert.AreEqual(oracleEntries[ceilingIndex], ceiling, $"TryGetCeilingEntry({probe}) disagrees at op {op}.");
            Assert.AreEqual(oracle[ceilingIndex], ceilingKey, $"TryGetCeilingKey({probe}) disagrees at op {op}.");
        }

        // Higher: least key > probe.
        int higherIndex = position >= 0 ? position + 1 : insertion;
        Assert.AreEqual(higherIndex < oracle.Length, sut.TryGetHigherEntry(probe, out KeyValuePair<int, int> higher), $"TryGetHigherEntry({probe}) presence disagrees at op {op}.");
        Assert.AreEqual(higherIndex < oracle.Length, sut.TryGetHigherKey(probe, out int higherKey), $"TryGetHigherKey({probe}) presence disagrees at op {op}.");
        if (higherIndex < oracle.Length)
        {
            Assert.AreEqual(oracleEntries[higherIndex], higher, $"TryGetHigherEntry({probe}) disagrees at op {op}.");
            Assert.AreEqual(oracle[higherIndex], higherKey, $"TryGetHigherKey({probe}) disagrees at op {op}.");
        }

        // Lower: greatest key < probe.
        int lowerIndex = insertion - 1;
        Assert.AreEqual(lowerIndex >= 0, sut.TryGetLowerEntry(probe, out KeyValuePair<int, int> lower), $"TryGetLowerEntry({probe}) presence disagrees at op {op}.");
        Assert.AreEqual(lowerIndex >= 0, sut.TryGetLowerKey(probe, out int lowerKey), $"TryGetLowerKey({probe}) presence disagrees at op {op}.");
        if (lowerIndex >= 0)
        {
            Assert.AreEqual(oracleEntries[lowerIndex], lower, $"TryGetLowerEntry({probe}) disagrees at op {op}.");
            Assert.AreEqual(oracle[lowerIndex], lowerKey, $"TryGetLowerKey({probe}) disagrees at op {op}.");
        }

        // Rank: index in key-sorted order, -1 when absent.
        Assert.AreEqual(position >= 0 ? position : -1, sut.IndexOfKey(probe), $"IndexOfKey({probe}) disagrees at op {op}.");
    }

    /// <summary>
    /// Computes the inclusive-range count over the sorted oracle key array via binary search.
    /// </summary>
    /// <param name="oracle">The sorted, distinct oracle key array.</param>
    /// <param name="lowInclusive">The inclusive lower bound.</param>
    /// <param name="highInclusive">The inclusive upper bound.</param>
    /// <returns>The number of oracle keys within the range.</returns>
    private static int CountInRangeOracle(int[] oracle, int lowInclusive, int highInclusive)
    {
        int lowPosition = Array.BinarySearch(oracle, lowInclusive);
        int start = lowPosition >= 0 ? lowPosition : ~lowPosition;

        int highPosition = Array.BinarySearch(oracle, highInclusive);
        int end = highPosition >= 0 ? highPosition + 1 : ~highPosition;

        return end - start;
    }
}
