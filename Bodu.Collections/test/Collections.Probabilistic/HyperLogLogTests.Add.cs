// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HyperLogLogTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class HyperLogLogTests
{
    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.Add" /> throws <see cref="ArgumentNullException" /> with the expected
    /// parameter name when the item is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var sketch = new HyperLogLog<string>(precision: 10);

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sketch.Add(null!);
        }, "item");
    }

    /// <summary>
    /// Verifies that duplicates do not move the estimate: adding the same item 1,000 times touches a single register,
    /// so the small-range linear-counting estimate stays close to one distinct element (between 0.5 and 2).
    /// </summary>
    [TestMethod]
    public void Add_WhenSameItemAddedManyTimes_ShouldEstimateOneDistinctElement()
    {
        var sketch = new HyperLogLog<int>(precision: 14);

        for (var i = 0; i < 1000; i++)
            sketch.Add(42);

        var estimate = sketch.EstimateCardinality();

        Assert.IsTrue(estimate is >= 0.5 and <= 2.0, $"Estimate {estimate} for a single repeated item fell outside [0.5, 2].");
    }

    /// <summary>
    /// Verifies that re-adding an already observed item leaves the sketch state unchanged: the estimate after 1,000
    /// duplicate adds equals the estimate after the first add exactly.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsDuplicate_ShouldLeaveEstimateUnchanged()
    {
        var sketch = new HyperLogLog<int>(precision: 10);
        sketch.Add(42);
        var afterFirstAdd = sketch.EstimateCardinality();

        for (var i = 0; i < 1000; i++)
            sketch.Add(42);

        Assert.AreEqual(afterFirstAdd, sketch.EstimateCardinality());
    }

    /// <summary>
    /// Verifies that adding distinct items grows the estimate: an empty sketch estimates 0, and each order-of-magnitude
    /// increase in distinct items produces a strictly larger estimate.
    /// </summary>
    [TestMethod]
    public void Add_WhenDistinctItemsAdded_ShouldGrowEstimate()
    {
        var sketch = new HyperLogLog<int>(precision: 12);
        var previous = sketch.EstimateCardinality();
        Assert.AreEqual(0.0, previous);

        var added = 0;
        foreach (var target in new[] { 10, 100, 1000, 10_000 })
        {
            while (added < target)
                sketch.Add(added++);

            var estimate = sketch.EstimateCardinality();

            Assert.IsTrue(estimate > previous, $"Estimate {estimate} after {target} distinct items did not exceed the previous estimate {previous}.");
            previous = estimate;
        }
    }
}
