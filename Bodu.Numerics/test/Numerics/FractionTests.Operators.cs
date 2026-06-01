// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that the arithmetic operators produce the expected canonical results.
    /// </summary>
    [TestMethod]
    public void Operators_WhenCombiningFractions_ShouldProduceCanonicalResults()
    {
        var a = new Fraction<int>(2, 3);
        var b = new Fraction<int>(1, 6);

        Assert.AreEqual(new Fraction<int>(5, 6), a + b);
        Assert.AreEqual(new Fraction<int>(1, 2), a - b);
        Assert.AreEqual(new Fraction<int>(1, 9), a * b);
        Assert.AreEqual(new Fraction<int>(4, 1), a / b);
    }

    /// <summary>
    /// Verifies that dividing a fraction by zero throws <see cref="DivideByZeroException" />.
    /// </summary>
    [TestMethod]
    public void DivisionOperator_WhenDividingByZero_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<DivideByZeroException>(() =>
        {
            _ = new Fraction<int>(1, 2) / Fraction<int>.Zero;
        });
    }

    /// <summary>
    /// Verifies that the unary negation operator reverses the sign of the value.
    /// </summary>
    [TestMethod]
    public void UnaryNegationOperator_WhenApplied_ShouldReverseSign()
    {
        Assert.AreEqual(new Fraction<int>(-3, 4), -new Fraction<int>(3, 4));
        Assert.AreEqual(new Fraction<int>(3, 4), -new Fraction<int>(-3, 4));
    }

    /// <summary>
    /// Verifies that the increment and decrement operators change the value by one.
    /// </summary>
    [TestMethod]
    public void IncrementAndDecrementOperators_WhenApplied_ShouldChangeValueByOne()
    {
        var value = new Fraction<int>(1, 2);
        value++;
        Assert.AreEqual(new Fraction<int>(3, 2), value);

        value--;
        Assert.AreEqual(new Fraction<int>(1, 2), value);
    }

    /// <summary>
    /// Verifies that the modulus operator returns a remainder with the sign of the dividend.
    /// </summary>
    [TestMethod]
    public void ModulusOperator_WhenApplied_ShouldReturnSignedRemainder()
    {
        Assert.AreEqual(new Fraction<int>(3, 4), new Fraction<int>(7, 4) % Fraction<int>.One);
        Assert.AreEqual(new Fraction<int>(-3, 4), new Fraction<int>(-7, 4) % Fraction<int>.One);
    }

    /// <summary>
    /// Verifies that the comparison operators order rational values correctly.
    /// </summary>
    [TestMethod]
    public void ComparisonOperators_WhenComparingValues_ShouldOrderCorrectly()
    {
        var small = new Fraction<int>(1, 3);
        var large = new Fraction<int>(1, 2);

        Assert.IsTrue(small < large);
        Assert.IsTrue(small <= large);
        Assert.IsTrue(large > small);
        Assert.IsTrue(large >= small);
        Assert.IsTrue(small != large);
        Assert.IsTrue(small == new Fraction<int>(2, 6));
    }

    /// <summary>
    /// Verifies that comparison yields the expected relative order for a table of known operand pairs.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(1, 3, 1, 2, -1)]
    [DataRow(1, 2, 1, 3, 1)]
    [DataRow(2, 4, 1, 2, 0)]
    [DataRow(-1, 2, 1, 2, -1)]
    [DataRow(-1, 2, -1, 3, -1)]
    [DataRow(0, 1, 0, 1, 0)]
    [DataRow(3, 4, 3, 4, 0)]
    [DataRow(-3, 4, -3, 4, 0)]
    [DataRow(100, 1, 99, 1, 1)]
    [DataRow(1, 1000000, 0, 1, 1)]
    [DataRow(-1, 1000000, 0, 1, -1)]
    public void Comparison_WhenGivenKnownOperands_ShouldYieldExpectedOrder(int an, int ad, int bn, int bd, int expectedSign)
    {
        var a = new Fraction<int>(an, ad);
        var b = new Fraction<int>(bn, bd);

        Assert.AreEqual(expectedSign, Math.Sign(a.CompareTo(b)));
    }

    /// <summary>
    /// Verifies that the relational operators agree with the result of <c>CompareTo</c>.
    /// </summary>
    [TestMethod]
    [DataRow(1, 3, 1, 2)]
    [DataRow(1, 2, 1, 2)]
    [DataRow(3, 2, 1, 2)]
    [DataRow(-1, 2, 1, 4)]
    public void RelationalOperators_WhenCompared_ShouldAgreeWithCompareTo(int an, int ad, int bn, int bd)
    {
        var a = new Fraction<int>(an, ad);
        var b = new Fraction<int>(bn, bd);
        var order = a.CompareTo(b);

        Assert.AreEqual(order < 0, a < b);
        Assert.AreEqual(order <= 0, a <= b);
        Assert.AreEqual(order > 0, a > b);
        Assert.AreEqual(order >= 0, a >= b);
        Assert.AreEqual(order == 0, a == b);
        Assert.AreEqual(order != 0, a != b);
    }
}
