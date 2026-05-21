// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Conversions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that an integer of the backing type implicitly converts to a whole-number fraction.
    /// </summary>
    [TestMethod]
    public void ImplicitConversion_WhenFromBackingInteger_ShouldProduceWholeNumber()
    {
        Fraction<int> value = 5;

        Assert.AreEqual(new Fraction<int>(5, 1), value);
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.FromDecimal" /> produces the exact rational value.
    /// </summary>
    [TestMethod]
    [DataRow("0.75", 3, 4)]
    [DataRow("0.25", 1, 4)]
    [DataRow("-0.5", -1, 2)]
    [DataRow("2.5", 5, 2)]
    public void FromDecimal_WhenGivenDecimal_ShouldProduceExactValue(string text, int expectedNumerator, int expectedDenominator)
    {
        decimal input = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        Fraction<int> value = Fraction<int>.FromDecimal(input);

        Assert.AreEqual(new Fraction<int>(expectedNumerator, expectedDenominator), value);
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.FromDouble" /> produces the exact rational value for binary fractions.
    /// </summary>
    [TestMethod]
    public void FromDouble_WhenGivenBinaryFraction_ShouldProduceExactValue()
    {
        Assert.AreEqual(new Fraction<int>(1, 2), Fraction<int>.FromDouble(0.5));
        Assert.AreEqual(new Fraction<int>(-3, 8), Fraction<int>.FromDouble(-0.375));
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.FromDouble" /> rejects non-finite values.
    /// </summary>
    [TestMethod]
    public void FromDouble_WhenGivenNonFiniteValue_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Fraction<int>.FromDouble(double.NaN);
        });
    }

    /// <summary>
    /// Verifies that conversion to <see cref="double" /> and <see cref="decimal" /> yields the expected value.
    /// </summary>
    [TestMethod]
    public void ToDoubleAndToDecimal_WhenConverted_ShouldYieldExpectedValue()
    {
        Fraction<int> value = new Fraction<int>(3, 4);

        Assert.AreEqual(0.75, value.ToDouble());
        Assert.AreEqual(0.75m, value.ToDecimal());
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.As{TOther}" /> retypes the fraction onto a different backing type.
    /// </summary>
    [TestMethod]
    public void As_WhenRetypingBackingComponent_ShouldPreserveValue()
    {
        Fraction<long> value = new Fraction<int>(3, 4).As<long>();

        Assert.AreEqual(3L, value.Numerator);
        Assert.AreEqual(4L, value.Denominator);
    }

    /// <summary>
    /// Verifies that retyping onto a backing type too small to hold the value throws
    /// <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void As_WhenComponentDoesNotFitTargetType_ShouldThrowOverflowException()
    {
        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = new Fraction<int>(1, 1000).As<byte>();
        });
    }

    /// <summary>
    /// Verifies that the explicit decimal conversion operator matches <see cref="Fraction{T}.FromDecimal" />.
    /// </summary>
    [TestMethod]
    public void ExplicitDecimalOperator_WhenConverting_ShouldMatchFromDecimal()
    {
        Fraction<BigInteger> value = (Fraction<BigInteger>)1.5m;

        Assert.AreEqual(new Fraction<BigInteger>(3, 2), value);
    }
}
