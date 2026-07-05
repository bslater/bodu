// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountMinSketchTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class CountMinSketchTests
{
    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Add(T)" /> throws <see cref="ArgumentNullException" /> with the
    /// expected parameter name when the item is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var sketch = new CountMinSketch<string>(0.01, 0.01);

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sketch.Add(null!);
        }, "item");
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Add(T, long)" /> throws <see cref="ArgumentNullException" /> with
    /// the expected parameter name when the item is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNull_ForCountedOverload_ShouldThrowArgumentNullException()
    {
        var sketch = new CountMinSketch<string>(0.01, 0.01);

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sketch.Add(null!, 5);
        }, "item");
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Add(T, long)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with the expected parameter name when the count is zero or
    /// negative, and leaves <see cref="CountMinSketch{T}.TotalCount" /> unchanged.
    /// </summary>
    [TestMethod]
    [DataRow(0L)]
    [DataRow(-1L)]
    [DataRow(long.MinValue)]
    public void Add_WhenCountIsZeroOrNegative_ShouldThrowArgumentOutOfRangeException(long count)
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);

        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            sketch.Add(42, count);
        }, "count");

        Assert.AreEqual(0L, sketch.TotalCount);
    }

    /// <summary>
    /// Verifies that after a single-occurrence <see cref="CountMinSketch{T}.Add(T)" />, the estimate for the item is
    /// at least 1 and equals 1 on a sparse sketch.
    /// </summary>
    [TestMethod]
    public void Add_WhenSingleItemAdded_ShouldEstimateOne()
    {
        var sketch = new CountMinSketch<int>(0.001, 0.001);

        sketch.Add(42);

        Assert.AreEqual(1L, sketch.EstimateCount(42));
    }

    /// <summary>
    /// Verifies that repeated single-occurrence adds accumulate: three adds of the same item report an estimate of 3
    /// on a sparse sketch.
    /// </summary>
    [TestMethod]
    public void Add_WhenSameItemAddedRepeatedly_ShouldAccumulateCounts()
    {
        var sketch = new CountMinSketch<int>(0.001, 0.001);

        sketch.Add(42);
        sketch.Add(42);
        sketch.Add(42);

        Assert.AreEqual(3L, sketch.EstimateCount(42));
    }

    /// <summary>
    /// Verifies that the counted overload adds the full magnitude in one call: a single add of 41 after an add of 1
    /// reports an estimate of 42 on a sparse sketch.
    /// </summary>
    [TestMethod]
    public void Add_WhenCountedAddFollowsSingleAdd_ShouldSumMagnitudes()
    {
        var sketch = new CountMinSketch<int>(0.001, 0.001);

        sketch.Add(7);
        sketch.Add(7, 41);

        Assert.AreEqual(42L, sketch.EstimateCount(7));
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.TotalCount" /> accumulates the magnitudes of every add:
    /// single-occurrence adds contribute 1 and counted adds contribute their count.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemsAdded_ShouldAccumulateTotalCount()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);

        sketch.Add(1);
        sketch.Add(2, 5);
        sketch.Add(3, 7);

        Assert.AreEqual(13L, sketch.TotalCount);
    }

    /// <summary>
    /// Verifies that items hashed through a custom comparer share counters with any value the comparer treats as
    /// equal — here a case-insensitive string comparer.
    /// </summary>
    [TestMethod]
    public void Add_WhenCustomComparerSupplied_ShouldCountComparerEqualValuesTogether()
    {
        var sketch = new CountMinSketch<string>(0.001, 0.001, StringComparer.OrdinalIgnoreCase);

        sketch.Add("HELLO", 2);
        sketch.Add("hello", 3);

        Assert.AreEqual(5L, sketch.EstimateCount("Hello"));
    }
}
