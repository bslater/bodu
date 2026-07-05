// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BloomFilterTests.MightContain.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class BloomFilterTests
{
    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.MightContain" /> throws <see cref="ArgumentNullException" /> with the
    /// expected parameter name when the item is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void MightContain_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var filter = new BloomFilter<string>(100, 0.01);

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = filter.MightContain(null!);
        }, "item");
    }

    /// <summary>
    /// Verifies that an empty filter reports every probed value as definitely absent, because no bits are set.
    /// </summary>
    [TestMethod]
    public void MightContain_WhenFilterIsEmpty_ShouldReturnFalseForAllValues()
    {
        var filter = new BloomFilter<int>(1000, 0.01);

        for (var i = 0; i < 100; i++)
            Assert.IsFalse(filter.MightContain(i), $"Empty filter reported {i} as present.");
    }

    /// <summary>
    /// Verifies the no-false-negatives guarantee: every one of 1000 added items is always reported as present. The
    /// sweep is fully deterministic because <see cref="int" /> hash codes are stable.
    /// </summary>
    [TestMethod]
    public void MightContain_WhenItemsWereAdded_ShouldNeverReportFalseNegative()
    {
        var filter = new BloomFilter<int>(expectedItems: 1000, falsePositiveRate: 0.01);

        for (var i = 0; i < 1000; i++)
            filter.Add(i * 7919);

        for (var i = 0; i < 1000; i++)
            Assert.IsTrue(filter.MightContain(i * 7919), $"Added item {i * 7919} was not found.");
    }

    /// <summary>
    /// Verifies that the observed false-positive rate of a filter loaded to its design capacity stays within 3× the
    /// design rate: 10,000 sequential ints are inserted into a filter sized for n = 10,000 at p = 0.01 and 10,000
    /// disjoint absent ints are probed. The 3× headroom (0.03) absorbs the statistical variance of a single
    /// deterministic sample — the expected count is ~100 false positives with a standard deviation of ~10, so 300 is
    /// far outside plausible noise while still catching a broken hash or sizing regression. The sweep is
    /// deterministic because <see cref="int" /> hash codes are stable across processes.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void MightContain_WhenFilledToDesignCapacity_ShouldObserveFalsePositiveRateWithinThreeTimesDesignRate()
    {
        const int itemCount = 10_000;
        var filter = new BloomFilter<int>(expectedItems: itemCount, falsePositiveRate: 0.01);

        for (var i = 0; i < itemCount; i++)
            filter.Add(i);

        var falsePositives = 0;
        for (var i = 1_000_000; i < 1_000_000 + itemCount; i++)
        {
            if (filter.MightContain(i))
                falsePositives++;
        }

        var observedRate = falsePositives / (double)itemCount;
        Assert.IsLessThan(0.03, observedRate, $"Observed false-positive rate {observedRate} exceeded 3x the design rate of 0.01.");
    }
}
