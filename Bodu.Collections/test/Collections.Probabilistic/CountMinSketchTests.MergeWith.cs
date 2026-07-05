// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountMinSketchTests.MergeWith.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class CountMinSketchTests
{
    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.MergeWith" /> throws <see cref="ArgumentNullException" /> with the
    /// expected parameter name when the other sketch is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sketch.MergeWith(null!);
        }, "other");
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.MergeWith" /> sums counts cell-wise: after the merge, a sparse
    /// target reports the combined counts of items added to either sketch, including items counted in both.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenSketchesAreCompatible_ShouldSumCounts()
    {
        var target = new CountMinSketch<int>(0.001, 0.001);
        var other = new CountMinSketch<int>(0.001, 0.001);
        target.Add(1, 10);
        target.Add(2, 20);
        other.Add(2, 5);
        other.Add(3, 30);

        target.MergeWith(other);

        Assert.AreEqual(10L, target.EstimateCount(1));
        Assert.AreEqual(25L, target.EstimateCount(2));
        Assert.AreEqual(30L, target.EstimateCount(3));
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.MergeWith" /> sums the running totals: after the merge,
    /// <see cref="CountMinSketch{T}.TotalCount" /> equals the sum of both sketches' totals.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenSketchesAreCompatible_ShouldSumTotalCounts()
    {
        var target = new CountMinSketch<int>(0.01, 0.01);
        var other = new CountMinSketch<int>(0.01, 0.01);
        target.Add(1, 100);
        other.Add(2, 23);

        target.MergeWith(other);

        Assert.AreEqual(123L, target.TotalCount);
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.MergeWith" /> does not modify the other sketch: its estimates and
    /// total are unchanged by the merge.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenCalled_ShouldNotModifyOtherSketch()
    {
        var target = new CountMinSketch<int>(0.001, 0.001);
        var other = new CountMinSketch<int>(0.001, 0.001);
        target.Add(1, 10);
        other.Add(2, 20);

        target.MergeWith(other);

        Assert.AreEqual(20L, other.EstimateCount(2));
        Assert.AreEqual(0L, other.EstimateCount(1));
        Assert.AreEqual(20L, other.TotalCount);
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.MergeWith" /> throws <see cref="ArgumentException" /> with the
    /// expected parameter name when the sketches were sized differently and therefore have incompatible geometry.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenGeometryDiffers_ShouldThrowArgumentException()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);
        var differentSizing = new CountMinSketch<int>(0.02, 0.01);

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            sketch.MergeWith(differentSizing);
        }, "other");
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.MergeWith" /> throws <see cref="ArgumentException" /> when the
    /// sketches share geometry but use distinct, non-equal comparer instances.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenComparersAreDistinctInstances_ShouldThrowArgumentException()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01, new ReferenceOnlyIntComparer());
        var otherComparerInstance = new CountMinSketch<int>(0.01, 0.01, new ReferenceOnlyIntComparer());

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            sketch.MergeWith(otherComparerInstance);
        }, "other");
    }

    /// <summary>
    /// Verifies that sketches sharing the same comparer instance and geometry are compatible, so the merge succeeds.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenSameComparerInstanceShared_ShouldSucceed()
    {
        var comparer = new ReferenceOnlyIntComparer();
        var sketch = new CountMinSketch<int>(0.001, 0.001, comparer);
        var other = new CountMinSketch<int>(0.001, 0.001, comparer);
        other.Add(42, 6);

        sketch.MergeWith(other);

        Assert.AreEqual(6L, sketch.EstimateCount(42));
    }

    /// <summary>
    /// Verifies that a self-merge doubles every count and the running total, matching the documented
    /// counting-the-stream-twice semantics.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenMergedWithSelf_ShouldDoubleCountsAndTotal()
    {
        var sketch = new CountMinSketch<int>(0.001, 0.001);
        sketch.Add(1, 10);
        sketch.Add(2, 20);

        sketch.MergeWith(sketch);

        Assert.AreEqual(20L, sketch.EstimateCount(1));
        Assert.AreEqual(40L, sketch.EstimateCount(2));
        Assert.AreEqual(60L, sketch.TotalCount);
    }

    /// <summary>
    /// Verifies that the cell-wise merge uses checked arithmetic: merging a sketch whose counters hold
    /// <see cref="long.MaxValue" /> with one holding a further count for the same item throws
    /// <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenCellSumOverflows_ShouldThrowOverflowException()
    {
        var saturated = new CountMinSketch<int>(0.01, 0.01);
        var other = new CountMinSketch<int>(0.01, 0.01);
        saturated.Add(42, long.MaxValue);
        other.Add(42, 1);

        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            saturated.MergeWith(other);
        });
    }
}
