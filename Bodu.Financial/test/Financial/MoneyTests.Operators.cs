// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that subtracting two same-currency amounts returns their difference.
    /// </summary>
    [TestMethod]
    public void Subtraction_WhenSameCurrency_ShouldReturnDifference() =>
        Assert.AreEqual(new Money(7m, CurrencyCode.USD), new Money(10m, CurrencyCode.USD) - new Money(3m, CurrencyCode.USD));

    /// <summary>
    /// Verifies that the unary negation operator reverses the sign while preserving the currency.
    /// </summary>
    [TestMethod]
    public void UnaryNegation_WhenHasCurrency_ShouldReverseSign() =>
        Assert.AreEqual(new Money(-5m, CurrencyCode.USD), -new Money(5m, CurrencyCode.USD));

    /// <summary>
    /// Verifies that the unary negation operator rejects a currency-less default value.
    /// </summary>
    [TestMethod]
    public void UnaryNegation_WhenDefault_ShouldThrowInvalidOperationException() =>
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = -default(Money));

    /// <summary>
    /// Verifies that the unary plus operator returns the operand unchanged.
    /// </summary>
    [TestMethod]
    public void UnaryPlus_WhenApplied_ShouldReturnValueUnchanged()
    {
        var value = new Money(5m, CurrencyCode.USD);

        Assert.AreEqual(value, +value);
    }

    /// <summary>
    /// Verifies that scalar multiplication is commutative: a scalar on the left yields the same product as on the
    /// right.
    /// </summary>
    [TestMethod]
    public void Multiplication_WhenScalarOnLeft_ShouldMatchScalarOnRight()
    {
        var price = new Money(0.95m, CurrencyCode.USD);

        Assert.AreEqual(price * 3m, 3m * price);
    }

    /// <summary>
    /// Verifies that a scalar-on-the-left product rejects a currency-less default value.
    /// </summary>
    [TestMethod]
    public void Multiplication_WhenScalarOnLeftAndDefault_ShouldThrowInvalidOperationException() =>
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = 3m * default(Money));

    /// <summary>
    /// Verifies that the named multiply and divide methods agree with the operators and honour an explicit midpoint
    /// rule.
    /// </summary>
    [TestMethod]
    public void NamedMultiplyAndDivide_WhenInvoked_ShouldMatchOperatorsAndRounding()
    {
        var value = new Money(10m, CurrencyCode.USD);

        Assert.AreEqual(value * 3m, value.Multiply(3m));
        Assert.AreEqual(value / 4m, value.Divide(4m));

        // 1 / 8 = 0.125 — a half-cent midpoint that the two rules resolve differently.
        Assert.AreEqual(new Money(0.12m, CurrencyCode.USD), new Money(1m, CurrencyCode.USD).Divide(8m, MidpointRounding.ToEven));
        Assert.AreEqual(new Money(0.13m, CurrencyCode.USD), new Money(1m, CurrencyCode.USD).Multiply(0.125m, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Verifies that the ordering operators compare same-currency amounts by value.
    /// </summary>
    [TestMethod]
    public void ComparisonOperators_WhenSameCurrency_ShouldOrderByAmount()
    {
        var low = new Money(1m, CurrencyCode.USD);
        var high = new Money(2m, CurrencyCode.USD);
        var lowAgain = new Money(1m, CurrencyCode.USD);

        Assert.IsTrue(low < high);
        Assert.IsFalse(high < low);
        Assert.IsTrue(low <= lowAgain);
        Assert.IsTrue(high > low);
        Assert.IsFalse(low > high);
        Assert.IsTrue(high >= low);
        Assert.IsTrue(low >= lowAgain);
    }

    /// <summary>
    /// Verifies that the inequality operator is the negation of equality.
    /// </summary>
    [TestMethod]
    public void InequalityOperator_WhenComparingValues_ShouldReflectEquality()
    {
        Assert.IsTrue(new Money(1m, CurrencyCode.USD) != new Money(2m, CurrencyCode.USD));
        Assert.IsFalse(new Money(1m, CurrencyCode.USD) != new Money(1m, CurrencyCode.USD));
    }

    /// <summary>
    /// Verifies that the boxed <see cref="object.Equals(object)" /> override matches an equal value and rejects a
    /// non-<see cref="Money" /> object.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedAsBoxedObject_ShouldMatchValue()
    {
        Assert.IsTrue(new Money(1m, CurrencyCode.USD).Equals((object)new Money(1m, CurrencyCode.USD)));
        Assert.IsFalse(new Money(1m, CurrencyCode.USD).Equals((object)"USD"));
    }

    /// <summary>
    /// Verifies that <see cref="IComparable.CompareTo(object)" /> orders boxed same-currency values, treats
    /// <see langword="null" /> as preceding, and rejects a non-<see cref="Money" /> argument.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenComparedAsBoxedObject_ShouldOrderAndReject()
    {
        var value = new Money(1m, CurrencyCode.USD);

        Assert.AreEqual(1, value.CompareTo((object?)null));
        Assert.AreEqual(0, value.CompareTo((object)new Money(1m, CurrencyCode.USD)));
        Assert.IsTrue(value.CompareTo((object)new Money(2m, CurrencyCode.USD)) < 0);
        Assert.ThrowsExactly<ArgumentException>(() => _ = value.CompareTo((object)"not money"));
    }

    /// <summary>
    /// Verifies that <c>Sign</c> reports the sign of the amount.
    /// </summary>
    [TestMethod]
    [DataRow(5, 1)]
    [DataRow(-5, -1)]
    [DataRow(0, 0)]
    public void Sign_WhenInspected_ShouldReturnSignOfAmount(int amount, int expected) =>
        Assert.AreEqual(expected, new Money(amount, CurrencyCode.USD).Sign);
}
