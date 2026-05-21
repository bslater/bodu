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
    public void Reciprocal_WhenValueIsZero_ShouldThrowDivideByZeroException()
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
    public void Pow_WhenZeroRaisedToNegativePower_ShouldThrowDivideByZeroException()
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
    public void Addition_WhenResultExceedsFixedWidthRange_ShouldThrowOverflowException()
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
}
