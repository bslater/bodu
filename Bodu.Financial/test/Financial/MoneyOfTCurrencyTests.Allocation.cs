// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyTests.Allocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyOfTCurrencyTests
{
    /// <summary>
    /// Verifies that allocating ten cents into three shares returns <c>[0.04, 0.03, 0.03]</c>, with the residual
    /// going to the first share.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenTenCentsIntoThreeShares_ShouldDistributeResidualToFirstShare()
    {
        Money<USD>[] shares = new Money<USD>(0.10m).Allocate(3);

        Assert.AreEqual(3, shares.Length);
        Assert.AreEqual(new Money<USD>(0.04m), shares[0]);
        Assert.AreEqual(new Money<USD>(0.03m), shares[1]);
        Assert.AreEqual(new Money<USD>(0.03m), shares[2]);
    }

    /// <summary>
    /// Verifies that the sum of every allocation equals the original amount across a range of inputs.
    /// </summary>
    [TestMethod]
    [DataRow(0.10, 3)]
    [DataRow(100.0, 7)]
    [DataRow(1.0, 11)]
    [DataRow(0.05, 100)]
    public void Allocate_WhenSumProperty_ShouldPreserveOriginalAmount(double amount, int parts)
    {
        var original = new Money<USD>((decimal)amount);

        Money<USD>[] shares = original.Allocate(parts);

        Money<USD> sum = default;
        foreach (Money<USD> share in shares)
            sum += share;

        Assert.AreEqual(original, sum);
    }

    /// <summary>
    /// Verifies that allocating a negative amount preserves the sign in each share and the total.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenNegativeAmount_ShouldDistributeSignStable()
    {
        Money<USD>[] shares = new Money<USD>(-10m).Allocate(3);

        Assert.AreEqual(new Money<USD>(-3.34m), shares[0]);
        Assert.AreEqual(new Money<USD>(-3.33m), shares[1]);
        Assert.AreEqual(new Money<USD>(-3.33m), shares[2]);
    }

    /// <summary>
    /// Verifies that allocating fewer minor units than parts yields trailing zero shares.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenAmountSmallerThanParts_ShouldPadWithZeroShares()
    {
        Money<USD>[] shares = new Money<USD>(0.02m).Allocate(5);

        Assert.AreEqual(new Money<USD>(0.01m), shares[0]);
        Assert.AreEqual(new Money<USD>(0.01m), shares[1]);
        Assert.AreEqual(Money<USD>.Zero, shares[2]);
        Assert.AreEqual(Money<USD>.Zero, shares[3]);
        Assert.AreEqual(Money<USD>.Zero, shares[4]);
    }

    /// <summary>
    /// Verifies that allocating into zero or fewer parts throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Allocate_WhenPartsNotPositive_ShouldThrowArgumentOutOfRangeException(int parts)
    {
        var money = new Money<USD>(10m);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = money.Allocate(parts);
        });
    }

    /// <summary>
    /// Verifies that ratio-based allocation distributes shares proportionally and preserves the original amount.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenGivenRatios_ShouldDistributeProportionally()
    {
        var revenue = new Money<USD>(100m);
        decimal[] ratios = [1m, 1m, 2m];

        Money<USD>[] shares = revenue.Allocate(ratios);

        Money<USD> sum = default;
        foreach (Money<USD> share in shares)
            sum += share;

        Assert.AreEqual(revenue, sum);
        Assert.AreEqual(new Money<USD>(25m), shares[0]);
        Assert.AreEqual(new Money<USD>(25m), shares[1]);
        Assert.AreEqual(new Money<USD>(50m), shares[2]);
    }

    /// <summary>
    /// Verifies that ratio-based allocation skips zero-ratio slots when distributing the residual.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenZeroRatioMixedWithPositive_ShouldYieldZeroForZeroRatio()
    {
        var revenue = new Money<USD>(0.07m);
        decimal[] ratios = [1m, 0m, 1m];

        Money<USD>[] shares = revenue.Allocate(ratios);

        Money<USD> sum = default;
        foreach (Money<USD> share in shares)
            sum += share;

        Assert.AreEqual(revenue, sum);
        Assert.AreEqual(Money<USD>.Zero, shares[1]);
    }

    /// <summary>
    /// Verifies that an empty ratios span throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenRatiosEmpty_ShouldThrowArgumentException()
    {
        var money = new Money<USD>(10m);
        decimal[] ratios = Array.Empty<decimal>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = money.Allocate(ratios);
        });
    }

    /// <summary>
    /// Verifies that a negative ratio throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenNegativeRatio_ShouldThrowArgumentException()
    {
        var money = new Money<USD>(10m);
        decimal[] ratios = [1m, -1m, 2m];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = money.Allocate(ratios);
        });
    }

    /// <summary>
    /// Verifies that all-zero ratios throw <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenRatiosAllZero_ShouldThrowArgumentException()
    {
        var money = new Money<USD>(10m);
        decimal[] ratios = [0m, 0m, 0m];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = money.Allocate(ratios);
        });
    }
}
