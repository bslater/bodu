// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BloomFilterTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class BloomFilterTests
{
    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.Add" /> throws <see cref="ArgumentNullException" /> with the expected
    /// parameter name when the item is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var filter = new BloomFilter<string>(100, 0.01);

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            filter.Add(null!);
        }, "item");
    }

    /// <summary>
    /// Verifies that after <see cref="BloomFilter{T}.Add" /> returns, <see cref="BloomFilter{T}.MightContain" /> is
    /// guaranteed to report the added item as present.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemAdded_ShouldMakeMightContainReturnTrue()
    {
        var filter = new BloomFilter<int>(100, 0.01);

        filter.Add(42);

        Assert.IsTrue(filter.MightContain(42));
    }

    /// <summary>
    /// Verifies that adding the same item repeatedly is idempotent on the bit array: the estimated false-positive
    /// rate does not change after the first add.
    /// </summary>
    [TestMethod]
    public void Add_WhenSameItemAddedTwice_ShouldNotChangeEstimatedFalsePositiveRate()
    {
        var filter = new BloomFilter<int>(100, 0.01);
        filter.Add(42);
        var estimateAfterFirstAdd = filter.EstimatedFalsePositiveRate;

        filter.Add(42);

        Assert.AreEqual(estimateAfterFirstAdd, filter.EstimatedFalsePositiveRate);
    }

    /// <summary>
    /// Verifies that adding a first item raises <see cref="BloomFilter{T}.EstimatedFalsePositiveRate" /> above the
    /// zero reported by an empty filter.
    /// </summary>
    [TestMethod]
    public void Add_WhenFirstItemAdded_ShouldRaiseEstimatedFalsePositiveRateAboveZero()
    {
        var filter = new BloomFilter<int>(100, 0.01);
        Assert.AreEqual(0.0, filter.EstimatedFalsePositiveRate);

        filter.Add(1);

        Assert.IsGreaterThan(0.0, filter.EstimatedFalsePositiveRate);
    }

    /// <summary>
    /// Verifies that items hashed through a custom comparer are found via any value the comparer treats as equal —
    /// here a case-insensitive string comparer.
    /// </summary>
    [TestMethod]
    public void Add_WhenCustomComparerSupplied_ShouldFindComparerEqualValues()
    {
        var filter = new BloomFilter<string>(100, 0.01, StringComparer.OrdinalIgnoreCase);

        filter.Add("HELLO");

        Assert.IsTrue(filter.MightContain("hello"));
        Assert.IsTrue(filter.MightContain("Hello"));
    }
}
