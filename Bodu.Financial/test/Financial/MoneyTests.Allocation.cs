// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Allocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that allocating ten cents into three equal shares distributes the residual to the leading shares and
    /// sums to the original amount.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenTenCentsIntoThreeShares_ShouldSumToOriginal()
    {
        Money[] shares = Money.From(0.10m, "USD").Allocate(3);

        Assert.AreEqual(3, shares.Length);
        Assert.AreEqual(Money.From(0.04m, "USD"), shares[0]);
        Assert.AreEqual(Money.From(0.03m, "USD"), shares[1]);
        Assert.AreEqual(Money.From(0.03m, "USD"), shares[2]);
    }

    /// <summary>
    /// Verifies that ratio allocation awards residual minor units by largest fractional remainder rather than by input
    /// order. With ratios <c>[3, 2, 2]</c> over ten cents the exact shares are <c>4.29, 2.86, 2.86</c>; the two largest
    /// remainders belong to the second and third slots, so the result is <c>[0.04, 0.03, 0.03]</c> — not the
    /// input-order result <c>[0.05, 0.03, 0.02]</c>.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenResidualFavoursLargerRemainders_ShouldUseLargestRemainderMethod()
    {
        Money[] shares = Money.From(0.10m, "USD").Allocate([3m, 2m, 2m]);

        Assert.AreEqual(Money.From(0.04m, "USD"), shares[0]);
        Assert.AreEqual(Money.From(0.03m, "USD"), shares[1]);
        Assert.AreEqual(Money.From(0.03m, "USD"), shares[2]);
    }

    /// <summary>
    /// Verifies that ratio allocation breaks ties in fractional remainder by ascending input order, so the earlier slot
    /// receives the residual when two remainders are equal.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenRemaindersTie_ShouldBreakTieByInputOrder()
    {
        Money[] shares = Money.From(0.05m, "USD").Allocate([1m, 1m, 1m]);

        // Exact shares are 1.667 each; floored to [1,1,1], residual 2, all remainders equal => slots 0 and 1.
        Assert.AreEqual(Money.From(0.02m, "USD"), shares[0]);
        Assert.AreEqual(Money.From(0.02m, "USD"), shares[1]);
        Assert.AreEqual(Money.From(0.01m, "USD"), shares[2]);
    }

    /// <summary>
    /// Verifies that a zero-weight slot never receives residual minor units.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenRatioIsZero_ShouldNotReceiveResidual()
    {
        Money[] shares = Money.From(0.10m, "USD").Allocate([1m, 0m, 1m]);

        Assert.AreEqual(Money.From(0.05m, "USD"), shares[0]);
        Assert.AreEqual(Money.From(0.00m, "USD"), shares[1]);
        Assert.AreEqual(Money.From(0.05m, "USD"), shares[2]);
    }

    /// <summary>
    /// Verifies that ratio allocation of a negative amount preserves the sign and still sums to the original amount.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenAmountIsNegative_ShouldPreserveSignAndSum()
    {
        Money[] shares = Money.From(-0.10m, "USD").Allocate([3m, 2m, 2m]);

        Assert.AreEqual(Money.From(-0.04m, "USD"), shares[0]);
        Assert.AreEqual(Money.From(-0.03m, "USD"), shares[1]);
        Assert.AreEqual(Money.From(-0.03m, "USD"), shares[2]);
    }

    /// <summary>
    /// Verifies that ratio allocation honours a zero-minor-unit currency, distributing whole major units by largest
    /// remainder.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenCurrencyHasZeroMinorUnits_ShouldAllocateWholeUnits()
    {
        Money[] shares = Money.From(10m, "JPY").Allocate([3m, 2m, 2m]);

        Assert.AreEqual(Money.From(4m, "JPY"), shares[0]);
        Assert.AreEqual(Money.From(3m, "JPY"), shares[1]);
        Assert.AreEqual(Money.From(3m, "JPY"), shares[2]);
    }

    /// <summary>
    /// Verifies that ratio allocation honours a three-minor-unit currency, distributing the residual fils by largest
    /// remainder.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenCurrencyHasThreeMinorUnits_ShouldAllocateThousandths()
    {
        Money[] shares = Money.From(0.010m, "BHD").Allocate([3m, 2m, 2m]);

        Assert.AreEqual(Money.From(0.004m, "BHD"), shares[0]);
        Assert.AreEqual(Money.From(0.003m, "BHD"), shares[1]);
        Assert.AreEqual(Money.From(0.003m, "BHD"), shares[2]);
    }

    /// <summary>
    /// Verifies that ratio allocation never places a share more than one minor unit away from its exact proportional
    /// value, and that the shares sum to the original amount.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenUnevenRatios_ShouldStayWithinOneMinorUnitOfExactShare()
    {
        decimal[] ratios = [17m, 5m, 3m, 11m, 1m];
        var original = Money.From(1.23m, "USD");

        Money[] shares = original.Allocate(ratios);

        var totalWeight = 0m;
        foreach (var r in ratios)
            totalWeight += r;

        var sum = Money.From(0m, "USD");
        for (var i = 0; i < ratios.Length; i++)
        {
            sum += shares[i];
            var exact = original.Amount * ratios[i] / totalWeight;
            Assert.IsTrue(Math.Abs(shares[i].Amount - exact) <= 0.01m);
        }

        Assert.AreEqual(original, sum);
    }

    /// <summary>
    /// Verifies that allocating an amount whose scaled minor-unit count exceeds the range of a 64-bit signed integer
    /// allocates exactly via <see cref="System.Numerics.BigInteger" /> scaling, with the shares summing to the
    /// original.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenScaledMinorUnitsExceedInt64_ShouldAllocateExactlyAndSumToOriginal()
    {
        var huge = Money.From(100_000_000_000_000_000m, "USD");

        Money[] shares = huge.Allocate([1m, 1m]);

        Assert.AreEqual(2, shares.Length);
        Assert.AreEqual(Money.From(50_000_000_000_000_000m, "USD"), shares[0]);
        Assert.AreEqual(Money.From(50_000_000_000_000_000m, "USD"), shares[1]);
        Assert.AreEqual(huge, shares[0] + shares[1]);
    }

    /// <summary>
    /// Verifies that a ratio-based allocation assigns nothing to a zero-weighted slot, even when a residual cent must
    /// be distributed across the remaining slots.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenRatioIsZero_ShouldGiveZeroSlotNothing()
    {
        Money[] shares = new Money(0.11m, "USD").Allocate([0m, 1m, 1m]);

        Assert.AreEqual(Money.From(0m, "USD"), shares[0]);
        Assert.AreEqual(new Money(0.11m, "USD"), shares[0] + shares[1] + shares[2]);
    }
}
