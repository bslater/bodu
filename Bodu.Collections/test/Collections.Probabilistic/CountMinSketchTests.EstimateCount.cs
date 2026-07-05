// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountMinSketchTests.EstimateCount.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class CountMinSketchTests
{
    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.EstimateCount" /> throws <see cref="ArgumentNullException" /> with
    /// the expected parameter name when the item is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EstimateCount_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var sketch = new CountMinSketch<string>(0.01, 0.01);

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = sketch.EstimateCount(null!);
        }, "item");
    }

    /// <summary>
    /// Verifies that an empty sketch reports an estimate of zero for any queried item.
    /// </summary>
    [TestMethod]
    public void EstimateCount_WhenSketchIsEmpty_ShouldReturnZero()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);

        for (var i = 0; i < 100; i++)
            Assert.AreEqual(0L, sketch.EstimateCount(i), $"Empty sketch reported a non-zero estimate for {i}.");
    }

    /// <summary>
    /// Verifies the never-underestimate guarantee on a deliberately overloaded sketch: after 1000 distinct items are
    /// added with known counts into a 272×5 sketch, every estimate is at least the item's true count.
    /// </summary>
    [TestMethod]
    public void EstimateCount_WhenSketchIsHeavilyLoaded_ShouldNeverUnderestimate()
    {
        var sketch = new CountMinSketch<int>(epsilon: 0.01, delta: 0.01);

        for (var i = 0; i < 1000; i++)
            sketch.Add(i, 1 + (i % 10));

        for (var i = 0; i < 1000; i++)
            Assert.IsGreaterThanOrEqualTo(1 + (i % 10), sketch.EstimateCount(i), $"Estimate for item {i} fell below the true count.");
    }

    /// <summary>
    /// Verifies that an item that was never added can report a positive estimate from counter collisions but never a
    /// negative one.
    /// </summary>
    [TestMethod]
    public void EstimateCount_WhenItemWasNeverAdded_ShouldNeverReturnNegative()
    {
        var sketch = new CountMinSketch<int>(epsilon: 0.01, delta: 0.01);

        for (var i = 0; i < 1000; i++)
            sketch.Add(i, 1 + (i % 10));

        for (var i = 1000; i < 2000; i++)
            Assert.IsGreaterThanOrEqualTo(0L, sketch.EstimateCount(i), $"Estimate for unseen item {i} was negative.");
    }

    /// <summary>
    /// Verifies that values a custom comparer treats as equal report the same estimate — here a case-insensitive
    /// string comparer.
    /// </summary>
    [TestMethod]
    public void EstimateCount_WhenCustomComparerSupplied_ShouldReportSameEstimateForComparerEqualValues()
    {
        var sketch = new CountMinSketch<string>(0.001, 0.001, StringComparer.OrdinalIgnoreCase);

        sketch.Add("VALUE", 9);

        Assert.AreEqual(sketch.EstimateCount("VALUE"), sketch.EstimateCount("value"));
    }

    /// <summary>
    /// Verifies the count-min error contract over a deterministic 50,000-add sweep of 5,000 distinct items with
    /// skewed counts (count = 1 + (i % 100)): every estimate is at least the exact count, and for the 100 heaviest
    /// items the estimate stays within the formal bound of exact + ε·TotalCount at the design epsilon. The bound
    /// holds per query with probability 1 − δ; at this load the expected per-row error is well below the bound, so
    /// the deterministic sweep passes with overwhelming margin.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void EstimateCount_WhenSweptWithSkewedCounts_ShouldRespectErrorBounds()
    {
        var sketch = new CountMinSketch<int>(epsilon: 0.01, delta: 0.01);
        var exact = new long[5000];

        for (var i = 0; i < 50_000; i++)
        {
            var item = i % 5000;
            var count = 1 + (i % 100);
            sketch.Add(item, count);
            exact[item] += count;
        }

        for (var item = 0; item < 5000; item++)
            Assert.IsGreaterThanOrEqualTo(exact[item], sketch.EstimateCount(item), $"Estimate for item {item} fell below the exact count.");

        var bound = (long)(sketch.Epsilon * sketch.TotalCount);
        var heavyHitters = Enumerable.Range(0, 5000).OrderByDescending(item => exact[item]).Take(100);
        foreach (var item in heavyHitters)
            Assert.IsLessThanOrEqualTo(exact[item] + bound, sketch.EstimateCount(item), $"Estimate for heavy hitter {item} exceeded the ε·TotalCount bound.");
    }
}
