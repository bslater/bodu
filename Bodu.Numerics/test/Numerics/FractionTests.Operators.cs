// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Operators.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
        Fraction<int> a = new Fraction<int>(2, 3);
        Fraction<int> b = new Fraction<int>(1, 6);

        Assert.AreEqual(new Fraction<int>(5, 6), a + b);
        Assert.AreEqual(new Fraction<int>(1, 2), a - b);
        Assert.AreEqual(new Fraction<int>(1, 9), a * b);
        Assert.AreEqual(new Fraction<int>(4, 1), a / b);
    }

    /// <summary>
    /// Verifies that dividing a fraction by zero throws <see cref="DivideByZeroException" />.
    /// </summary>
    [TestMethod]
    public void DivisionOperator_WhenDividingByZero_ShouldThrowDivideByZeroException()
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
        Fraction<int> value = new Fraction<int>(1, 2);
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
        Fraction<int> small = new Fraction<int>(1, 3);
        Fraction<int> large = new Fraction<int>(1, 2);

        Assert.IsTrue(small < large);
        Assert.IsTrue(small <= large);
        Assert.IsTrue(large > small);
        Assert.IsTrue(large >= small);
        Assert.IsTrue(small != large);
        Assert.IsTrue(small == new Fraction<int>(2, 6));
    }
}
