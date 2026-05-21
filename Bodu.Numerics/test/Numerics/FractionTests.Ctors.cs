// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
        Fraction<int> value = new Fraction<int>(numerator, denominator);

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
        Fraction<int> value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(expectedNumerator, value.Numerator);
        Assert.AreEqual(expectedDenominator, value.Denominator);
    }

    /// <summary>
    /// Verifies that a zero numerator is reduced to the canonical zero <c>0/1</c>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenNumeratorIsZero_ShouldProduceCanonicalZero()
    {
        Fraction<int> value = new Fraction<int>(0, 5);

        Assert.AreEqual(0, value.Numerator);
        Assert.AreEqual(1, value.Denominator);
        Assert.IsTrue(value.IsZero);
    }

    /// <summary>
    /// Verifies that constructing a fraction with a zero denominator throws
    /// <see cref="DivideByZeroException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDenominatorIsZero_ShouldThrowDivideByZeroException()
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
        Fraction<int> value = new Fraction<int>(7);

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
        BigInteger large = BigInteger.Pow(10, 40);
        Fraction<BigInteger> value = new Fraction<BigInteger>(large * 2, large * 4);

        Assert.AreEqual(BigInteger.One, value.Numerator);
        Assert.AreEqual((BigInteger)2, value.Denominator);
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.Deconstruct" /> yields the canonical components.
    /// </summary>
    [TestMethod]
    public void Deconstruct_WhenCalled_ShouldYieldCanonicalComponents()
    {
        (int numerator, int denominator) = new Fraction<int>(6, 8);

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
        Fraction<int> value = new Fraction<int>(numerator, denominator);

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
        Fraction<int> value = new Fraction<int>(numerator, denominator);

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
}
