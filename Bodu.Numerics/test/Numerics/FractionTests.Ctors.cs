// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that the constructor reduces a non-canonical fraction to its lowest terms.
    /// </summary>
    [TestMethod]
    [DataRow(2, 4, 1, 2)]
    [DataRow(6, 8, 3, 4)]
    [DataRow(100, 10, 10, 1)]
    [DataRow(9, 6, 3, 2)]
    public void Constructor_WhenGivenReducibleComponents_ShouldNormalizeToLowestTerms(int numerator, int denominator, int expectedNumerator, int expectedDenominator)
    {
        var value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(expectedNumerator, value.Numerator);
        Assert.AreEqual(expectedDenominator, value.Denominator);
    }

    /// <summary>
    /// Verifies that a negative denominator is canonicalized so the numerator carries the sign.
    /// </summary>
    [TestMethod]
    [DataRow(1, -2, -1, 2)]
    [DataRow(-1, -2, 1, 2)]
    [DataRow(-3, 4, -3, 4)]
    public void Constructor_WhenGivenNegativeDenominator_ShouldMoveSignToNumerator(int numerator, int denominator, int expectedNumerator, int expectedDenominator)
    {
        var value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(expectedNumerator, value.Numerator);
        Assert.AreEqual(expectedDenominator, value.Denominator);
    }

    /// <summary>
    /// Verifies that a zero numerator is reduced to the canonical zero <c>0/1</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenNumeratorIsZero_ShouldProduceCanonicalZero()
    {
        var value = new Fraction<int>(0, 5);

        Assert.AreEqual(0, value.Numerator);
        Assert.AreEqual(1, value.Denominator);
        Assert.IsTrue(value.IsZero);
    }

    /// <summary>
    /// Verifies that constructing a fraction with a zero denominator throws
    /// <see cref="DivideByZeroException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDenominatorIsZero_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<DivideByZeroException>(() =>
        {
            _ = new Fraction<int>(1, 0);
        });
    }

    /// <summary>
    /// Verifies that the single-argument constructor produces a whole-number fraction.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenGivenSingleInteger_ShouldProduceWholeNumber()
    {
        var value = new Fraction<int>(7);

        Assert.AreEqual(7, value.Numerator);
        Assert.AreEqual(1, value.Denominator);
        Assert.IsTrue(value.IsInteger);
    }

    /// <summary>
    /// Verifies that a default-initialized fraction behaves as the rational value zero.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDefaultInitialized_ShouldBehaveAsZero()
    {
        Fraction<int> value = default;

        Assert.AreEqual(0, value.Numerator);
        Assert.AreEqual(1, value.Denominator);
        Assert.IsTrue(value.IsZero);
        Assert.AreEqual(Fraction<int>.Zero, value);
    }

    /// <summary>
    /// Verifies that <see cref="BigInteger" /> components support construction beyond fixed-width ranges.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenBackedByBigInteger_ShouldNormalizeLargeComponents()
    {
        var large = BigInteger.Pow(10, 40);
        var value = new Fraction<BigInteger>(large * 2, large * 4);

        Assert.AreEqual(BigInteger.One, value.Numerator);
        Assert.AreEqual((BigInteger)2, value.Denominator);
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.Deconstruct" /> yields the canonical components.
    /// </summary>
    [TestMethod]
    public void Deconstruct_WhenCalled_ShouldYieldCanonicalComponents()
    {
        (var numerator, var denominator) = new Fraction<int>(6, 8);

        Assert.AreEqual(3, numerator);
        Assert.AreEqual(4, denominator);
    }

    /// <summary>
    /// Verifies that the <c>Sign</c> property reflects the sign of the rational value.
    /// </summary>
    [TestMethod]
    [DataRow(-3, 4, -1)]
    [DataRow(0, 4, 0)]
    [DataRow(3, 4, 1)]
    public void Sign_WhenInspected_ShouldReflectTheValueSign(int numerator, int denominator, int expectedSign)
    {
        var value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(expectedSign, value.Sign);
    }

    /// <summary>
    /// Verifies that construction reduces a wide table of components to their canonical form.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(2, 4, 1, 2)]
    [DataRow(6, 8, 3, 4)]
    [DataRow(-2, 4, -1, 2)]
    [DataRow(2, -4, -1, 2)]
    [DataRow(-2, -4, 1, 2)]
    [DataRow(0, 5, 0, 1)]
    [DataRow(0, -5, 0, 1)]
    [DataRow(10, 5, 2, 1)]
    [DataRow(100, 10, 10, 1)]
    [DataRow(7, 7, 1, 1)]
    [DataRow(-7, 7, -1, 1)]
    [DataRow(15, 25, 3, 5)]
    [DataRow(-15, 25, -3, 5)]
    [DataRow(9, -6, -3, 2)]
    [DataRow(12, 18, 2, 3)]
    [DataRow(-12, -18, 2, 3)]
    [DataRow(17, 19, 17, 19)]
    [DataRow(1000000, 1, 1000000, 1)]
    public void Constructor_WhenGivenComponents_ShouldNormalizeToKnownAnswer(int numerator, int denominator, int expectedNumerator, int expectedDenominator)
    {
        var value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(expectedNumerator, value.Numerator);
        Assert.AreEqual(expectedDenominator, value.Denominator);
    }

    /// <summary>
    /// Verifies that the most-negative and most-positive fixed-width values construct without overflow.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenGivenFixedWidthExtremes_ShouldConstructWithoutOverflow()
    {
        Assert.AreEqual(int.MinValue, new Fraction<int>(int.MinValue).Numerator);
        Assert.AreEqual(int.MaxValue, new Fraction<int>(int.MaxValue).Numerator);
        Assert.AreEqual(1, new Fraction<int>(int.MaxValue).Denominator);
    }

    /// <summary>
    /// Verifies that Create produces the same canonical value as the constructor.
    /// </summary>
    [TestMethod]
    public void Create_WhenGivenComponents_ShouldReturnCanonicalValue() => Assert.AreEqual(new Fraction<int>(1, 2), Fraction<int>.Create(2, 4));

    /// <summary>
    /// Verifies that Create throws DivideByZeroException for a zero denominator.
    /// </summary>
    [TestMethod]
    public void Create_WhenDenominatorIsZero_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<DivideByZeroException>(() =>
        {
            _ = Fraction<int>.Create(1, 0);
        });
    }

    /// <summary>
    /// Verifies that TryCreate succeeds for valid components and fails for a zero denominator.
    /// </summary>
    [TestMethod]
    public void TryCreate_WhenComponentsAreValidOrInvalid_ShouldReportSuccess()
    {
        Assert.IsTrue(Fraction<int>.TryCreate(2, 4, out Fraction<int> created));
        Assert.AreEqual(new Fraction<int>(1, 2), created);

        Assert.IsFalse(Fraction<int>.TryCreate(1, 0, out Fraction<int> failed));
        Assert.AreEqual(Fraction<int>.Zero, failed);
    }

    /// <summary>
    /// Verifies that FromBigInteger reduces large components to a canonical value.
    /// </summary>
    [TestMethod]
    public void FromBigInteger_WhenGivenComponents_ShouldReturnReducedValue() => Assert.AreEqual(new Fraction<int>(1, 2), Fraction<int>.FromBigInteger(50, 100));

    /// <summary>
    /// Verifies that FromBigInteger throws DivideByZeroException for a zero denominator.
    /// </summary>
    [TestMethod]
    public void FromBigInteger_WhenDenominatorIsZero_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<DivideByZeroException>(() =>
        {
            _ = Fraction<int>.FromBigInteger(BigInteger.One, BigInteger.Zero);
        });
    }

    /// <summary>
    /// Verifies that TryFromBigInteger reports failure when the result exceeds the backing type range.
    /// </summary>
    [TestMethod]
    public void TryFromBigInteger_WhenResultExceedsFixedWidthRange_ShouldReturnFalse() => Assert.IsFalse(Fraction<int>.TryFromBigInteger(BigInteger.Pow(10, 20), BigInteger.One, out _));

    /// <summary>
    /// Verifies that TryFromDecimal converts an exact decimal and reports overflow as failure.
    /// </summary>
    [TestMethod]
    public void TryFromDecimal_WhenValueIsRepresentableOrNot_ShouldReportSuccess()
    {
        Assert.IsTrue(Fraction<int>.TryFromDecimal(0.25m, out Fraction<int> created));
        Assert.AreEqual(new Fraction<int>(1, 4), created);

        Assert.IsFalse(Fraction<int>.TryFromDecimal(decimal.MaxValue, out _));
    }

    /// <summary>
    /// Verifies that TryFromDouble converts a finite value and rejects non-finite input.
    /// </summary>
    [TestMethod]
    public void TryFromDouble_WhenValueIsFiniteOrNot_ShouldReportSuccess()
    {
        Assert.IsTrue(Fraction<int>.TryFromDouble(0.5, out Fraction<int> created));
        Assert.AreEqual(new Fraction<int>(1, 2), created);

        Assert.IsFalse(Fraction<int>.TryFromDouble(double.NaN, out _));
        Assert.IsFalse(Fraction<int>.TryFromDouble(double.PositiveInfinity, out _));
    }
}
