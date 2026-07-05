// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HyperLogLogTests.MergeWith.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class HyperLogLogTests
{
    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.MergeWith" /> throws <see cref="ArgumentNullException" /> with the
    /// expected parameter name when the other sketch is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var sketch = new HyperLogLog<int>(precision: 10);

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sketch.MergeWith(null!);
        }, "other");
    }

    /// <summary>
    /// Verifies that merging sketches of two disjoint streams estimates the union: 5,000 + 5,000 disjoint distinct
    /// integers merge to an estimate within 5% of 10,000.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenStreamsAreDisjoint_ShouldEstimateUnionAsSum()
    {
        var target = new HyperLogLog<int>(precision: 14);
        var other = new HyperLogLog<int>(precision: 14);
        for (var i = 0; i < 5000; i++)
            target.Add(i);
        for (var i = 5000; i < 10_000; i++)
            other.Add(i);

        target.MergeWith(other);

        var estimate = target.EstimateCardinality();
        Assert.IsTrue(Math.Abs(estimate - 10_000.0) / 10_000.0 < 0.05, $"Union estimate {estimate} deviated more than 5% from 10,000.");
    }

    /// <summary>
    /// Verifies that overlapping streams are not double-counted: merging two sketches that observed the identical
    /// element set produces exactly the register state of either source, so the estimate is unchanged.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenStreamsAreIdentical_ShouldNotDoubleCount()
    {
        var target = new HyperLogLog<int>(precision: 12);
        var other = new HyperLogLog<int>(precision: 12);
        for (var i = 0; i < 3000; i++)
        {
            target.Add(i);
            other.Add(i);
        }

        var before = target.EstimateCardinality();

        target.MergeWith(other);

        Assert.AreEqual(before, target.EstimateCardinality());
    }

    /// <summary>
    /// Verifies that a self-merge is idempotent: register-wise maximum against the sketch's own registers leaves the
    /// estimate exactly unchanged.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenMergedWithSelf_ShouldLeaveEstimateUnchanged()
    {
        var sketch = new HyperLogLog<int>(precision: 12);
        for (var i = 0; i < 2000; i++)
            sketch.Add(i);
        var before = sketch.EstimateCardinality();

        sketch.MergeWith(sketch);

        Assert.AreEqual(before, sketch.EstimateCardinality());
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.MergeWith" /> does not modify the other sketch: its estimate is
    /// unchanged by the merge.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenCalled_ShouldNotModifyOtherSketch()
    {
        var target = new HyperLogLog<int>(precision: 12);
        var other = new HyperLogLog<int>(precision: 12);
        for (var i = 0; i < 1000; i++)
            target.Add(i);
        for (var i = 1000; i < 1500; i++)
            other.Add(i);
        var otherBefore = other.EstimateCardinality();

        target.MergeWith(other);

        Assert.AreEqual(otherBefore, other.EstimateCardinality());
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.MergeWith" /> throws <see cref="ArgumentException" /> with the
    /// expected parameter name when the precisions differ and the register arrays are therefore incompatible.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenPrecisionDiffers_ShouldThrowArgumentException()
    {
        var sketch = new HyperLogLog<int>(precision: 10);
        var differentPrecision = new HyperLogLog<int>(precision: 11);

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            sketch.MergeWith(differentPrecision);
        }, "other");
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.MergeWith" /> throws <see cref="ArgumentException" /> when the
    /// sketches share a precision but use distinct, non-equal comparer instances.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenComparersAreDistinctInstances_ShouldThrowArgumentException()
    {
        var sketch = new HyperLogLog<int>(10, new ReferenceOnlyIntComparer());
        var otherComparerInstance = new HyperLogLog<int>(10, new ReferenceOnlyIntComparer());

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            sketch.MergeWith(otherComparerInstance);
        }, "other");
    }

    /// <summary>
    /// Verifies that sketches sharing the same comparer instance and precision are compatible, so the merge succeeds
    /// and the union is reported.
    /// </summary>
    [TestMethod]
    public void MergeWith_WhenSameComparerInstanceShared_ShouldSucceed()
    {
        var comparer = new ReferenceOnlyIntComparer();
        var sketch = new HyperLogLog<int>(14, comparer);
        var other = new HyperLogLog<int>(14, comparer);
        for (var i = 0; i < 1000; i++)
            other.Add(i);

        sketch.MergeWith(other);

        var estimate = sketch.EstimateCardinality();
        Assert.IsTrue(Math.Abs(estimate - 1000.0) / 1000.0 < 0.05, $"Merged estimate {estimate} deviated more than 5% from 1,000.");
    }
}
