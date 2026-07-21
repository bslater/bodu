// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HyperLogLogTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

/// <summary>
/// Unit tests for <see cref="HyperLogLog{T}" />.
/// </summary>
[TestClass]
public sealed partial class HyperLogLogTests
{
    /// <summary>
    /// Verifies that a precision-14 sketch estimates 1,000 distinct integers to within 5% of the true count,
    /// exercising the primary add-then-estimate happy path.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void HyperLogLog_AddThenEstimateCardinality_ShouldEstimateOneThousandDistinctItemsWithinFivePercent()
    {
        var sketch = new HyperLogLog<int>(precision: 14);

        for (var i = 0; i < 1000; i++)
            sketch.Add(i);

        var estimate = sketch.EstimateCardinality();

        Assert.IsTrue(Math.Abs(estimate - 1000.0) / 1000.0 < 0.05, $"Estimate {estimate} deviated more than 5% from 1,000.");
    }

    /// <summary>
    /// A reference-type equality comparer whose <see cref="object.Equals(object)" /> is reference-based, so two
    /// instances are never equal — used to drive the <see cref="HyperLogLog{T}.MergeWith" /> comparer-compatibility
    /// checks.
    /// </summary>
    private sealed class ReferenceOnlyIntComparer
        : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;

        public int GetHashCode(int obj) => obj;
    }
}
