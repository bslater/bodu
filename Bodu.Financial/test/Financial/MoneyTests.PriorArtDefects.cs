// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.PriorArtDefects.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Cross-cutting defect-parity contract: each test reproduces the input of a documented defect, gotcha, or
/// known-limitation in a reference money library (dinero.js, Joda-Money, or the <c>java.math.BigDecimal</c> engine
/// Joda wraps) and asserts that <see cref="Money" /> produces the correct result instead. The upstream defect class is
/// named in each test's summary so the guarded behaviour survives refactoring.
/// </summary>
public partial class MoneyTests
{
    /// <summary>
    /// Verifies that fractional arithmetic is exact — the binary-float precision loss that motivates dinero.js's
    /// integer-minor-units design (<c>0.1 + 0.2 !== 0.3</c> in IEEE-754 doubles) cannot occur because amounts are
    /// <see cref="decimal" />.
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenAddingFractionalTenths_ShouldBeExact()
    {
        Money sum = Money.FromExplicitScale(0.1m, CurrencyCode.USD, 1) + Money.FromExplicitScale(0.2m, CurrencyCode.USD, 1);

        Assert.AreEqual(new Money(0.30m, CurrencyCode.USD), sum);
        Assert.AreEqual(0.3m, sum.Amount);
    }

    /// <summary>
    /// Verifies that multiplying by a fractional factor is exact — the dinero.js documented float-multiplication
    /// hazard (e.g. a 29% fee on 4,545 units computed through binary floats drifts off 1,318.05) cannot occur with
    /// decimal scalar multiplication.
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenMultiplyingByFractionalFactor_ShouldBeExact()
    {
        Money fee = new Money(4545m, CurrencyCode.USD) * 0.29m;

        Assert.AreEqual(1318.05m, fee.Amount);
    }

    /// <summary>
    /// Verifies that a decimal midpoint literal rounds by its printed digits — the Joda-Money <c>Money.of(double)</c>
    /// surprise, where the binary double nearest to <c>1.235</c> is fractionally below the midpoint and rounds down,
    /// cannot occur because construction takes <see cref="decimal" />.
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenConstructingFromMidpointLiteral_ShouldRoundByPrintedDigits()
    {
        Money rounded = new Money(1.235m, CurrencyCode.USD, MidpointRounding.AwayFromZero);

        Assert.AreEqual(1.24m, rounded.Amount);
    }

    /// <summary>
    /// Verifies that equal amounts at different reported scales are equal and hash identically — the
    /// <c>BigDecimal.equals</c> scale-sensitivity defect class (<c>2.0</c> ≠ <c>2.00</c>, breaking
    /// <c>HashSet</c>/<c>HashMap</c> deduplication) does not exist; equality follows <see cref="decimal" /> numeric
    /// semantics.
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenComparingAcrossScales_ShouldBeEqualWithConsistentHashCodes()
    {
        Money settled = new Money(12.50m, CurrencyCode.USD);
        Money unitPrice = Money.FromExplicitScale(12.5m, CurrencyCode.USD, 6);

        Assert.AreEqual(settled, unitPrice);
        Assert.AreEqual(settled.GetHashCode(), unitPrice.GetHashCode());
        Assert.IsTrue(new HashSet<Money> { settled, unitPrice }.Count == 1);
    }

    /// <summary>
    /// Verifies that negating a zero amount produces a clean zero — the negative-zero rendering defect class (a
    /// stray <c>-0.00</c> after negation or sign-preserving arithmetic) does not leak into formatting or equality.
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenNegatingZero_ShouldFormatWithoutNegativeSign()
    {
        Money negatedZero = -new Money(0.00m, CurrencyCode.USD);

        Assert.AreEqual("USD 0.00", negatedZero.ToString("R"));
        Assert.AreEqual(Money.Zero(CurrencyCode.USD), negatedZero);
    }

    /// <summary>
    /// Verifies that midpoint rounding is symmetric for negative amounts — away-from-zero moves a negative midpoint
    /// further from zero, mirroring the positive case, rather than the "half-up moves toward positive infinity"
    /// asymmetry some rounding implementations exhibit.
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenRoundingNegativeMidpoint_ShouldBeSymmetricWithPositive()
    {
        Assert.AreEqual(-1.23m, new Money(-1.225m, CurrencyCode.USD, MidpointRounding.AwayFromZero).Amount);
        Assert.AreEqual(1.23m, new Money(1.225m, CurrencyCode.USD, MidpointRounding.AwayFromZero).Amount);
        Assert.AreEqual(-1.22m, new Money(-1.225m, CurrencyCode.USD).Amount);
        Assert.AreEqual(1.22m, new Money(1.225m, CurrencyCode.USD).Amount);
    }

    /// <summary>
    /// Verifies that arithmetic beyond the representable range throws <see cref="OverflowException" /> — the silent
    /// precision corruption dinero.js v1 exhibits past <c>Number.MAX_SAFE_INTEGER</c> cannot occur; <see cref="decimal" />
    /// arithmetic is checked.
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenSumExceedsRepresentableRange_ShouldThrowOverflowException() =>
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = new Money(decimal.MaxValue, CurrencyCode.USD) + new Money(1m, CurrencyCode.USD);
        });

    /// <summary>
    /// Verifies that allocation always conserves the whole amount — the lost-penny defect class in naive
    /// percentage-split implementations — including for negative amounts and residue-bearing ratios.
    /// </summary>
    [TestMethod]
    [DataRow("1.00", "1,3", DisplayName = "Residue on unequal ratios")]
    [DataRow("0.05", "70,30", DisplayName = "Amount smaller than ratio spread")]
    [DataRow("-0.05", "70,30", DisplayName = "Negative amount")]
    [DataRow("0.01", "1,1,1", DisplayName = "Single minor unit across three ways")]
    public void PriorArtDefects_WhenAllocating_ShouldConserveTotalExactly(string amountText, string ratioText)
    {
        var money = new Money(decimal.Parse(amountText, CultureInfo.InvariantCulture), CurrencyCode.USD);
        decimal[] ratios = ratioText.Split(',').Select(r => decimal.Parse(r, CultureInfo.InvariantCulture)).ToArray();

        Money[] shares = money.Allocate(ratios);

        Assert.AreEqual(money.Amount, shares.Sum(s => s.Amount));
    }

    /// <summary>
    /// Verifies that zero-weight allocation slots never receive residue — the misdirected-remainder defect class in
    /// largest-remainder implementations that consider zero-ratio slots when distributing leftover minor units.
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenAllocatingWithZeroRatio_ShouldGiveZeroSlotNothing()
    {
        Money[] shares = new Money(1.00m, CurrencyCode.USD).Allocate([0m, 50m, 50m]);

        Assert.AreEqual(0m, shares[0].Amount);
        Assert.AreEqual(1.00m, shares[1].Amount + shares[2].Amount);
    }

    /// <summary>
    /// Verifies that rescaling to a coarser precision uses the defaulted rounding rule instead of failing — the
    /// <c>BigDecimal.setScale</c> defect class, which throws <c>ArithmeticException</c> when precision would be lost
    /// and no rounding mode was supplied.
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenRescalingLosesPrecision_ShouldRoundInsteadOfThrowing()
    {
        Money rescaled = Money.FromExplicitScale(1.999m, CurrencyCode.USD, 3).Rescale(2);

        Assert.AreEqual(2.00m, rescaled.Amount);
    }

    /// <summary>
    /// Verifies that trimming a zero amount held at a wide scale collapses cleanly to the registered precision — the
    /// trim-of-zero edge some scale-normalisation implementations mishandle.
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenTrimmingZeroAtWideScale_ShouldCollapseToRegistryPrecision()
    {
        Money trimmed = Money.FromExplicitScale(0m, CurrencyCode.USD, 6).TrimScale();

        Assert.AreEqual(2, trimmed.MinorUnits);
        Assert.AreEqual("USD 0.00", trimmed.ToString("R"));
    }
}
