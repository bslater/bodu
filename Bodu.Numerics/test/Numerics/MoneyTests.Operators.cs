// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Operators.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics.Currencies;

namespace Bodu.Numerics;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that addition of two same-currency amounts produces their sum.
    /// </summary>
    [TestMethod]
    public void Addition_WhenSameCurrency_ShouldReturnSum()
    {
        Money<USD> a = new Money<USD>(10.50m);
        Money<USD> b = new Money<USD>(5.25m);

        Assert.AreEqual(new Money<USD>(15.75m), a + b);
    }

    /// <summary>
    /// Verifies that subtraction returns the difference between two same-currency amounts.
    /// </summary>
    [TestMethod]
    public void Subtraction_WhenSameCurrency_ShouldReturnDifference()
    {
        Money<USD> a = new Money<USD>(10.50m);
        Money<USD> b = new Money<USD>(5.25m);

        Assert.AreEqual(new Money<USD>(5.25m), a - b);
    }

    /// <summary>
    /// Verifies that unary negation flips the sign of the amount.
    /// </summary>
    [TestMethod]
    public void UnaryNegation_WhenApplied_ShouldNegateAmount()
    {
        Money<USD> money = new Money<USD>(19.99m);

        Assert.AreEqual(new Money<USD>(-19.99m), -money);
    }

    /// <summary>
    /// Verifies that scalar multiplication scales the amount and rounds to the currency's minor-unit precision.
    /// </summary>
    [TestMethod]
    public void Multiplication_WhenScalar_ShouldScaleAndRoundToMinorUnits()
    {
        Money<USD> price = new Money<USD>(0.95m);

        Money<USD> result = price * 3m;

        Assert.AreEqual(new Money<USD>(2.85m), result);
    }

    /// <summary>
    /// Verifies that scalar multiplication is commutative.
    /// </summary>
    [TestMethod]
    public void Multiplication_WhenScalarOnLeft_ShouldBeCommutative()
    {
        Money<USD> price = new Money<USD>(2m);

        Assert.AreEqual(price * 3m, 3m * price);
    }

    /// <summary>
    /// Verifies that scalar division returns the quotient rounded to the currency's minor-unit precision.
    /// </summary>
    [TestMethod]
    public void Division_WhenScalar_ShouldReturnRoundedQuotient()
    {
        Money<USD> total = new Money<USD>(10m);

        Money<USD> share = total / 3m;

        Assert.AreEqual(new Money<USD>(3.33m), share);
    }

    /// <summary>
    /// Verifies that dividing two amounts returns their dimensionless ratio.
    /// </summary>
    [TestMethod]
    public void Division_WhenTwoMoneyValues_ShouldReturnDimensionlessRatio()
    {
        Money<USD> a = new Money<USD>(10m);
        Money<USD> b = new Money<USD>(4m);

        decimal ratio = a / b;

        Assert.AreEqual(2.5m, ratio);
    }

    /// <summary>
    /// Verifies that scalar division by zero throws <see cref="DivideByZeroException" />.
    /// </summary>
    [TestMethod]
    public void Division_WhenScalarIsZero_ShouldThrowDivideByZeroException()
    {
        Money<USD> money = new Money<USD>(10m);

        Assert.ThrowsExactly<DivideByZeroException>(() =>
        {
            _ = money / 0m;
        });
    }

    /// <summary>
    /// Verifies that the comparison operators agree with the magnitude of the amounts.
    /// </summary>
    [TestMethod]
    public void ComparisonOperators_WhenSameCurrency_ShouldOrderByAmount()
    {
        Money<USD> small = new Money<USD>(1m);
        Money<USD> large = new Money<USD>(2m);

        Assert.IsTrue(small < large);
        Assert.IsTrue(small <= large);
        Assert.IsTrue(large > small);
        Assert.IsTrue(large >= small);
        Assert.IsTrue(small <= new Money<USD>(1m));
        Assert.IsTrue(small >= new Money<USD>(1m));
    }
}
