// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalTests.Arithmetic.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class BigDecimalTests
{
    /// <summary>
    /// Verifies that addition aligns scales and is exact — including the classic <c>0.1 + 0.2 == 0.3</c>.
    /// </summary>
    [TestMethod]
    public void Add_WhenScalesDiffer_ShouldAlignAndSumExactly()
    {
        Assert.AreEqual(BD(3, 1), BD(1, 1) + BD(2, 1));                 // 0.1 + 0.2 = 0.3
        Assert.AreEqual("0.3", (BD(1, 1) + BD(2, 1)).ToString());
        Assert.AreEqual(BD(105, 1), BD(11, 0) + BD(-5, 1));           // 11 + (-0.5) = 10.5
        Assert.AreEqual("10.5", (BD(11, 0) + BD(-5, 1)).ToString());
    }

    /// <summary>
    /// Verifies that subtraction is exact and canonicalizes the result.
    /// </summary>
    [TestMethod]
    public void Subtract_WhenComputed_ShouldBeExact()
    {
        Assert.AreEqual(BigDecimal.Zero, BD(3, 1) - BD(3, 1));
        Assert.AreEqual("0.05", (BD(15, 2) - BD(1, 1)).ToString());    // 0.15 - 0.10 = 0.05
    }

    /// <summary>
    /// Verifies that multiplication adds scales and is exact.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenComputed_ShouldAddScalesExactly()
    {
        Assert.AreEqual("0.06", (BD(2, 1) * BD(3, 1)).ToString());     // 0.2 × 0.3 = 0.06
        Assert.AreEqual("6", (BD(2, 0) * BD(3, 0)).ToString());
    }

    /// <summary>
    /// Verifies that an exact division trims to its shortest canonical form.
    /// </summary>
    [TestMethod]
    public void Divide_WhenExact_ShouldTrimToCanonicalForm()
    {
        Assert.AreEqual("0.5", (BigDecimal.One / BD(2, 0)).ToString());
        Assert.AreEqual("0.25", (BigDecimal.One / BD(4, 0)).ToString());
        Assert.AreEqual("2.5", (BD(5, 0) / BD(2, 0)).ToString());
    }

    /// <summary>
    /// Verifies that a non-terminating division rounds to the default scale using half-even.
    /// </summary>
    [TestMethod]
    public void Divide_WhenNonTerminating_ShouldRoundToDefaultScaleHalfEven()
    {
        BigDecimal third = BigDecimal.One / BD(3, 0);

        Assert.AreEqual(BigDecimal.DefaultDivisionScale, third.Scale);
        Assert.IsTrue(third.ToString().StartsWith("0.3333", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that division to an explicit scale honours the requested rounding mode.
    /// </summary>
    [TestMethod]
    public void Divide_WhenGivenScaleAndMode_ShouldRoundAccordingly()
    {
        Assert.AreEqual("0.33", BigDecimal.Divide(BigDecimal.One, BD(3, 0), 2, MidpointRounding.ToEven).ToString());
        Assert.AreEqual("0.67", BigDecimal.Divide(BD(2, 0), BD(3, 0), 2, MidpointRounding.ToEven).ToString());
        Assert.AreEqual("0.12", BigDecimal.Divide(BD(125, 3), BigDecimal.One, 2, MidpointRounding.ToEven).ToString()); // 0.125 -> 0.12 (half to even)
    }

    /// <summary>
    /// Verifies that dividing by zero throws <see cref="DivideByZeroException" />.
    /// </summary>
    [TestMethod]
    public void Divide_WhenDivisorIsZero_ShouldThrowDivideByZeroException()
    {
        Assert.ThrowsExactly<DivideByZeroException>(() =>
        {
            _ = BigDecimal.One / BigDecimal.Zero;
        });
    }

    /// <summary>
    /// Verifies that the remainder is exact and its sign matches the dividend.
    /// </summary>
    [TestMethod]
    public void Remainder_WhenComputed_ShouldBeExact()
    {
        Assert.AreEqual("1", (BD(10, 0) % BD(3, 0)).ToString());
        Assert.AreEqual("0.1", (BD(7, 1) % BD(2, 1)).ToString());       // 0.7 % 0.2 = 0.1
        Assert.AreEqual("-1", (BD(-10, 0) % BD(3, 0)).ToString());
    }

    /// <summary>
    /// Verifies that raising to an integer power is exact for non-negative exponents and divides for negative ones.
    /// </summary>
    [TestMethod]
    public void Pow_WhenComputed_ShouldMatchExpected()
    {
        Assert.AreEqual("0.008", BigDecimal.Pow(BD(2, 1), 3).ToString());   // 0.2^3 = 0.008
        Assert.AreEqual("1", BigDecimal.Pow(BD(1234, 2), 0).ToString());
        Assert.AreEqual("0.25", BigDecimal.Pow(BD(2, 0), -2).ToString());   // 2^-2 = 0.25
    }

    /// <summary>
    /// Verifies that negation and absolute value behave as expected.
    /// </summary>
    [TestMethod]
    public void NegateAndAbs_WhenComputed_ShouldMatchExpected()
    {
        Assert.AreEqual("-3.14", BigDecimal.Negate(BD(314, 2)).ToString());
        Assert.AreEqual("3.14", BigDecimal.Abs(BD(-314, 2)).ToString());
        Assert.AreEqual(BigDecimal.Zero, BigDecimal.Negate(BigDecimal.Zero));
    }

    /// <summary>
    /// Verifies that <see cref="BigDecimal.Pow(BigDecimal, int)" /> throws <see cref="OverflowException" /> when the
    /// resulting scale (scale × exponent) exceeds <see cref="int" /> range, rather than silently wrapping to a wrong
    /// result. The base is a power of ten (mantissa 1) so <see cref="System.Numerics.BigInteger" /> exponentiation is
    /// cheap and the scale is the only large quantity.
    /// </summary>
    [TestMethod]
    public void Pow_WhenScaleTimesExponentOverflowsInt_ShouldThrowOverflowException()
    {
        BigDecimal value = BD(1, 2); // 0.01 — mantissa 1, scale 2

        Assert.ThrowsExactly<OverflowException>(() => _ = BigDecimal.Pow(value, int.MaxValue));
    }
}
