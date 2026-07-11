// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.CapacityPartitioning.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that the segment capacity slices always sum to the requested capacity exactly.
    /// </summary>
    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(4, 1)]
    [DataRow(4, 4)]
    [DataRow(4, 7)]
    [DataRow(8, 10)]
    [DataRow(8, 64)]
    [DataRow(32, 1000)]
    [DataRow(3, 100)]
    public void Ctor_WhenPartitioningCapacity_ShouldSumSegmentSlicesToCapacityExactly(int concurrencyLevel, int capacity)
    {
        var dictionary = new ConcurrentEvictingDictionary<int, int>(concurrencyLevel, capacity, EvictingDictionaryPolicy.LeastRecentlyUsed, null, null);

        int sum = 0;
        for (int i = 0; i < dictionary.SegmentCount; i++)
        {
            int slice = dictionary.GetSegmentCapacity(i);
            Assert.IsTrue(slice >= 1, $"Segment {i} received a zero-capacity slice.");
            sum += slice;
        }

        Assert.AreEqual(capacity, sum, "The segment slices must sum to the total capacity.");
    }

    /// <summary>
    /// Verifies that the segment count is the smaller of the concurrency level and the capacity, so no segment owns
    /// zero slots.
    /// </summary>
    [TestMethod]
    [DataRow(8, 3, 3)]
    [DataRow(8, 8, 8)]
    [DataRow(8, 100, 8)]
    [DataRow(1, 100, 1)]
    public void Ctor_WhenConcurrencyLevelSupplied_ShouldClampSegmentCountToCapacity(int concurrencyLevel, int capacity, int expectedSegments)
    {
        var dictionary = new ConcurrentEvictingDictionary<int, int>(concurrencyLevel, capacity, EvictingDictionaryPolicy.LeastRecentlyUsed, null, null);

        Assert.AreEqual(expectedSegments, dictionary.SegmentCount);
    }

    /// <summary>
    /// Verifies that a non-positive concurrency level throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConcurrencyLevelIsZeroOrNegative_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentEvictingDictionary<int, int>(0, 4, EvictingDictionaryPolicy.LeastRecentlyUsed, null, null);
        });

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentEvictingDictionary<int, int>(-1, 4, EvictingDictionaryPolicy.LeastRecentlyUsed, null, null);
        });
    }

    /// <summary>
    /// Verifies that the default concurrency-level clamp keeps the segment count within <c>[1, 32]</c> regardless of
    /// the reported processor count.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [DataRow(8, 8)]
    [DataRow(32, 32)]
    [DataRow(128, 32)]
    public void ClampDefaultConcurrencyLevel_WhenProcessorCountSupplied_ShouldClampToEnvelope(int processorCount, int expected)
    {
        Assert.AreEqual(expected, ConcurrentEvictingDictionary<int, int>.ClampDefaultConcurrencyLevel(processorCount));
    }

    /// <summary>
    /// Verifies that a remainder is distributed one extra slot per leading segment: the slice sizes differ by at most
    /// one.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityNotDivisible_ShouldDistributeRemainderEvenly()
    {
        var dictionary = new ConcurrentEvictingDictionary<int, int>(4, 10, EvictingDictionaryPolicy.LeastRecentlyUsed, null, null);

        int[] slices = Enumerable.Range(0, dictionary.SegmentCount)
            .Select(dictionary.GetSegmentCapacity)
            .ToArray();

        CollectionAssert.AreEqual(new[] { 3, 3, 2, 2 }, slices);
    }

    /// <summary>
    /// Verifies that keys distributed across every segment can fill the dictionary to its exact capacity.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeysCoverEverySegment_ShouldFillToExactCapacity()
    {
        var dictionary = new ConcurrentEvictingDictionary<int, int>(4, 16, EvictingDictionaryPolicy.FirstInFirstOut, null, null);

        for (int i = 0; i < 16; i++)
            dictionary.Add(i, i);

        Assert.AreEqual(16, dictionary.Count);
        Assert.AreEqual(0, dictionary.EvictionCount, "Sixteen evenly distributed keys must fill the dictionary without evicting.");
    }
}
