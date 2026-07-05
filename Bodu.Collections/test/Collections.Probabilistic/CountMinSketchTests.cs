// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountMinSketchTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

/// <summary>
/// Unit tests for <see cref="CountMinSketch{T}" />.
/// </summary>
[TestClass]
public sealed partial class CountMinSketchTests
{
    /// <summary>
    /// Verifies that a sparse sketch reports the exact count for every one of 20 items added with known counts,
    /// exercising the primary add-then-estimate happy path; the estimate is also never below the true count.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void CountMinSketch_AddThenEstimateCount_ShouldReportKnownCountsForSparseSketch()
    {
        var sketch = new CountMinSketch<int>(epsilon: 0.001, delta: 0.001);

        for (var i = 0; i < 20; i++)
            sketch.Add(i, i + 1);

        for (var i = 0; i < 20; i++)
        {
            Assert.IsGreaterThanOrEqualTo(i + 1, sketch.EstimateCount(i), $"Estimate for item {i} underestimated the true count.");
            Assert.AreEqual(i + 1, sketch.EstimateCount(i), $"Sparse sketch did not report the exact count for item {i}.");
        }
    }

    /// <summary>
    /// A reference-type equality comparer whose <see cref="object.Equals(object)" /> is reference-based, so two
    /// instances are never equal — used to drive the <see cref="CountMinSketch{T}.MergeWith" /> comparer-compatibility
    /// checks.
    /// </summary>
    private sealed class ReferenceOnlyIntComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;

        public int GetHashCode(int obj) => obj;
    }
}
