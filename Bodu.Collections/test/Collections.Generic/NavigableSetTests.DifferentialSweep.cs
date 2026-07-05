// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.DifferentialSweep.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>The exclusive upper bound of the value universe exercised by the differential sweep.</summary>
    private const int SweepUniverse = 512;

    /// <summary>The number of mixed operations applied by the differential sweep.</summary>
    private const int SweepOperations = 20_000;

    /// <summary>The number of operations between full-oracle checkpoints.</summary>
    private const int SweepCheckpointInterval = 500;

    /// <summary>
    /// Verifies that 20,000 seeded mixed add/remove/contains operations mirrored against a <see cref="SortedSet{T}" />
    /// leave the <see cref="NavigableSet{T}" /> in exactly the mirrored state at every checkpoint — full ordered
    /// content, count, and min/max — and that a floor/ceiling/higher/lower/rank/select/count-in-range probe battery
    /// agrees with a sorted-array binary-search oracle. This sweep is the correctness gate for the size-augmented
    /// red-black tree.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void NavigableSet_WhenRandomOperationSweepApplied_ShouldMatchSortedSetAndArrayOracle()
    {
        var random = new Random(20260705);
        var sut = new NavigableSet<int>();
        var mirror = new SortedSet<int>();

        for (int op = 1; op <= SweepOperations; op++)
        {
            int value = random.Next(SweepUniverse);
            switch (random.Next(5))
            {
                case 0:
                case 1:
                    // Bias toward adds so the tree grows through every insertion fixup path.
                    Assert.AreEqual(mirror.Add(value), sut.Add(value), $"Add({value}) disagrees at op {op}.");
                    break;

                case 2:
                case 3:
                    Assert.AreEqual(mirror.Remove(value), sut.Remove(value), $"Remove({value}) disagrees at op {op}.");
                    break;

                default:
                    Assert.AreEqual(mirror.Contains(value), sut.Contains(value), $"Contains({value}) disagrees at op {op}.");
                    break;
            }

            if (op % SweepCheckpointInterval == 0)
                AssertMatchesOracles(sut, mirror, op);
        }

        // Drain the survivors through Remove to sweep the deletion fixup paths once more.
        foreach (int value in mirror.ToArray())
        {
            Assert.IsTrue(sut.Remove(value));
        }

        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Asserts that the set agrees with the <see cref="SortedSet{T}" /> mirror on content, count, and extremes, and
    /// with a sorted-array binary-search oracle on every navigation, rank, select, and range-count probe.
    /// </summary>
    /// <param name="sut">The set under test.</param>
    /// <param name="mirror">The mirrored <see cref="SortedSet{T}" />.</param>
    /// <param name="op">The operation ordinal, for failure labelling.</param>
    private static void AssertMatchesOracles(NavigableSet<int> sut, SortedSet<int> mirror, int op)
    {
        int[] oracle = mirror.ToArray();

        Assert.AreEqual(oracle.Length, sut.Count, $"Count disagrees at op {op}.");
        CollectionAssert.AreEqual(oracle, sut.ToList(), $"Ordered content disagrees at op {op}.");

        if (oracle.Length > 0)
        {
            Assert.AreEqual(mirror.Min, sut.Min, $"Min disagrees at op {op}.");
            Assert.AreEqual(mirror.Max, sut.Max, $"Max disagrees at op {op}.");
        }
        else
        {
            Assert.IsFalse(sut.TryGetMin(out _), $"TryGetMin should fail on empty at op {op}.");
        }

        // Select must return exactly the sorted-array element at every rank.
        for (int rank = 0; rank < oracle.Length; rank++)
            Assert.AreEqual(oracle[rank], sut.GetAt(rank), $"GetAt({rank}) disagrees at op {op}.");

        // Probe every universe boundary region plus a sampled interior battery.
        for (int probe = -1; probe <= SweepUniverse; probe += 7)
        {
            AssertNavigationMatchesOracle(sut, oracle, probe, op);
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
    /// Asserts the four navigation queries and the rank query for <paramref name="probe" /> against the sorted-array
    /// oracle via binary search.
    /// </summary>
    /// <param name="sut">The set under test.</param>
    /// <param name="oracle">The sorted, distinct oracle array.</param>
    /// <param name="probe">The probe value.</param>
    /// <param name="op">The operation ordinal, for failure labelling.</param>
    private static void AssertNavigationMatchesOracle(NavigableSet<int> sut, int[] oracle, int probe, int op)
    {
        int position = Array.BinarySearch(oracle, probe);
        int insertion = position >= 0 ? position : ~position;

        // Floor: greatest element <= probe.
        int floorIndex = position >= 0 ? position : insertion - 1;
        Assert.AreEqual(floorIndex >= 0, sut.TryGetFloor(probe, out int floor), $"TryGetFloor({probe}) presence disagrees at op {op}.");
        if (floorIndex >= 0)
            Assert.AreEqual(oracle[floorIndex], floor, $"TryGetFloor({probe}) disagrees at op {op}.");

        // Ceiling: least element >= probe.
        int ceilingIndex = position >= 0 ? position : insertion;
        Assert.AreEqual(ceilingIndex < oracle.Length, sut.TryGetCeiling(probe, out int ceiling), $"TryGetCeiling({probe}) presence disagrees at op {op}.");
        if (ceilingIndex < oracle.Length)
            Assert.AreEqual(oracle[ceilingIndex], ceiling, $"TryGetCeiling({probe}) disagrees at op {op}.");

        // Higher: least element > probe.
        int higherIndex = position >= 0 ? position + 1 : insertion;
        Assert.AreEqual(higherIndex < oracle.Length, sut.TryGetHigher(probe, out int higher), $"TryGetHigher({probe}) presence disagrees at op {op}.");
        if (higherIndex < oracle.Length)
            Assert.AreEqual(oracle[higherIndex], higher, $"TryGetHigher({probe}) disagrees at op {op}.");

        // Lower: greatest element < probe.
        int lowerIndex = insertion - 1;
        Assert.AreEqual(lowerIndex >= 0, sut.TryGetLower(probe, out int lower), $"TryGetLower({probe}) presence disagrees at op {op}.");
        if (lowerIndex >= 0)
            Assert.AreEqual(oracle[lowerIndex], lower, $"TryGetLower({probe}) disagrees at op {op}.");

        // Rank: index in sorted order, -1 when absent.
        Assert.AreEqual(position >= 0 ? position : -1, sut.IndexOf(probe), $"IndexOf({probe}) disagrees at op {op}.");
    }

    /// <summary>
    /// Computes the inclusive-range count over the sorted oracle array via binary search.
    /// </summary>
    /// <param name="oracle">The sorted, distinct oracle array.</param>
    /// <param name="lowInclusive">The inclusive lower bound.</param>
    /// <param name="highInclusive">The inclusive upper bound.</param>
    /// <returns>The number of oracle elements within the range.</returns>
    private static int CountInRangeOracle(int[] oracle, int lowInclusive, int highInclusive)
    {
        int lowPosition = Array.BinarySearch(oracle, lowInclusive);
        int start = lowPosition >= 0 ? lowPosition : ~lowPosition;

        int highPosition = Array.BinarySearch(oracle, highInclusive);
        int end = highPosition >= 0 ? highPosition + 1 : ~highPosition;

        return end - start;
    }
}
