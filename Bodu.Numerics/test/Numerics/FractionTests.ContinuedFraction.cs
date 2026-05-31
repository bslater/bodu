// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.ContinuedFraction.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

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
        var original = new Fraction<int>(numerator, denominator);

        var roundTripped = Fraction<int>.FromContinuedFraction(original.ToContinuedFraction());

        Assert.AreEqual(original, roundTripped);
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.FromContinuedFraction" /> rejects an empty coefficient set.
    /// </summary>
    [TestMethod]
    public void FromContinuedFraction_WhenGivenNoCoefficients_ShouldThrowExactly()
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
    public void FromContinuedFraction_WhenTrailingCoefficientIsNotPositive_ShouldThrowExactly()
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
    public void FromContinuedFraction_WhenGivenNullArray_ShouldThrowExactly()
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
        var pi = Fraction<long>.FromDouble(Math.PI);

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
        var value = new Fraction<int>(1, 2);

        Assert.AreEqual(value, value.LimitDenominator(10));
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.LimitDenominator" /> rejects a limit below one.
    /// </summary>
    [TestMethod]
    public void LimitDenominator_WhenLimitIsLessThanOne_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Fraction<int>(3, 4).LimitDenominator(0);
        });
    }

    /// <summary>
    /// Verifies that continued-fraction expansion yields known coefficient sequences.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(3, 4, "0,1,3")]
    [DataRow(22, 7, "3,7")]
    [DataRow(1, 1, "1")]
    [DataRow(0, 1, "0")]
    [DataRow(7, 4, "1,1,3")]
    [DataRow(-5, 8, "-1,2,1,2")]
    [DataRow(355, 113, "3,7,16")]
    [DataRow(43, 19, "2,3,1,4")]
    [DataRow(5, 1, "5")]
    [DataRow(-3, 1, "-3")]
    public void ToContinuedFraction_WhenExpanded_ShouldYieldKnownCoefficients(int numerator, int denominator, string expected)
    {
        var coefficients = new Fraction<int>(numerator, denominator).ToContinuedFraction();

        CollectionAssert.AreEqual(ParseCoefficients(expected), coefficients);
    }

    /// <summary>
    /// Verifies that reconstructing a fraction from known coefficients yields the expected value.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow("0,1,3", 3, 4)]
    [DataRow("3,7", 22, 7)]
    [DataRow("1,1,3", 7, 4)]
    [DataRow("-1,2,1,2", -5, 8)]
    [DataRow("-2,3", -5, 3)]
    [DataRow("3,7,16", 355, 113)]
    [DataRow("5", 5, 1)]
    public void FromContinuedFraction_WhenGivenKnownCoefficients_ShouldReturnExpectedValue(string coefficients, int en, int ed)
    {
        var value = Fraction<int>.FromContinuedFraction(ParseCoefficients(coefficients));

        Assert.AreEqual(new Fraction<int>(en, ed), value);
    }

    /// <summary>
    /// Verifies that a single-coefficient continued fraction reconstructs a whole number.
    /// </summary>
    [TestMethod]
    public void FromContinuedFraction_WhenGivenSingleCoefficient_ShouldReturnWholeNumber() => Assert.AreEqual(new Fraction<int>(-3, 1), Fraction<int>.FromContinuedFraction(-3));

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.LimitDenominator" /> returns an exact rational unchanged when its
    /// denominator does not exceed the limit.
    /// </summary>
    [TestMethod]
    [DataRow(3, 7, 10)]
    [DataRow(355, 113, 200)]
    [DataRow(-5, 8, 8)]
    public void LimitDenominator_WhenDenominatorDoesNotExceedLimit_ShouldReturnInputValue(int numerator, int denominator, int limit)
    {
        var value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(value, value.LimitDenominator(limit));
    }

    /// <summary>
    /// Splits a comma-separated coefficient string into an array of integers.
    /// </summary>
    /// <param name="text">The comma-separated coefficients.</param>
    /// <returns>The parsed coefficient array.</returns>
    private static int[] ParseCoefficients(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return text.Split(',').Select(int.Parse).ToArray();
    }

    /// <summary>
    /// Verifies that Approximate finds a best fraction even when the exact value would overflow the backing type.
    /// </summary>
    [TestMethod]
    public void Approximate_WhenExactValueOverflowsBackingType_ShouldStillFindBestApproximation() => Assert.AreEqual(new Fraction<int>(311, 99), Fraction<int>.Approximate(Math.PI, 100));

    /// <summary>
    /// Verifies that Approximate produces a best rational approximation of a decimal value.
    /// </summary>
    [TestMethod]
    public void Approximate_WhenGivenDecimal_ShouldReturnBestApproximation() => Assert.AreEqual(new Fraction<int>(1, 3), Fraction<int>.Approximate(0.3333m, 10));

    /// <summary>
    /// Verifies that Approximate parses a numeric string and approximates the parsed value.
    /// </summary>
    [TestMethod]
    public void Approximate_WhenGivenString_ShouldParseAndApproximate() => Assert.AreEqual(new Fraction<int>(1, 2), Fraction<int>.Approximate("0.5", 10, CultureInfo.InvariantCulture));

    /// <summary>
    /// Verifies that Approximate rejects a denominator limit below one.
    /// </summary>
    [TestMethod]
    public void Approximate_WhenDenominatorLimitIsZero_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Fraction<int>.Approximate(0.5, 0);
        });
    }
}
