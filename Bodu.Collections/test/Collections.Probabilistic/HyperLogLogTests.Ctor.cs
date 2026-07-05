// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HyperLogLogTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class HyperLogLogTests
{
    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> with the expected parameter
    /// name when <c>precision</c> lies outside the inclusive interval [4, 18], including both adjacent boundary
    /// values.
    /// </summary>
    [TestMethod]
    [DataRow(3)]
    [DataRow(19)]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    [DataRow(int.MaxValue)]
    public void Ctor_WhenPrecisionIsOutsideSupportedRange_ShouldThrowArgumentOutOfRangeException(int precision)
    {
        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new HyperLogLog<int>(precision);
        }, "precision");
    }

    /// <summary>
    /// Verifies that the constructor allocates <c>2^b</c> registers for every supported precision and records the
    /// precision on the <see cref="HyperLogLog{T}.Precision" /> property.
    /// </summary>
    [TestMethod]
    [DataRow(4, 16)]
    [DataRow(5, 32)]
    [DataRow(6, 64)]
    [DataRow(10, 1024)]
    [DataRow(14, 16384)]
    [DataRow(18, 262144)]
    public void Ctor_WhenPrecisionIsSupported_ShouldAllocateTwoToThePrecisionRegisters(int precision, int expectedRegisterCount)
    {
        var sketch = new HyperLogLog<int>(precision);

        Assert.AreEqual(precision, sketch.Precision);
        Assert.AreEqual(expectedRegisterCount, sketch.RegisterCount);
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.StandardError" /> reports the known 1.04/√m answers: 0.26 at
    /// precision 4 (m = 16), 0.008125 at precision 14 (m = 16,384), and 0.00203125 at precision 18 (m = 262,144).
    /// </summary>
    [TestMethod]
    [DataRow(4, 0.26)]
    [DataRow(14, 0.008125)]
    [DataRow(18, 0.00203125)]
    public void Ctor_WhenConstructed_ShouldExposeKnownStandardError(int precision, double expectedStandardError)
    {
        var sketch = new HyperLogLog<int>(precision);

        Assert.AreEqual(expectedStandardError, sketch.StandardError, 1e-12);
    }

    /// <summary>
    /// Verifies that a freshly constructed sketch starts empty, with an estimated cardinality of exactly zero.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldStartWithZeroEstimate()
    {
        var sketch = new HyperLogLog<int>(precision: 10);

        Assert.AreEqual(0.0, sketch.EstimateCardinality());
    }

    /// <summary>
    /// Verifies that the one-argument constructor uses <see cref="EqualityComparer{T}.Default" /> and the
    /// two-argument constructor falls back to it when the comparer argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsOmittedOrNull_ShouldUseDefaultComparer()
    {
        var implicitDefault = new HyperLogLog<int>(10);
        var explicitNull = new HyperLogLog<int>(10, null);

        Assert.AreSame(EqualityComparer<int>.Default, implicitDefault.Comparer);
        Assert.AreSame(EqualityComparer<int>.Default, explicitNull.Comparer);
    }

    /// <summary>
    /// Verifies that a custom equality comparer supplied at construction is exposed by the
    /// <see cref="HyperLogLog{T}.Comparer" /> property.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCustomComparerSupplied_ShouldExposeThatComparer()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;

        var sketch = new HyperLogLog<string>(10, comparer);

        Assert.AreSame(comparer, sketch.Comparer);
    }
}
