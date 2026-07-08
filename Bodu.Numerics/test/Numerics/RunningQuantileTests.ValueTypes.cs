// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningQuantileTests.ValueTypes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class RunningQuantileTests
{
    /// <summary>
    /// Verifies that passing the estimator by value gives the callee an independent copy — the caller does not
    /// observe the callee's additions.
    /// </summary>
    [TestMethod]
    public void ValueTypes_WhenPassedByValue_ShouldNotObserveCalleeMutation()
    {
        var median = RunningQuantile<double>.CreateMedian();

        AddOneByValue(median);

        Assert.AreEqual(0L, median.Count);
    }

    /// <summary>
    /// Verifies that passing the estimator by <see langword="ref" /> shares the instance — the caller observes the
    /// callee's additions, including the inline-array marker state.
    /// </summary>
    [TestMethod]
    public void ValueTypes_WhenPassedByRef_ShouldObserveCalleeMutation()
    {
        var median = RunningQuantile<double>.CreateMedian();

        AddOneByRef(ref median);

        Assert.AreEqual(1L, median.Count);
        Assert.AreEqual(4.0, median.Estimate, 1e-12);
    }

    /// <summary>
    /// Adds a sample to a by-value copy of the estimator; the caller's instance is unaffected.
    /// </summary>
    /// <param name="estimator">The estimator, received by value.</param>
    private static void AddOneByValue(RunningQuantile<double> estimator) =>
        estimator.Add(4.0);

    /// <summary>
    /// Adds a sample to the caller's estimator through a <see langword="ref" /> parameter.
    /// </summary>
    /// <param name="estimator">The estimator, received by reference.</param>
    private static void AddOneByRef(ref RunningQuantile<double> estimator) =>
        estimator.Add(4.0);
}
