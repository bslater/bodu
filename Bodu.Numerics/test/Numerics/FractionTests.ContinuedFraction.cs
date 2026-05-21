// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.ContinuedFraction.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that <see cref="Fraction{T}.ToContinuedFraction" /> expands a value into its expected
    /// coefficients.
    /// </summary>
    [TestMethod]
    public void ToContinuedFraction_WhenExpanded_ShouldProduceExpectedCoefficients()
    {
        CollectionAssert.AreEqual(new[] { 0, 1, 3 }, new Fraction<int>(3, 4).ToContinuedFraction());
        CollectionAssert.AreEqual(new[] { 3, 7 }, new Fraction<int>(22, 7).ToContinuedFraction());
    }

    /// <summary>
    /// Verifies that expanding then reconstructing a value reproduces the original fraction.
    /// </summary>
    [TestMethod]
    [DataRow(3, 4)]
    [DataRow(22, 7)]
    [DataRow(-5, 8)]
    [DataRow(355, 113)]
    public void ContinuedFraction_WhenRoundTripped_ShouldReproduceOriginal(int numerator, int denominator)
    {
        Fraction<int> original = new Fraction<int>(numerator, denominator);

        Fraction<int> roundTripped = Fraction<int>.FromContinuedFraction(original.ToContinuedFraction());

        Assert.AreEqual(original, roundTripped);
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.FromContinuedFraction" /> rejects an empty coefficient set.
    /// </summary>
    [TestMethod]
    public void FromContinuedFraction_WhenGivenNoCoefficients_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Fraction<int>.FromContinuedFraction();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.FromContinuedFraction" /> rejects a non-positive trailing
    /// coefficient.
    /// </summary>
    [TestMethod]
    public void FromContinuedFraction_WhenTrailingCoefficientIsNotPositive_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Fraction<int>.FromContinuedFraction(1, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.FromContinuedFraction" /> rejects a <see langword="null" /> array.
    /// </summary>
    [TestMethod]
    public void FromContinuedFraction_WhenGivenNullArray_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Fraction<int>.FromContinuedFraction(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.LimitDenominator" /> returns the known best approximation of pi.
    /// </summary>
    [TestMethod]
    public void LimitDenominator_WhenApproximatingPi_ShouldReturnKnownConvergents()
    {
        Fraction<long> pi = Fraction<long>.FromDouble(Math.PI);

        Assert.AreEqual(new Fraction<long>(22, 7), pi.LimitDenominator(10));
        Assert.AreEqual(new Fraction<long>(311, 99), pi.LimitDenominator(100));
        Assert.AreEqual(new Fraction<long>(355, 113), pi.LimitDenominator(113));
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.LimitDenominator" /> returns the value unchanged when its
    /// denominator is already within the limit.
    /// </summary>
    [TestMethod]
    public void LimitDenominator_WhenDenominatorWithinLimit_ShouldReturnSameValue()
    {
        Fraction<int> value = new Fraction<int>(1, 2);

        Assert.AreEqual(value, value.LimitDenominator(10));
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.LimitDenominator" /> rejects a limit below one.
    /// </summary>
    [TestMethod]
    public void LimitDenominator_WhenLimitIsLessThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Fraction<int>(3, 4).LimitDenominator(0);
        });
    }
}
