// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyTests.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyOfTCurrencyTests
{
    /// <summary>
    /// Verifies that addition of two same-currency amounts produces their sum.
    /// </summary>
    [TestMethod]
    public void Addition_WhenSameCurrency_ShouldReturnSum()
    {
        var a = new Money<USD>(10.50m);
        var b = new Money<USD>(5.25m);

        Assert.AreEqual(new Money<USD>(15.75m), a + b);
    }

    /// <summary>
    /// Verifies that subtraction returns the difference between two same-currency amounts.
    /// </summary>
    [TestMethod]
    public void Subtraction_WhenSameCurrency_ShouldReturnDifference()
    {
        var a = new Money<USD>(10.50m);
        var b = new Money<USD>(5.25m);

        Assert.AreEqual(new Money<USD>(5.25m), a - b);
    }

    /// <summary>
    /// Verifies that unary negation flips the sign of the amount.
    /// </summary>
    [TestMethod]
    public void UnaryNegation_WhenApplied_ShouldNegateAmount()
    {
        var money = new Money<USD>(19.99m);

        Assert.AreEqual(new Money<USD>(-19.99m), -money);
    }

    /// <summary>
    /// Verifies that scalar multiplication scales the amount and rounds to the currency's minor-unit precision.
    /// </summary>
    [TestMethod]
    public void Multiplication_WhenScalar_ShouldScaleAndRoundToMinorUnits()
    {
        var price = new Money<USD>(0.95m);

        Money<USD> result = price * 3m;

        Assert.AreEqual(new Money<USD>(2.85m), result);
    }

    /// <summary>
    /// Verifies that scalar multiplication is commutative.
    /// </summary>
    [TestMethod]
    public void Multiplication_WhenScalarOnLeft_ShouldBeCommutative()
    {
        var price = new Money<USD>(2m);

        Assert.AreEqual(price * 3m, 3m * price);
    }

    /// <summary>
    /// Verifies that scalar division returns the quotient rounded to the currency's minor-unit precision.
    /// </summary>
    [TestMethod]
    public void Division_WhenScalar_ShouldReturnRoundedQuotient()
    {
        var total = new Money<USD>(10m);

        Money<USD> share = total / 3m;

        Assert.AreEqual(new Money<USD>(3.33m), share);
    }

    /// <summary>
    /// Verifies that dividing two amounts returns their dimensionless ratio.
    /// </summary>
    [TestMethod]
    public void Division_WhenTwoMoneyValues_ShouldReturnDimensionlessRatio()
    {
        var a = new Money<USD>(10m);
        var b = new Money<USD>(4m);

        var ratio = a / b;

        Assert.AreEqual(2.5m, ratio);
    }

    /// <summary>
    /// Verifies that scalar division by zero throws <see cref="DivideByZeroException" />.
    /// </summary>
    [TestMethod]
    public void Division_WhenScalarIsZero_ShouldThrowDivideByZeroException()
    {
        var money = new Money<USD>(10m);

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
        var small = new Money<USD>(1m);
        var large = new Money<USD>(2m);

        Assert.IsTrue(small < large);
        Assert.IsTrue(small <= large);
        Assert.IsTrue(large > small);
        Assert.IsTrue(large >= small);
        Assert.IsTrue(small <= new Money<USD>(1m));
        Assert.IsTrue(small >= new Money<USD>(1m));
    }

    /// <summary>
    /// Verifies that addition throws <see cref="OverflowException" /> when the sum exceeds the range of
    /// <see cref="decimal" />.
    /// </summary>
    [TestMethod]
    public void Addition_WhenResultExceedsDecimalRange_ShouldThrowOverflowException()
    {
        var max = new Money<USD>(decimal.MaxValue);
        var one = new Money<USD>(1m);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = max + one;
        });
    }

    /// <summary>
    /// Verifies that subtraction throws <see cref="OverflowException" /> when the difference falls below
    /// <see cref="decimal.MinValue" />.
    /// </summary>
    [TestMethod]
    public void Subtraction_WhenResultExceedsDecimalRange_ShouldThrowOverflowException()
    {
        var min = new Money<USD>(decimal.MinValue);
        var one = new Money<USD>(1m);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = min - one;
        });
    }

    /// <summary>
    /// Verifies that scalar multiplication throws <see cref="OverflowException" /> when the product falls
    /// outside the range of <see cref="decimal" />.
    /// </summary>
    [TestMethod]
    public void Multiplication_WhenResultExceedsDecimalRange_ShouldThrowOverflowException()
    {
        var large = new Money<USD>(decimal.MaxValue / 2m);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = large * 3m;
        });
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Multiply(decimal)" /> matches the <c>*</c> operator output for
    /// banker's-rounded results.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenDefaultRounding_ShouldMatchOperator()
    {
        var money = new Money<USD>(1m);

        Assert.AreEqual(money * 1.225m, money.Multiply(1.225m));
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Multiply(decimal, MidpointRounding)" /> applies the supplied
    /// rounding rule, diverging from the operator at midpoint values.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenAwayFromZeroSpecified_ShouldRoundMidpointAwayFromZero()
    {
        var money = new Money<USD>(1m);

        Money<USD> banker = money.Multiply(1.225m);                                      // 1.225 → 1.22 (down to even)
        Money<USD> awayFromZero = money.Multiply(1.225m, MidpointRounding.AwayFromZero); // 1.225 → 1.23

        Assert.AreEqual(new Money<USD>(1.22m), banker);
        Assert.AreEqual(new Money<USD>(1.23m), awayFromZero);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Divide(decimal)" /> matches the <c>/</c> operator output for
    /// banker's-rounded results.
    /// </summary>
    [TestMethod]
    public void Divide_WhenDefaultRounding_ShouldMatchOperator()
    {
        var money = new Money<USD>(10m);

        Assert.AreEqual(money / 8m, money.Divide(8m));
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Divide(decimal, MidpointRounding)" /> applies the supplied
    /// rounding rule.
    /// </summary>
    [TestMethod]
    public void Divide_WhenAwayFromZeroSpecified_ShouldRoundMidpointAwayFromZero()
    {
        var money = new Money<USD>(1m);

        Money<USD> banker = money.Divide(8m);                                       // 0.125 → 0.12 (down to even)
        Money<USD> awayFromZero = money.Divide(8m, MidpointRounding.AwayFromZero);  // 0.125 → 0.13

        Assert.AreEqual(new Money<USD>(0.12m), banker);
        Assert.AreEqual(new Money<USD>(0.13m), awayFromZero);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Divide(decimal, MidpointRounding)" /> throws
    /// <see cref="DivideByZeroException" /> when the divisor is zero.
    /// </summary>
    [TestMethod]
    public void Divide_WhenDivisorIsZero_ShouldThrowDivideByZeroException()
    {
        var money = new Money<USD>(10m);

        Assert.ThrowsExactly<DivideByZeroException>(() =>
        {
            _ = money.Divide(0m, MidpointRounding.AwayFromZero);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.RatioTo(Money{TCurrency})" /> returns the dimensionless ratio
    /// of two same-currency amounts.
    /// </summary>
    [TestMethod]
    public void RatioTo_WhenSameCurrencyAmounts_ShouldReturnDecimalRatio()
    {
        var ten = new Money<USD>(10m);
        var four = new Money<USD>(4m);

        Assert.AreEqual(2.5m, ten.RatioTo(four));
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.RatioTo(Money{TCurrency})" /> throws
    /// <see cref="DivideByZeroException" /> when the denominator amount is zero.
    /// </summary>
    [TestMethod]
    public void RatioTo_WhenRightAmountIsZero_ShouldThrowDivideByZeroException()
    {
        var ten = new Money<USD>(10m);

        Assert.ThrowsExactly<DivideByZeroException>(() =>
        {
            _ = ten.RatioTo(Money<USD>.Zero);
        });
    }

    /// <summary>
    /// Verifies that the <c>Money / Money</c> operator throws <see cref="DivideByZeroException" /> when the
    /// right-hand amount is zero, consistent with <see cref="RatioTo(Money{TCurrency})" />.
    /// </summary>
    [TestMethod]
    public void Operator_WhenMoneyDividedByZeroValuedMoney_ShouldThrowDivideByZeroException()
    {
        var ten = new Money<USD>(10m);

        Assert.ThrowsExactly<DivideByZeroException>(() =>
        {
            _ = ten / Money<USD>.Zero;
        });
    }

    /// <summary>
    /// Verifies that the unary plus operator returns the operand unchanged.
    /// </summary>
    [TestMethod]
    public void UnaryPlus_WhenApplied_ShouldReturnSameValue()
    {
        var money = new Money<USD>(12.34m);

        Assert.AreEqual(money, +money);
    }
}

