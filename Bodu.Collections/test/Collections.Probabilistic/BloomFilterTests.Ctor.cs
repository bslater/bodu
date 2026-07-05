// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BloomFilterTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class BloomFilterTests
{
    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> with the expected parameter
    /// name when <c>expectedItems</c> is zero or negative.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void Ctor_WhenExpectedItemsIsZeroOrNegative_ShouldThrowArgumentOutOfRangeException(int expectedItems)
    {
        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new BloomFilter<int>(expectedItems, 0.01);
        }, "expectedItems");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> with the expected parameter
    /// name when <c>falsePositiveRate</c> lies outside the exclusive interval (0, 1), including the boundary values
    /// and NaN.
    /// </summary>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(1.0)]
    [DataRow(-0.5)]
    [DataRow(1.5)]
    [DataRow(double.NaN)]
    public void Ctor_WhenFalsePositiveRateIsOutsideExclusiveUnitInterval_ShouldThrowArgumentOutOfRangeException(double falsePositiveRate)
    {
        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new BloomFilter<int>(1000, falsePositiveRate);
        }, "falsePositiveRate");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> when the combination of
    /// expected items and false-positive rate requires more bits than the maximum supported bit count.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSizingExceedsMaximumBitCount_ShouldThrowArgumentOutOfRangeException()
    {
        ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new BloomFilter<int>(int.MaxValue, 1e-9);
        }, "expectedItems");
    }

    /// <summary>
    /// Verifies that the constructor derives the bit count and hash count from the standard Bloom sizing formulas:
    /// for n = 1000 and p = 0.01, m = ⌈−1000·ln(0.01)/ln(2)²⌉ = 9586 and k = round(9586/1000·ln 2) = 7.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSizedForThousandItemsAtOnePercent_ShouldDeriveStandardBitAndHashCounts()
    {
        var filter = new BloomFilter<int>(expectedItems: 1000, falsePositiveRate: 0.01);

        Assert.AreEqual(9586, filter.BitCount);
        Assert.AreEqual(7, filter.HashCount);
    }

    /// <summary>
    /// Verifies that the constructor records the sizing arguments on the <see cref="BloomFilter{T}.ExpectedItems" />
    /// and <see cref="BloomFilter{T}.DesignFalsePositiveRate" /> properties.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldExposeSizingArguments()
    {
        var filter = new BloomFilter<string>(expectedItems: 250, falsePositiveRate: 0.05);

        Assert.AreEqual(250, filter.ExpectedItems);
        Assert.AreEqual(0.05, filter.DesignFalsePositiveRate);
    }

    /// <summary>
    /// Verifies that the constructor guarantees at least one probe per element even when the sizing formulas would
    /// round the hash count toward zero (very high false-positive rates).
    /// </summary>
    [TestMethod]
    public void Ctor_WhenFalsePositiveRateIsNearOne_ShouldUseAtLeastOneHash()
    {
        var filter = new BloomFilter<int>(expectedItems: 1000, falsePositiveRate: 0.999);

        Assert.IsGreaterThanOrEqualTo(1, filter.HashCount);
        Assert.IsGreaterThanOrEqualTo(1, filter.BitCount);
    }

    /// <summary>
    /// Verifies that the two-argument constructor uses <see cref="EqualityComparer{T}.Default" /> and the
    /// three-argument constructor falls back to it when the comparer argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsOmittedOrNull_ShouldUseDefaultComparer()
    {
        var implicitDefault = new BloomFilter<int>(100, 0.01);
        var explicitNull = new BloomFilter<int>(100, 0.01, null);

        Assert.AreSame(EqualityComparer<int>.Default, implicitDefault.Comparer);
        Assert.AreSame(EqualityComparer<int>.Default, explicitNull.Comparer);
    }

    /// <summary>
    /// Verifies that a custom equality comparer supplied at construction is exposed by the
    /// <see cref="BloomFilter{T}.Comparer" /> property.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCustomComparerSupplied_ShouldExposeThatComparer()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;

        var filter = new BloomFilter<string>(100, 0.01, comparer);

        Assert.AreSame(comparer, filter.Comparer);
    }
}
