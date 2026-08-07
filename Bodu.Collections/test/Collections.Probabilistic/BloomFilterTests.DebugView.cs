// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BloomFilterTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class BloomFilterTests
{
    /// <summary>
    /// Verifies that <see cref="BloomFilterDebugView{T}" /> surfaces the filter's geometry unchanged.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenFilterPopulated_ShouldSurfaceGeometry()
    {
        var filter = new BloomFilter<string>(1000, 0.01);
        filter.Add("alpha");
        filter.Add("beta");

        var view = new BloomFilterDebugView<string>(filter);

        Assert.AreEqual(filter.BitCount, view.BitCount);
        Assert.AreEqual(filter.ExpectedItems, view.ExpectedItems);
        Assert.AreEqual(filter.HashCount, view.HashCount);
        Assert.AreEqual(filter.DesignFalsePositiveRate, view.DesignFalsePositiveRate);
        Assert.AreEqual(filter.EstimatedFalsePositiveRate, view.EstimatedFalsePositiveRate);
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilterDebugView{T}.EstimatedFalsePositiveRate" /> tracks the current fill
    /// rather than reporting the design rate.
    /// </summary>
    [TestMethod]
    public void DebugView_WhenFilterEmpty_ShouldReportZeroEstimatedFalsePositiveRate()
    {
        var filter = new BloomFilter<string>(1000, 0.01);

        var view = new BloomFilterDebugView<string>(filter);

        Assert.AreEqual(0d, view.EstimatedFalsePositiveRate);
    }

    /// <summary>
    /// Verifies that constructing <see cref="BloomFilterDebugView{T}" /> with a <see langword="null" /> filter
    /// throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenFilterIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BloomFilterDebugView<string>(null!);
        });

        Assert.AreEqual("filter", ex.ParamName);
    }
}
