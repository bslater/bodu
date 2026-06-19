// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Arithmetic.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that the <c>*</c> operator rounds the product to the currency's minor units using banker's rounding,
    /// so a midpoint product (<c>0.125</c>) rounds to the even neighbour <c>0.12</c>.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenUsingOperator_ShouldRoundToEven()
    {
        Money result = Money.From(0.05m, CurrencyCode.USD) * 2.5m;

        Assert.AreEqual(0.12m, result.Amount);
    }

    /// <summary>
    /// Verifies that <see cref="Money.Multiply(decimal, MidpointRounding)" /> honours the supplied rounding rule, so a
    /// midpoint product rounds away from zero to <c>0.13</c> rather than to the even neighbour produced by the operator.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenRoundingAwayFromZero_ShouldRoundAwayFromZero()
    {
        Money result = Money.From(0.05m, CurrencyCode.USD).Multiply(2.5m, MidpointRounding.AwayFromZero);

        Assert.AreEqual(0.13m, result.Amount);
    }

    /// <summary>
    /// Verifies that <see cref="Money.Multiply(decimal)" /> rounds with banker's rounding, matching the <c>*</c>
    /// operator.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenNoRoundingMode_ShouldMatchOperator()
    {
        Money result = Money.From(0.05m, CurrencyCode.USD).Multiply(2.5m);

        Assert.AreEqual(0.12m, result.Amount);
    }

    /// <summary>
    /// Verifies that <see cref="Money.Divide(decimal, MidpointRounding)" /> honours the supplied rounding rule, so a
    /// midpoint quotient rounds away from zero to <c>0.13</c>.
    /// </summary>
    [TestMethod]
    public void Divide_WhenRoundingAwayFromZero_ShouldRoundAwayFromZero()
    {
        Money result = Money.From(0.05m, CurrencyCode.USD).Divide(0.4m, MidpointRounding.AwayFromZero);

        Assert.AreEqual(0.13m, result.Amount);
    }

    /// <summary>
    /// Verifies that the <c>/</c> operator rounds the quotient with banker's rounding, so a midpoint quotient rounds to
    /// the even neighbour <c>0.12</c>.
    /// </summary>
    [TestMethod]
    public void Divide_WhenUsingOperator_ShouldRoundToEven()
    {
        Money result = Money.From(0.05m, CurrencyCode.USD) / 0.4m;

        Assert.AreEqual(0.12m, result.Amount);
    }
}
