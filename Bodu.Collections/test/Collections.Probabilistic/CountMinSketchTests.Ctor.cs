// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountMinSketchTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class CountMinSketchTests
{
    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> with the expected parameter
    /// name when <c>epsilon</c> lies outside the exclusive interval (0, 1), including the boundary values and NaN.
    /// </summary>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(1.0)]
    [DataRow(-0.5)]
    [DataRow(1.5)]
    [DataRow(double.NaN)]
    public void Ctor_WhenEpsilonIsOutsideExclusiveUnitInterval_ShouldThrowArgumentOutOfRangeException(double epsilon)
    {
        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new CountMinSketch<int>(epsilon, 0.01);
        }, "epsilon");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> with the expected parameter
    /// name when <c>delta</c> lies outside the exclusive interval (0, 1), including the boundary values and NaN.
    /// </summary>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(1.0)]
    [DataRow(-0.5)]
    [DataRow(1.5)]
    [DataRow(double.NaN)]
    public void Ctor_WhenDeltaIsOutsideExclusiveUnitInterval_ShouldThrowArgumentOutOfRangeException(double delta)
    {
        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new CountMinSketch<int>(0.01, delta);
        }, "delta");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> when the combination of
    /// epsilon and delta requires more counter cells than the maximum supported cell count.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSizingExceedsMaximumCellCount_ShouldThrowArgumentOutOfRangeException()
    {
        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new CountMinSketch<int>(1e-8, 0.5);
        }, "epsilon");
    }

    /// <summary>
    /// Verifies that the constructor derives the width and depth from the standard count-min sizing formulas: for
    /// ε = 0.01 and δ = 0.01, w = ⌈e/0.01⌉ = 272 and d = ⌈ln(1/0.01)⌉ = 5.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSizedForOnePercentErrorAtOnePercentDelta_ShouldDeriveStandardWidthAndDepth()
    {
        var sketch = new CountMinSketch<int>(epsilon: 0.01, delta: 0.01);

        Assert.AreEqual(272, sketch.Width);
        Assert.AreEqual(5, sketch.Depth);
    }

    /// <summary>
    /// Verifies that the constructor records the sizing arguments on the <see cref="CountMinSketch{T}.Epsilon" /> and
    /// <see cref="CountMinSketch{T}.Delta" /> properties.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldExposeSizingArguments()
    {
        var sketch = new CountMinSketch<string>(epsilon: 0.02, delta: 0.05);

        Assert.AreEqual(0.02, sketch.Epsilon);
        Assert.AreEqual(0.05, sketch.Delta);
    }

    /// <summary>
    /// Verifies that a freshly constructed sketch starts with a zero <see cref="CountMinSketch{T}.TotalCount" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldStartWithZeroTotalCount()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);

        Assert.AreEqual(0L, sketch.TotalCount);
    }

    /// <summary>
    /// Verifies that the constructor guarantees at least one row even when delta is close to 1 and the sizing formula
    /// would round the depth toward zero.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDeltaIsNearOne_ShouldUseAtLeastOneRow()
    {
        var sketch = new CountMinSketch<int>(epsilon: 0.01, delta: 0.999);

        Assert.AreEqual(1, sketch.Depth);
        Assert.IsGreaterThanOrEqualTo(1, sketch.Width);
    }

    /// <summary>
    /// Verifies that the two-argument constructor uses <see cref="EqualityComparer{T}.Default" /> and the
    /// three-argument constructor falls back to it when the comparer argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsOmittedOrNull_ShouldUseDefaultComparer()
    {
        var implicitDefault = new CountMinSketch<int>(0.01, 0.01);
        var explicitNull = new CountMinSketch<int>(0.01, 0.01, null);

        Assert.AreSame(EqualityComparer<int>.Default, implicitDefault.Comparer);
        Assert.AreSame(EqualityComparer<int>.Default, explicitNull.Comparer);
    }

    /// <summary>
    /// Verifies that a custom equality comparer supplied at construction is exposed by the
    /// <see cref="CountMinSketch{T}.Comparer" /> property.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCustomComparerSupplied_ShouldExposeThatComparer()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;

        var sketch = new CountMinSketch<string>(0.01, 0.01, comparer);

        Assert.AreSame(comparer, sketch.Comparer);
    }
}
