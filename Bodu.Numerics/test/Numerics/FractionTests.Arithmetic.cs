// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Arithmetic.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that the named arithmetic methods agree with the arithmetic operators.
    /// </summary>
    [TestMethod]
    public void NamedArithmeticMethods_WhenInvoked_ShouldMatchOperators()
    {
        Fraction<int> a = new Fraction<int>(3, 4);
        Fraction<int> b = new Fraction<int>(1, 6);

        Assert.AreEqual(a + b, Fraction<int>.Add(a, b));
        Assert.AreEqual(a - b, Fraction<int>.Subtract(a, b));
        Assert.AreEqual(a * b, Fraction<int>.Multiply(a, b));
        Assert.AreEqual(a / b, Fraction<int>.Divide(a, b));
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.Abs" /> returns the magnitude of the value.
    /// </summary>
    [TestMethod]
    public void Abs_WhenValueIsNegative_ShouldReturnMagnitude()
    {
        Assert.AreEqual(new Fraction<int>(3, 4), new Fraction<int>(-3, 4).Abs());
        Assert.AreEqual(new Fraction<int>(3, 4), new Fraction<int>(3, 4).Abs());
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.Reciprocal" /> exchanges the numerator and denominator.
    /// </summary>
    [TestMethod]
    public void Reciprocal_WhenValueIsNonZero_ShouldExchangeComponents()
    {
        Assert.AreEqual(new Fraction<int>(4, 3), new Fraction<int>(3, 4).Reciprocal());
        Assert.AreEqual(new Fraction<int>(-2, 1), new Fraction<int>(-1, 2).Reciprocal());
    }

    /// <summary>
    /// Verifies that taking the reciprocal of zero throws <see cref="DivideByZeroException" />.
    /// </summary>
    [TestMethod]
    public void Reciprocal_WhenValueIsZero_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<DivideByZeroException>(() =>
        {
            _ = Fraction<int>.Zero.Reciprocal();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.Pow" /> raises the value to integer powers.
    /// </summary>
    [TestMethod]
    [DataRow(2, 3, 2, 4, 9)]
    [DataRow(2, 3, 0, 1, 1)]
    [DataRow(2, 3, -2, 9, 4)]
    public void Pow_WhenRaisedToInteger_ShouldReturnExpectedValue(int numerator, int denominator, int exponent, int expectedNumerator, int expectedDenominator)
    {
        Fraction<int> result = new Fraction<int>(numerator, denominator).Pow(exponent);

        Assert.AreEqual(new Fraction<int>(expectedNumerator, expectedDenominator), result);
    }

    /// <summary>
    /// Verifies that raising zero to a negative power throws <see cref="DivideByZeroException" />.
    /// </summary>
    [TestMethod]
    public void Pow_WhenZeroRaisedToNegativePower_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<DivideByZeroException>(() =>
        {
            _ = Fraction<int>.Zero.Pow(-1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.Remainder" /> matches the modulus operator.
    /// </summary>
    [TestMethod]
    public void Remainder_WhenInvoked_ShouldMatchModulusOperator()
    {
        Fraction<int> value = new Fraction<int>(7, 4);

        Assert.AreEqual(value % Fraction<int>.One, value.Remainder(Fraction<int>.One));
    }

    /// <summary>
    /// Verifies that arithmetic overflowing a fixed-width component throws <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void Addition_WhenResultExceedsFixedWidthRange_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = new Fraction<int>(int.MaxValue) + new Fraction<int>(int.MaxValue);
        });
    }

    /// <summary>
    /// Verifies that the same arithmetic succeeds without overflow when backed by <see cref="BigInteger" />.
    /// </summary>
    [TestMethod]
    public void Addition_WhenBackedByBigInteger_ShouldNotOverflow()
    {
        Fraction<BigInteger> result = new Fraction<BigInteger>(int.MaxValue) + new Fraction<BigInteger>(int.MaxValue);

        Assert.AreEqual((BigInteger)int.MaxValue * 2, result.Numerator);
        Assert.AreEqual(BigInteger.One, result.Denominator);
    }

    /// <summary>
    /// Verifies that the greatest common divisor and least common multiple helpers compute known answers.
    /// </summary>
    [TestMethod]
    [DataRow(12, 18, 6, 36)]
    [DataRow(17, 5, 1, 85)]
    [DataRow(0, 9, 9, 0)]
    public void GcdAndLcm_WhenComputed_ShouldReturnKnownAnswers(int left, int right, int expectedGcd, int expectedLcm)
    {
        Assert.AreEqual(expectedGcd, Fraction<int>.GreatestCommonDivisor(left, right));
        Assert.AreEqual(expectedLcm, Fraction<int>.LeastCommonMultiple(left, right));
    }

    /// <summary>
    /// Verifies that addition produces the canonical sum for a table of known operands.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(1, 2, 1, 3, 5, 6)]
    [DataRow(1, 2, 1, 2, 1, 1)]
    [DataRow(3, 4, -1, 4, 1, 2)]
    [DataRow(-1, 3, -1, 6, -1, 2)]
    [DataRow(0, 1, 5, 7, 5, 7)]
    [DataRow(2, 3, 1, 3, 1, 1)]
    [DataRow(7, 10, 3, 10, 1, 1)]
    [DataRow(1, 6, 1, 6, 1, 3)]
    [DataRow(5, 8, -5, 8, 0, 1)]
    public void Addition_WhenGivenKnownOperands_ShouldReturnCanonicalSum(int an, int ad, int bn, int bd, int en, int ed)
    {
        Assert.AreEqual(new Fraction<int>(en, ed), new Fraction<int>(an, ad) + new Fraction<int>(bn, bd));
    }

    /// <summary>
    /// Verifies that subtraction produces the canonical difference for a table of known operands.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(5, 6, 1, 3, 1, 2)]
    [DataRow(1, 2, 1, 2, 0, 1)]
    [DataRow(1, 3, 2, 3, -1, 3)]
    [DataRow(0, 1, 1, 4, -1, 4)]
    [DataRow(-1, 2, 1, 2, -1, 1)]
    [DataRow(3, 4, -1, 4, 1, 1)]
    public void Subtraction_WhenGivenKnownOperands_ShouldReturnCanonicalDifference(int an, int ad, int bn, int bd, int en, int ed)
    {
        Assert.AreEqual(new Fraction<int>(en, ed), new Fraction<int>(an, ad) - new Fraction<int>(bn, bd));
    }

    /// <summary>
    /// Verifies that multiplication produces the canonical product for a table of known operands.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(2, 3, 3, 4, 1, 2)]
    [DataRow(1, 2, 0, 1, 0, 1)]
    [DataRow(-2, 3, 3, 5, -2, 5)]
    [DataRow(-1, 2, -1, 2, 1, 4)]
    [DataRow(5, 6, 6, 5, 1, 1)]
    [DataRow(3, 7, 14, 9, 2, 3)]
    public void Multiplication_WhenGivenKnownOperands_ShouldReturnCanonicalProduct(int an, int ad, int bn, int bd, int en, int ed)
    {
        Assert.AreEqual(new Fraction<int>(en, ed), new Fraction<int>(an, ad) * new Fraction<int>(bn, bd));
    }

    /// <summary>
    /// Verifies that division produces the canonical quotient for a table of known operands.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(1, 2, 1, 4, 2, 1)]
    [DataRow(2, 3, 4, 3, 1, 2)]
    [DataRow(-3, 4, 1, 2, -3, 2)]
    [DataRow(5, 6, 5, 6, 1, 1)]
    [DataRow(0, 1, 3, 4, 0, 1)]
    [DataRow(3, 4, -3, 8, -2, 1)]
    public void Division_WhenGivenKnownOperands_ShouldReturnCanonicalQuotient(int an, int ad, int bn, int bd, int en, int ed)
    {
        Assert.AreEqual(new Fraction<int>(en, ed), new Fraction<int>(an, ad) / new Fraction<int>(bn, bd));
    }

    /// <summary>
    /// Verifies that the remainder operation returns the signed remainder for a table of known operands.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(7, 4, 1, 1, 3, 4)]
    [DataRow(-7, 4, 1, 1, -3, 4)]
    [DataRow(5, 2, 1, 2, 0, 1)]
    [DataRow(7, 3, 2, 3, 1, 3)]
    [DataRow(1, 2, 1, 3, 1, 6)]
    [DataRow(-1, 2, 1, 3, -1, 6)]
    [DataRow(10, 3, 1, 1, 1, 3)]
    public void Remainder_WhenGivenKnownOperands_ShouldReturnSignedRemainder(int an, int ad, int bn, int bd, int en, int ed)
    {
        Assert.AreEqual(new Fraction<int>(en, ed), new Fraction<int>(an, ad).Remainder(new Fraction<int>(bn, bd)));
    }

    /// <summary>
    /// Verifies that exponentiation returns known answers across positive, zero, and negative exponents.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(2, 3, 1, 2, 3)]
    [DataRow(2, 3, 3, 8, 27)]
    [DataRow(-2, 3, 2, 4, 9)]
    [DataRow(-2, 3, 3, -8, 27)]
    [DataRow(3, 4, -1, 4, 3)]
    [DataRow(1, 1, 100, 1, 1)]
    [DataRow(2, 1, 5, 32, 1)]
    [DataRow(-1, 2, -2, 4, 1)]
    [DataRow(5, 7, 0, 1, 1)]
    public void Pow_WhenGivenKnownOperands_ShouldReturnKnownAnswers(int numerator, int denominator, int exponent, int en, int ed)
    {
        Assert.AreEqual(new Fraction<int>(en, ed), new Fraction<int>(numerator, denominator).Pow(exponent));
    }

    /// <summary>
    /// Verifies that multiplication overflowing a fixed-width component throws <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void Multiplication_WhenResultExceedsFixedWidthRange_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = new Fraction<int>(int.MaxValue, 1) * new Fraction<int>(2, 1);
        });
    }

    /// <summary>
    /// Verifies that exponentiation overflowing a fixed-width component throws <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void Pow_WhenResultExceedsFixedWidthRange_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = new Fraction<int>(2, 1).Pow(40);
        });
    }

    /// <summary>
    /// Verifies that negating or taking the magnitude of the most-negative value throws
    /// <see cref="OverflowException" /> because the result cannot be represented.
    /// </summary>
    [TestMethod]
    public void NegateAndAbs_WhenMagnitudeExceedsFixedWidthRange_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = new Fraction<int>(int.MinValue).Abs();
        });

        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = new Fraction<int>(int.MinValue).Negate();
        });
    }

    /// <summary>
    /// Verifies that the most-negative fixed-width value negates without overflow when widened to
    /// <see cref="long" />.
    /// </summary>
    [TestMethod]
    public void Negate_WhenMagnitudeFitsWiderType_ShouldNotOverflow()
    {
        Fraction<long> result = new Fraction<long>(int.MinValue).Negate();

        Assert.AreEqual(-(long)int.MinValue, result.Numerator);
    }

    /// <summary>
    /// Verifies that the greatest common divisor of a signed minimum value is evaluated correctly when the result
    /// fits a wider backing type, rather than overflowing through an unsafe negation.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_WhenArgumentIsSignedMinimum_ShouldReturnCorrectMagnitude()
    {
        Assert.AreEqual(-(long)int.MinValue, Fraction<long>.GreatestCommonDivisor(int.MinValue, 0));
    }

    /// <summary>
    /// Verifies that the greatest common divisor throws <see cref="OverflowException" /> when its magnitude cannot
    /// be represented by a fixed-width backing type.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_WhenResultExceedsFixedWidthRange_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = Fraction<int>.GreatestCommonDivisor(int.MinValue, 0);
        });
    }

    /// <summary>
    /// Verifies that raising a value to <see cref="int.MinValue" /> throws <see cref="OverflowException" />, since
    /// the magnitude of that exponent cannot be evaluated.
    /// </summary>
    [TestMethod]
    public void Pow_WhenExponentIsIntMinValue_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = new Fraction<int>(2, 3).Pow(int.MinValue);
        });
    }

    /// <summary>
    /// Verifies that Invert returns the same value as Reciprocal.
    /// </summary>
    [TestMethod]
    public void Invert_WhenValueIsNonZero_ShouldMatchReciprocal()
    {
        Fraction<int> value = new Fraction<int>(3, 4);

        Assert.AreEqual(value.Reciprocal(), value.Invert());
    }

    /// <summary>
    /// Verifies that Squared and Cubed raise the value to the second and third powers.
    /// </summary>
    [TestMethod]
    public void SquaredAndCubed_WhenInvoked_ShouldReturnExpectedPowers()
    {
        Fraction<int> value = new Fraction<int>(2, 3);

        Assert.AreEqual(new Fraction<int>(4, 9), value.Squared());
        Assert.AreEqual(new Fraction<int>(8, 27), value.Cubed());
    }

    /// <summary>
    /// Verifies that Reduce returns an already-canonical value unchanged.
    /// </summary>
    [TestMethod]
    public void Reduce_WhenInvoked_ShouldReturnEquivalentValue()
    {
        Fraction<int> value = new Fraction<int>(6, 8);

        Assert.AreEqual(value, value.Reduce());
        Assert.AreEqual(new Fraction<int>(3, 4), value.Reduce());
    }
}
