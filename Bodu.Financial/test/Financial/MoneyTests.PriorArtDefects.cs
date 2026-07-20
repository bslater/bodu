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
    /// trim-of-zero edge dinero.js mishandled with an infinite loop (dinerojs/dinero.js#640).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenTrimmingZeroAtWideScale_ShouldCollapseToRegistryPrecision()
    {
        Money trimmed = Money.FromExplicitScale(0m, CurrencyCode.USD, 6).TrimScale();

        Assert.AreEqual(2, trimmed.MinorUnits);
        Assert.AreEqual("USD 0.00", trimmed.ToString("R"));
    }

    /// <summary>
    /// Verifies that the undivided minor unit follows the largest fractional remainder — dinero.js gave the residue to
    /// the first slot (<c>1003 by [49,51]</c> → 4.92/5.11 instead of 4.91/5.12; dinerojs/dinero.js#144).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenAllocatingResidue_ShouldFollowLargestRemainder()
    {
        Money[] shares = new Money(10.03m, CurrencyCode.USD).Allocate([49m, 51m]);

        Assert.AreEqual(4.91m, shares[0].Amount);
        Assert.AreEqual(5.12m, shares[1].Amount);
    }

    /// <summary>
    /// Verifies that residue lands on the slot with the largest remainder rather than positionally — dinero.js
    /// distributed <c>5 by [100,101,100,100]</c> as [1,1,1,2] instead of [1,2,1,1] (dinerojs/dinero.js#776).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenResidueBelongsToMiddleSlot_ShouldNotAssignPositionally()
    {
        Money[] shares = new Money(0.05m, CurrencyCode.USD).Allocate([100m, 101m, 100m, 100m]);

        Assert.AreEqual(0.01m, shares[0].Amount);
        Assert.AreEqual(0.02m, shares[1].Amount);
        Assert.AreEqual(0.01m, shares[2].Amount);
        Assert.AreEqual(0.01m, shares[3].Amount);
    }

    /// <summary>
    /// Verifies that allocating an amount far beyond 2^53 terminates and conserves the total — dinero.js's
    /// distribute loop hangs forever on such amounts (dinerojs/dinero.js#771).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenAllocatingHugeAmount_ShouldTerminateAndConserveTotal()
    {
        var money = new Money(337582417582417600000m, CurrencyCode.USD);

        Money[] shares = money.Allocate([3m, 7m]);

        Assert.AreEqual(money.Amount, shares[0].Amount + shares[1].Amount);
    }

    /// <summary>
    /// Verifies that multiplying a zero-minor-unit currency by a fractional factor rounds back to whole units —
    /// dinero.js produced sub-unit amounts for exponent-0 currencies (dinerojs/dinero.js#435).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenMultiplyingZeroMinorUnitCurrency_ShouldStayWholeUnits()
    {
        Money result = new Money(1423m, CurrencyCode.JPY) * 0.05m;

        Assert.AreEqual(71m, result.Amount);
        Assert.AreEqual(0, result.MinorUnits);
    }

    /// <summary>
    /// Verifies that rescaling a midpoint from a wider scale rounds half-even correctly — dinero.js's
    /// <c>convertPrecision</c> turned 0.015000 (scale 6 → 2, HALF_EVEN) into 0.01 instead of 0.02
    /// (dinerojs/dinero.js#187).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenRescalingMidpointHalfEven_ShouldRoundToEvenNeighbour() =>
        Assert.AreEqual(0.02m, Money.FromExplicitScale(0.015m, CurrencyCode.USD, 6).Rescale(2).Amount);

    /// <summary>
    /// Verifies that multiplying a negative amount by one is the identity — dinero.js turned −200 × 1 into −201
    /// through an off-by-one in negative rounding (dinerojs/dinero.js#713).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenMultiplyingNegativeByOne_ShouldBeIdentity() =>
        Assert.AreEqual(-2.00m, (new Money(-2.00m, CurrencyCode.USD) * 1m).Amount);

    /// <summary>
    /// Verifies that rescaling an exact zero from a wide scale yields zero — dinero.js's half-even rounding
    /// manufactured a minor unit from zero (<c>transformScale</c> 7 → 2 of 0 gave 1; dinerojs/dinero.js#710).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenRescalingZeroFromWideScale_ShouldStayZero() =>
        Assert.AreEqual(0m, Money.FromExplicitScale(0m, CurrencyCode.USD, 7).Rescale(2).Amount);

    /// <summary>
    /// Verifies that a negative midpoint rescaled away-from-zero moves away from zero — dinero.js v1 pulled −2.05
    /// toward zero (−2 instead of −2.1) via <c>Math.round</c> semantics (dinerojs/dinero.js#7).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenRescalingNegativeMidpointAwayFromZero_ShouldMoveAwayFromZero() =>
        Assert.AreEqual(-2.1m, new Money(-2.05m, CurrencyCode.USD).Rescale(1, MidpointRounding.AwayFromZero).Amount);

    /// <summary>
    /// Verifies that zero-minor-unit currencies format without fractional digits and without duplicating the whole
    /// part — dinero.js printed <c>¥500.00</c> (dinerojs/dinero.js#203) and Joda-Money printed <c>"1212"</c> for
    /// 12 JPY through a substring defect (JodaOrg/joda-money#49).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenFormattingZeroMinorUnitCurrency_ShouldPrintWholeUnitsOnly()
    {
        Assert.AreEqual("JPY 500", new Money(500m, CurrencyCode.JPY).ToString("R"));
        Assert.AreEqual("JPY 12", new Money(12.134m, CurrencyCode.JPY).ToString("R"));
    }

    /// <summary>
    /// Verifies grouped formatting renders correctly around the defect corners Joda-Money hit: no trailing group
    /// separator on a three-decimal currency (JodaOrg/joda-money#43), no separator between the sign and the first
    /// digit (JodaOrg/joda-money#10), and no grouping inside the fraction (JodaOrg/joda-money#16).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenFormattingGroupedAmounts_ShouldPlaceSeparatorsCorrectly()
    {
        Assert.AreEqual("BHD 6,345,345.735", new Money(6345345.735m, CurrencyCode.BHD).ToString("G", CultureInfo.InvariantCulture));
        Assert.AreEqual("USD -100,000.00", new Money(-100000.00m, CurrencyCode.USD).ToString("G", CultureInfo.InvariantCulture));
        Assert.AreEqual(
            "USD 1,234,567.891011",
            Money.FromExplicitScale(1234567.891011m, CurrencyCode.USD, 6).ToString("G", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that formatted output parses back to an equal value — the print/parse asymmetry defect class where a
    /// formatter emits text its own parser rejects (JodaOrg/joda-money#44).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenReparsingGroupedOutput_ShouldRoundTrip()
    {
        Money original = new Money(1234567.89m, CurrencyCode.USD);

        Money reparsed = Money.Parse(original.ToString("G", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        Assert.AreEqual(original, reparsed);
    }

    /// <summary>
    /// Verifies that a non-breaking space separating the code and amount parses — Joda-Money rejects the NBSP that
    /// localized number formatters commonly emit (JodaOrg/joda-money#53, closed WontFix).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenSeparatorIsNonBreakingSpace_ShouldParse()
    {
        Assert.IsTrue(Money.TryParse("USD 1794.19", CultureInfo.InvariantCulture, out Money prefix));
        Assert.AreEqual(new Money(1794.19m, CurrencyCode.USD), prefix);

        Assert.IsTrue(Money.TryParse("1794.19 USD", CultureInfo.InvariantCulture, out Money suffix));
        Assert.AreEqual(new Money(1794.19m, CurrencyCode.USD), suffix);
    }

    /// <summary>
    /// Verifies that a lowercase ISO code is rejected by the strict parser — the case-clashing currency registration
    /// defect class where <c>"usd"</c> and <c>"USD"</c> could coexist as distinct currencies
    /// (JodaOrg/joda-money#36).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenIsoCodeIsLowercase_ShouldNotParse() =>
        Assert.IsFalse(Money.TryParse("usd 5", CultureInfo.InvariantCulture, out _));

    /// <summary>
    /// Verifies that cash rounding to a currency's smallest physical denomination is supported — CHF amounts snap to
    /// the 0.05 increment Joda-Money cannot express (JodaOrg/joda-money#71, open RFE).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenCashRoundingChf_ShouldSnapToFiveCentimeIncrement()
    {
        var cashContext = MonetaryContext.Default with { CashRounding = CashRoundingPolicy.CurrencyCashIncrement };

        Assert.AreEqual(2.05m, new CalculatedMoney(2.03m, CurrencyCode.CHF).RoundToMoney(cashContext).Amount);
        Assert.AreEqual(2.00m, new CalculatedMoney(2.02m, CurrencyCode.CHF).RoundToMoney(cashContext).Amount);
    }

    /// <summary>
    /// Verifies that the deferred tier eliminates per-operation rounding drift — Joda-Money's <c>Money</c> rounds
    /// after every multiply/divide, accumulating cent drift across chains with no complete remedy
    /// (JodaOrg/joda-money#6). The settled tier documents the drift; <see cref="CalculatedMoney" /> avoids it.
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenChainingDivideMultiply_ShouldOfferDriftFreeDeferredTier()
    {
        Money perStep = (new Money(0.10m, CurrencyCode.USD) / 3m) * 3m;
        Money deferred = (new CalculatedMoney(0.10m, CurrencyCode.USD) / 3m * 3m).RoundToMoney();

        Assert.AreEqual(0.09m, perStep.Amount);
        Assert.AreEqual(0.10m, deferred.Amount);
    }

    /// <summary>
    /// Verifies that pseudo-currencies are rejected loudly rather than silently clamped — Joda-Money reports the
    /// scale of XAU (stored as −1 decimal places) as 0, indistinguishable from a genuine zero-decimal currency
    /// (JodaOrg/joda-money CurrencyUnit javadoc).
    /// </summary>
    [TestMethod]
    public void PriorArtDefects_WhenResolvingPseudoCurrency_ShouldRejectRatherThanClamp() =>
        Assert.IsFalse(CurrencyInfo.TryGetCurrencyCode("XAU", out _));

    /// <summary>
    /// Verifies that the shipped minor-unit metadata matches ISO 4217 for the currencies Joda-Money serially got
    /// wrong — CNY (JodaOrg/joda-money#51), COP (#48), ZMW (#57), and LBP (fixed in v0.7).
    /// </summary>
    [TestMethod]
    [DataRow("CNY", 2, DisplayName = "CNY (joda-money#51)")]
    [DataRow("COP", 2, DisplayName = "COP (joda-money#48)")]
    [DataRow("ZMW", 2, DisplayName = "ZMW (joda-money#57)")]
    [DataRow("LBP", 2, DisplayName = "LBP (joda-money v0.7)")]
    [DataRow("BHD", 3, DisplayName = "BHD (three minor units)")]
    [DataRow("JPY", 0, DisplayName = "JPY (zero minor units)")]
    public void PriorArtDefects_WhenReadingRegistryMetadata_ShouldMatchIso4217(string isoCode, int expectedMinorUnits) =>
        Assert.AreEqual(expectedMinorUnits, CurrencyRegistry.Get(isoCode).MinorUnits);
}
