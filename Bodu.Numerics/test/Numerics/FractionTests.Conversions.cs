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

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.FromDecimal" /> produces exact values for a table of known decimals.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow("0.5", 1, 2)]
    [DataRow("0.25", 1, 4)]
    [DataRow("0.75", 3, 4)]
    [DataRow("0.1", 1, 10)]
    [DataRow("0.125", 1, 8)]
    [DataRow("0.05", 1, 20)]
    [DataRow("0.2", 1, 5)]
    [DataRow("1.0", 1, 1)]
    [DataRow("2.5", 5, 2)]
    [DataRow("-1.25", -5, 4)]
    [DataRow("3", 3, 1)]
    [DataRow("0", 0, 1)]
    [DataRow("100", 100, 1)]
    public void FromDecimal_WhenGivenKnownValues_ShouldProduceExactValue(string text, int en, int ed)
    {
        decimal input = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual(new Fraction<int>(en, ed), Fraction<int>.FromDecimal(input));
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.FromDouble" /> produces exact values for a table of binary
    /// fractions.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(0.5, 1, 2)]
    [DataRow(0.25, 1, 4)]
    [DataRow(0.75, 3, 4)]
    [DataRow(0.125, 1, 8)]
    [DataRow(0.625, 5, 8)]
    [DataRow(-0.375, -3, 8)]
    [DataRow(1.0, 1, 1)]
    [DataRow(2.0, 2, 1)]
    [DataRow(3.0, 3, 1)]
    [DataRow(0.0, 0, 1)]
    [DataRow(-2.5, -5, 2)]
    public void FromDouble_WhenGivenKnownValues_ShouldProduceExactValue(double input, int en, int ed)
    {
        Assert.AreEqual(new Fraction<int>(en, ed), Fraction<int>.FromDouble(input));
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.FromDouble" /> rejects every non-finite value.
    /// </summary>
    [TestMethod]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity)]
    public void FromDouble_WhenGivenNonFiniteValue_ShouldThrowArgumentExceptionForEachCase(double input)
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Fraction<int>.FromDouble(input);
        });
    }

    /// <summary>
    /// Verifies that conversion to <see cref="double" /> returns known answers.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(1, 2, 0.5)]
    [DataRow(3, 4, 0.75)]
    [DataRow(-1, 4, -0.25)]
    [DataRow(5, 1, 5.0)]
    [DataRow(0, 1, 0.0)]
    [DataRow(-7, 2, -3.5)]
    public void ToDouble_WhenConverted_ShouldReturnKnownAnswers(int numerator, int denominator, double expected)
    {
        Assert.AreEqual(expected, new Fraction<int>(numerator, denominator).ToDouble());
    }

    /// <summary>
    /// Verifies that an exact binary fraction round-trips through <see cref="double" /> conversion.
    /// </summary>
    [TestMethod]
    [DataRow(0.5)]
    [DataRow(0.25)]
    [DataRow(-0.375)]
    [DataRow(123.0)]
    [DataRow(0.0)]
    public void FromDouble_WhenRoundTrippedThroughToDouble_ShouldReproduceValue(double input)
    {
        Assert.AreEqual(input, Fraction<long>.FromDouble(input).ToDouble());
    }

    /// <summary>
    /// Verifies that ToSingle, ToBigInteger, and ToInteger convert a value to each primitive type.
    /// </summary>
    [TestMethod]
    public void ToPrimitive_WhenConverting_ShouldReturnExpectedValues()
    {
        Fraction<int> value = new Fraction<int>(7, 2);

        Assert.AreEqual(3.5f, value.ToSingle());
        Assert.AreEqual((BigInteger)3, value.ToBigInteger());
        Assert.AreEqual(3, value.ToInteger());
    }

    /// <summary>
    /// Verifies that the explicit float conversion operator returns the single-precision approximation.
    /// </summary>
    [TestMethod]
    public void ExplicitFloatOperator_WhenConverting_ShouldReturnApproximation()
    {
        Assert.AreEqual(0.25f, (float)new Fraction<int>(1, 4));
    }

    /// <summary>
    /// Verifies that the non-failing Try-conversions report success and return the converted value.
    /// </summary>
    [TestMethod]
    public void TryToPrimitive_WhenConverting_ShouldAlwaysSucceed()
    {
        Fraction<int> value = new Fraction<int>(7, 2);

        Assert.IsTrue(value.TryToDouble(out double asDouble));
        Assert.AreEqual(3.5, asDouble);

        Assert.IsTrue(value.TryToSingle(out float asSingle));
        Assert.AreEqual(3.5f, asSingle);

        Assert.IsTrue(value.TryToBigInteger(out BigInteger asBig));
        Assert.AreEqual((BigInteger)3, asBig);

        Assert.IsTrue(value.TryToInteger(out int asInt));
        Assert.AreEqual(3, asInt);

        Assert.IsTrue(value.TryToDecimal(out decimal asDecimal));
        Assert.AreEqual(3.5m, asDecimal);
    }

    /// <summary>
    /// Verifies that TryToDecimal reports failure when the value exceeds the decimal range.
    /// </summary>
    [TestMethod]
    public void TryToDecimal_WhenValueExceedsDecimalRange_ShouldReturnFalse()
    {
        Fraction<BigInteger> value = new Fraction<BigInteger>(BigInteger.Pow(10, 40));

        Assert.IsFalse(value.TryToDecimal(out decimal result));
        Assert.AreEqual(0m, result);
    }
}
