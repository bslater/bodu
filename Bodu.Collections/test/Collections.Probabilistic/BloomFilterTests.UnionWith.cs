// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BloomFilterTests.UnionWith.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class BloomFilterTests
{
    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.UnionWith" /> throws <see cref="ArgumentNullException" /> with the
    /// expected parameter name when the other filter is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var filter = new BloomFilter<int>(100, 0.01);

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            filter.UnionWith(null!);
        }, "other");
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.UnionWith" /> merges membership: after the union, this filter reports
    /// the items added to either source filter as present.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenFiltersAreCompatible_ShouldMergeMembership()
    {
        var evens = new BloomFilter<int>(1000, 0.01);
        var odds = new BloomFilter<int>(1000, 0.01);
        for (var i = 0; i < 500; i++)
        {
            evens.Add(i * 2);
            odds.Add((i * 2) + 1);
        }

        evens.UnionWith(odds);

        for (var i = 0; i < 1000; i++)
            Assert.IsTrue(evens.MightContain(i), $"Merged filter did not contain {i}.");
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.UnionWith" /> does not modify the other filter: its estimated
    /// false-positive rate (a deterministic function of its bit array) is unchanged by the merge.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenCalled_ShouldNotModifyOtherFilter()
    {
        var target = new BloomFilter<int>(1000, 0.01);
        var other = new BloomFilter<int>(1000, 0.01);
        for (var i = 0; i < 100; i++)
        {
            target.Add(i);
            other.Add(i + 5000);
        }

        var otherEstimateBefore = other.EstimatedFalsePositiveRate;

        target.UnionWith(other);

        Assert.AreEqual(otherEstimateBefore, other.EstimatedFalsePositiveRate);
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.UnionWith" /> throws <see cref="ArgumentException" /> with the
    /// expected parameter name when the filters were sized differently and therefore have incompatible geometry.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenGeometryDiffers_ShouldThrowArgumentException()
    {
        var filter = new BloomFilter<int>(1000, 0.01);
        var differentSizing = new BloomFilter<int>(500, 0.01);

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            filter.UnionWith(differentSizing);
        }, "other");
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.UnionWith" /> throws <see cref="ArgumentException" /> when the filters
    /// share geometry but use distinct, non-equal comparer instances.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenComparersAreDistinctInstances_ShouldThrowArgumentException()
    {
        var filter = new BloomFilter<int>(100, 0.01, new ReferenceOnlyIntComparer());
        var otherComparerInstance = new BloomFilter<int>(100, 0.01, new ReferenceOnlyIntComparer());

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            filter.UnionWith(otherComparerInstance);
        }, "other");
    }

    /// <summary>
    /// Verifies that filters sharing the same comparer instance and geometry are compatible, so the union succeeds.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenSameComparerInstanceShared_ShouldSucceed()
    {
        var comparer = new ReferenceOnlyIntComparer();
        var filter = new BloomFilter<int>(100, 0.01, comparer);
        var other = new BloomFilter<int>(100, 0.01, comparer);
        other.Add(42);

        filter.UnionWith(other);

        Assert.IsTrue(filter.MightContain(42));
    }

    /// <summary>
    /// Verifies that a self-union is a harmless no-op: membership and the estimated false-positive rate are
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenUnionedWithSelf_ShouldLeaveStateUnchanged()
    {
        var filter = new BloomFilter<int>(100, 0.01);
        filter.Add(1);
        filter.Add(2);
        var estimateBefore = filter.EstimatedFalsePositiveRate;

        filter.UnionWith(filter);

        Assert.IsTrue(filter.MightContain(1));
        Assert.IsTrue(filter.MightContain(2));
        Assert.AreEqual(estimateBefore, filter.EstimatedFalsePositiveRate);
    }
}
