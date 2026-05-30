// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.WorkedExamples.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Financial.Currencies;

using Bodu.Numerics;

namespace Bodu.Financial;

/// <summary>
/// End-to-end worked scenarios that exercise <see cref="Money{TCurrency}" /> the way real-world accounting,
/// banking, retail, and FX code would. Each test reproduces a canonical calculation and asserts the exact final
/// value an accountant or actuary would expect, including the rounding boundaries that bite in practice.
/// </summary>
public partial class MoneyTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // Hospitality — restaurant bill with tip on pre-tax subtotal vs tax-inclusive total.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies a restaurant-bill computation: subtotal, 10% sales tax, 18% tip on the pre-tax subtotal, and
    /// final total. Confirms the additive chain produces the expected to-the-cent amount.
    /// </summary>
    [TestMethod]
    public void Hospitality_WhenComputingRestaurantBillWithTipOnPreTaxSubtotal_ShouldMatchExpectedTotal()
    {
        var subtotal = new Money<USD>(54.30m);
        Money<USD> tax = subtotal * 0.10m;
        Money<USD> tipOnNet = subtotal * 0.18m;

        Money<USD> total = subtotal + tax + tipOnNet;

        Assert.AreEqual(new Money<USD>(5.43m), tax);
        Assert.AreEqual(new Money<USD>(9.77m), tipOnNet);     // 54.30 × 0.18 = 9.774 → 9.77
        Assert.AreEqual(new Money<USD>(69.50m), total);
    }

    /// <summary>
    /// Verifies that tipping on the tax-inclusive total yields a larger total than tipping on the pre-tax
    /// subtotal, by the precisely-expected delta.
    /// </summary>
    [TestMethod]
    public void Hospitality_WhenTippingOnTaxInclusiveTotal_ShouldDifferFromPreTaxTippingByExpectedDelta()
    {
        var subtotal = new Money<USD>(50.00m);
        Money<USD> taxedTotal = subtotal + subtotal * 0.10m;     // 50 + 5 = 55

        Money<USD> tipOnNet = subtotal * 0.20m;                  // 10.00
        Money<USD> tipOnGross = taxedTotal * 0.20m;              // 11.00

        Assert.AreEqual(new Money<USD>(10.00m), tipOnNet);
        Assert.AreEqual(new Money<USD>(11.00m), tipOnGross);
        Assert.AreEqual(new Money<USD>(1.00m), tipOnGross - tipOnNet);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Retail — multi-line invoice with tax-inclusive line pricing (AU/NZ GST convention).
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that an AU/NZ-style 10% tax-inclusive invoice extracts the GST component (1/11 of gross) and net
    /// portion correctly across multiple lines.
    /// </summary>
    [TestMethod]
    public void Retail_WhenExtractingTenPercentGstFromTaxInclusiveLines_ShouldRecoverNetAndTax()
    {
        var line1 = new Money<AUD>(4.50m);
        var line2 = new Money<AUD>(12.00m);
        var line3 = new Money<AUD>(3.30m);

        Money<AUD> gross = line1 + line2 + line3;       // 19.80
        Money<AUD> gst = gross / 11m;                   // 19.80 / 11 = 1.80 exactly
        Money<AUD> net = gross - gst;                   // 18.00

        Assert.AreEqual(new Money<AUD>(19.80m), gross);
        Assert.AreEqual(new Money<AUD>(1.80m), gst);
        Assert.AreEqual(new Money<AUD>(18.00m), net);
    }

    /// <summary>
    /// Verifies a sequential-discount cascade ($100 with 10% then 5%) produces the same value as a one-shot
    /// combined-discount calculation, and that the equivalence holds for a value where the two paths visibly
    /// agree.
    /// </summary>
    [TestMethod]
    public void Retail_WhenApplyingSequentialDiscountsOnEvenAmount_ShouldMatchCombinedCalculation()
    {
        var list = new Money<USD>(100.00m);

        Money<USD> sequential = (list * 0.90m) * 0.95m;          // 100 → 90 → 85.50
        Money<USD> combined = list * (0.90m * 0.95m);            // 100 × 0.855 = 85.50

        Assert.AreEqual(new Money<USD>(85.50m), sequential);
        Assert.AreEqual(new Money<USD>(85.50m), combined);
    }

    /// <summary>
    /// Verifies the documented per-step rounding drift when sequential discounts are applied to an amount that
    /// rounds at each step ($33.33 × 0.95 × 0.90 vs $33.33 × 0.855). The intermediate-rounded chain ends a cent
    /// below the single-shot calculation — the kind of drift that matters in retail pricing engines.
    /// </summary>
    [TestMethod]
    public void Retail_WhenSequentialDiscountsCauseIntermediateRounding_ShouldDriftFromSingleShot()
    {
        var list = new Money<USD>(33.33m);

        Money<USD> sequential = (list * 0.95m) * 0.90m;          // 33.33×0.95=31.66 (rounded), ×0.90=28.49
        Money<USD> combined = list * (0.95m * 0.90m);            // 33.33×0.855=28.49715 → 28.50

        Assert.AreEqual(new Money<USD>(28.49m), sequential);
        Assert.AreEqual(new Money<USD>(28.50m), combined);
        Assert.AreEqual(new Money<USD>(0.01m), combined - sequential);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Payroll — annualisation, semi-monthly, and overtime accrual.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that an hourly wage scales to the textbook annual salary across the standard 40-hour-week,
    /// 52-week-year convention.
    /// </summary>
    [TestMethod]
    [DataRow(25.50, 53040.00)]    // standard
    [DataRow(15.00, 31200.00)]    // minimum-wage band
    [DataRow(0.01, 20.80)]        // single-cent edge case to verify no loss across the chain
    [DataRow(100.00, 208000.00)]
    public void Payroll_WhenAnnualisingHourlyWage_ShouldMatchFortyHourFiftyTwoWeekConvention(double hourly, double expectedAnnual)
    {
        var hourlyRate = new Money<USD>((decimal)hourly);

        Money<USD> annual = hourlyRate * 40m * 52m;

        Assert.AreEqual(new Money<USD>((decimal)expectedAnnual), annual);
    }

    /// <summary>
    /// Verifies that overtime accrual (1.5× the base rate above 40 hours/week) totals the expected weekly pay
    /// when the per-week hours straddle the overtime threshold.
    /// </summary>
    [TestMethod]
    public void Payroll_WhenOvertimeAccrual_ShouldSumBaseAndOnePointFiveTimeAboveForty()
    {
        var hourly = new Money<USD>(20.00m);
        var hoursWorked = 47m;

        Money<USD> basePay = hourly * 40m;
        Money<USD> overtimePay = hourly * 1.5m * (hoursWorked - 40m);
        Money<USD> weekly = basePay + overtimePay;

        Assert.AreEqual(new Money<USD>(800.00m), basePay);
        Assert.AreEqual(new Money<USD>(210.00m), overtimePay);   // 20×1.5×7 = 210
        Assert.AreEqual(new Money<USD>(1010.00m), weekly);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Banking — simple and compound interest.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies the simple-interest formula <c>I = P × r × t</c> across representative principal / rate / term
    /// combinations.
    /// </summary>
    [TestMethod]
    [DataRow(5000.00, 0.04, 2, 400.00, 5400.00)]
    [DataRow(1000.00, 0.075, 5, 375.00, 1375.00)]
    [DataRow(250.00, 0.10, 1, 25.00, 275.00)]
    [DataRow(0.00, 0.05, 3, 0.00, 0.00)]                 // zero principal accrues no interest
    public void Banking_WhenSimpleInterest_ShouldMatchFormula(double principal, double rate, int years, double expectedInterest, double expectedFinal)
    {
        var p = new Money<USD>((decimal)principal);

        Money<USD> interest = p * (decimal)rate * years;
        Money<USD> finalAmount = p + interest;

        Assert.AreEqual(new Money<USD>((decimal)expectedInterest), interest);
        Assert.AreEqual(new Money<USD>((decimal)expectedFinal), finalAmount);
    }

    /// <summary>
    /// Verifies year-by-year compound interest on $1,000 at 5% annual, compounded annually, for 3 years lands
    /// on the textbook value with the rounding boundary on the right side of $1,157.625 → $1,157.62 (banker's
    /// rounds the 5 down toward the even hundredth digit 2).
    /// </summary>
    [TestMethod]
    public void Banking_WhenCompoundInterestAnnualThreeYears_ShouldMatchTextbook()
    {
        var principal = new Money<USD>(1000.00m);

        Money<USD> year1 = principal * 1.05m;        // 1050.00
        Money<USD> year2 = year1 * 1.05m;            // 1102.50
        Money<USD> year3 = year2 * 1.05m;            // 1157.625 → 1157.62 banker's

        Assert.AreEqual(new Money<USD>(1050.00m), year1);
        Assert.AreEqual(new Money<USD>(1102.50m), year2);
        Assert.AreEqual(new Money<USD>(1157.62m), year3);
    }

    /// <summary>
    /// Verifies that compound interest computed exactly through <see cref="Fraction{T}" /> produces the same
    /// final value as the step-by-step scalar chain when the rate has a finite decimal representation, since
    /// no intermediate rounding occurs at the year boundaries.
    /// </summary>
    [TestMethod]
    public void Banking_WhenCompoundInterestComputedExactly_ShouldAgreeWithScalarChainAtFiveByThree()
    {
        var principal = new Money<USD>(1000.00m);

        var growth = Fraction<BigInteger>.Create(105, 100);
        Fraction<BigInteger> rational = principal.ToFraction() * growth * growth * growth;
        var exact = Money<USD>.FromFraction(rational);

        Money<USD> scalar = principal * 1.05m * 1.05m * 1.05m;

        Assert.AreEqual(new Money<USD>(1157.62m), exact);
        Assert.AreEqual(exact, scalar);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Mortgage — first-payment principal/interest split.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies the first-month interest / principal breakdown of a fixed-rate mortgage when the monthly payment
    /// is precomputed externally.
    /// </summary>
    [TestMethod]
    public void Mortgage_WhenFirstPaymentBreakdown_ShouldSplitInterestAndPrincipalCorrectly()
    {
        var openingBalance = new Money<USD>(300_000m);
        var monthlyRate = 0.00375m;                     // 4.5% APR / 12
        var monthlyPayment = new Money<USD>(1520.06m);

        Money<USD> interestPortion = openingBalance * monthlyRate;       // 1,125.00 exactly
        Money<USD> principalPortion = monthlyPayment - interestPortion;
        Money<USD> closingBalance = openingBalance - principalPortion;

        Assert.AreEqual(new Money<USD>(1125.00m), interestPortion);
        Assert.AreEqual(new Money<USD>(395.06m), principalPortion);
        Assert.AreEqual(new Money<USD>(299_604.94m), closingBalance);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // FX — single-leg, round-trip, and a rate that loses value below the destination minor unit.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies a single-leg FX conversion from USD to EUR at a flat rate produces the expected destination
    /// amount at EUR's 2-dp precision.
    /// </summary>
    [TestMethod]
    [DataRow(100.00, 0.92, 92.00)]
    [DataRow(50.00, 0.92, 46.00)]
    [DataRow(1234.56, 1.10, 1358.02)]     // 1234.56 × 1.10 = 1358.016 → 1358.02 banker's (1 is odd)
    [DataRow(0.05, 0.92, 0.05)]           // 0.05 × 0.92 = 0.0460; rounds to 0.05 at EUR's 2-dp precision (6 > 5)
    public void Fx_WhenConvertingUsdToEur_ShouldRoundToEurPrecision(double amount, double rate, double expected)
    {
        var source = new Money<USD>((decimal)amount);

        Money<EUR> target = source.Convert<EUR>((decimal)rate);

        Assert.AreEqual(new Money<EUR>((decimal)expected), target);
    }

    /// <summary>
    /// Verifies a USD → EUR → USD round-trip at flat rates recovers the original amount when the inverse rate
    /// has been computed precisely enough that the rounded EUR step does not lose a cent on the way back.
    /// </summary>
    [TestMethod]
    public void Fx_WhenRoundTrippingUsdToEurAndBack_ShouldRecoverOriginalAmount()
    {
        var original = new Money<USD>(100.00m);
        var usdToEur = 0.92m;
        var eurToUsd = 1.0869565m;                  // 1/0.92 truncated; multiplied back, rounds to 100.00

        Money<EUR> intermediate = original.Convert<EUR>(usdToEur);
        Money<USD> recovered = intermediate.Convert<USD>(eurToUsd);

        Assert.AreEqual(new Money<EUR>(92.00m), intermediate);
        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that converting an amount below the destination currency's minor unit rounds to zero, with the
    /// loss-of-value being explicit rather than silently retained.
    /// </summary>
    [TestMethod]
    public void Fx_WhenRateProducesSubMinorUnitResult_ShouldRoundToZeroAtDestination()
    {
        var dust = new Money<USD>(0.01m);
        var rate = 0.001m;                          // 0.01 × 0.001 = 0.00001

        Money<JPY> result = dust.Convert<JPY>(rate);

        Assert.AreEqual(Money<JPY>.Zero, result);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Investing — stock dividend payout exercising the MultiplyExact escape hatch.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that computing a dividend payout via <see cref="Money{TCurrency}.MultiplyExact" /> on a sub-cent
    /// dividend per share recovers the actuarially correct total, while the naive "construct DPS as Money first,
    /// then multiply" approach loses more than a dollar to early rounding.
    /// </summary>
    [TestMethod]
    public void Investing_WhenComputingDividendPayoutAtSubCentDps_ShouldUseMultiplyExactForCorrectTotal()
    {
        // Shares: 250. Declared dividend per share: $0.4275 (sub-cent precision).
        var shares = 250;
        var dpsExact = Fraction<BigInteger>.Create(4275, 10000);    // 0.4275 exactly

        // The naive path constructs Money<USD> with the DPS first — losing the 4th decimal to rounding.
        var naiveDps = new Money<USD>(0.4275m);                               // → 0.43 (digit at position 3 is 7 → rounds up at 2 dp)
        Money<USD> naivePayout = naiveDps * shares;                                  // 0.43 × 250 = 107.50

        // The exact path multiplies through Fraction<BigInteger> and rounds only at settlement.
        var exactPayout = Money<USD>.FromFraction(dpsExact * Fraction<BigInteger>.Create(shares, 1));

        Assert.AreEqual(new Money<USD>(107.50m), naivePayout);
        Assert.AreEqual(new Money<USD>(106.88m), exactPayout);                       // 106.875 → 106.88 (banker's: 7 odd, neighbour 8 even)
        Assert.AreEqual(new Money<USD>(0.62m), naivePayout - exactPayout);           // 62 cents over-paid by the naive path
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Banking — overdraft / negative-balance progression.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies a checking-account overdraft progression — withdrawal, overdraft-fee debit, deposit — produces
    /// the expected signed balance at each step.
    /// </summary>
    [TestMethod]
    public void Banking_WhenOverdraftProgression_ShouldTrackSignedBalance()
    {
        Money<USD> opening = Money<USD>.Zero;

        Money<USD> afterWithdrawal = opening - new Money<USD>(50.00m);
        Money<USD> afterFee = afterWithdrawal - new Money<USD>(35.00m);
        Money<USD> afterDeposit = afterFee + new Money<USD>(100.00m);

        Assert.AreEqual(new Money<USD>(-50.00m), afterWithdrawal);
        Assert.IsTrue(afterWithdrawal.IsNegative);
        Assert.AreEqual(new Money<USD>(-85.00m), afterFee);
        Assert.AreEqual(new Money<USD>(15.00m), afterDeposit);
        Assert.IsTrue(afterDeposit.IsPositive);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Edge case — single-cent transactions, zero amounts, single-part allocation.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies the minimum representable USD transaction round-trips through arithmetic without loss.
    /// </summary>
    [TestMethod]
    public void EdgeCase_WhenSingleCentTransaction_ShouldRoundTripWithoutLoss()
    {
        var cent = new Money<USD>(0.01m);

        Money<USD> doubled = cent + cent;
        Money<USD> back = doubled - cent;

        Assert.AreEqual(new Money<USD>(0.02m), doubled);
        Assert.AreEqual(cent, back);
    }

    /// <summary>
    /// Verifies that allocating zero across any number of parts produces an all-zero array whose length matches
    /// the requested parts.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(7)]
    [DataRow(1000)]
    public void EdgeCase_WhenAllocatingZeroAcrossAnyParts_ShouldProduceAllZeroShares(int parts)
    {
        Money<USD>[] shares = Money<USD>.Zero.Allocate(parts);

        Assert.AreEqual(parts, shares.Length);
        foreach (Money<USD> share in shares)
            Assert.AreEqual(Money<USD>.Zero, share);
    }

    /// <summary>
    /// Verifies that allocating any amount across a single part returns the unchanged amount as the only share.
    /// </summary>
    [TestMethod]
    [DataRow(0.00)]
    [DataRow(0.01)]
    [DataRow(100.00)]
    [DataRow(-50.00)]
    [DataRow(1234567.89)]
    public void EdgeCase_WhenAllocatingAcrossSinglePart_ShouldReturnSingletonWithOriginalAmount(double amount)
    {
        var original = new Money<USD>((decimal)amount);

        Money<USD>[] shares = original.Allocate(1);

        Assert.AreEqual(1, shares.Length);
        Assert.AreEqual(original, shares[0]);
    }

    /// <summary>
    /// Verifies that allocating an amount of three minor units across four parts produces three single-unit
    /// shares followed by a zero share — the canonical "amount smaller than parts" residual pattern.
    /// </summary>
    [TestMethod]
    public void EdgeCase_WhenAmountIsThreeMinorUnitsAcrossFourParts_ShouldPadTrailingZero()
    {
        Money<USD>[] shares = new Money<USD>(0.03m).Allocate(4);

        Assert.AreEqual(new Money<USD>(0.01m), shares[0]);
        Assert.AreEqual(new Money<USD>(0.01m), shares[1]);
        Assert.AreEqual(new Money<USD>(0.01m), shares[2]);
        Assert.AreEqual(Money<USD>.Zero, shares[3]);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Edge case — minor-unit boundaries across all three categories on the same scenario.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the same 10% tax computation applied to a unit amount produces minor-unit-precision results
    /// in each currency category — JPY rounds to whole units, USD to two places, BHD to three.
    /// </summary>
    [TestMethod]
    public void EdgeCase_WhenApplyingTenPercentTaxAcrossMinorUnitCategories_ShouldRoundAtEachCurrencysPrecision()
    {
        var jpy = new Money<JPY>(99m);
        var usd = new Money<USD>(99m);
        var bhd = new Money<BHD>(99m);

        Money<JPY> jpyTax = jpy * 0.10m;    // 9.9 → 10 banker's (9.9 → 10 since 0 is even, but 9.9 is not midpoint; just round nearest)
        Money<USD> usdTax = usd * 0.10m;    // 9.90 exactly
        Money<BHD> bhdTax = bhd * 0.10m;    // 9.900 exactly

        Assert.AreEqual(new Money<JPY>(10m), jpyTax);
        Assert.AreEqual(new Money<USD>(9.90m), usdTax);
        Assert.AreEqual(new Money<BHD>(9.900m), bhdTax);
    }

    /// <summary>
    /// Verifies banker's-rounding behaviour on midpoint values across all three currency precision categories.
    /// JPY rounds <c>8.5 → 8</c> (down to even); USD rounds <c>0.225 → 0.22</c> (down to even);
    /// BHD rounds <c>0.0025 → 0.002</c> (down to even).
    /// </summary>
    [TestMethod]
    public void EdgeCase_WhenMidpointRoundingAcrossMinorUnitCategories_ShouldRoundToEvenInEachPrecision()
    {
        Assert.AreEqual(new Money<JPY>(8m), new Money<JPY>(8.5m));
        Assert.AreEqual(new Money<USD>(0.22m), new Money<USD>(0.225m));
        Assert.AreEqual(new Money<BHD>(0.002m), new Money<BHD>(0.0025m));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Edge case — high-precision retention through Fraction for "thirds" and "sevenths" that scalar would drift on.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that splitting $1 into thirds via scalar arithmetic drifts a cent on multiply-back, while the
    /// <see cref="Fraction{T}" />-based escape hatch preserves the original amount exactly.
    /// </summary>
    [TestMethod]
    public void EdgeCase_WhenSplittingDollarIntoThirdsAndMultiplyingBack_ShouldDriftScalarButNotFraction()
    {
        var original = new Money<USD>(1.00m);

        Money<USD> scalarBack = (original / 3m) * 3m;        // 0.33 × 3 = 0.99

        var third = Fraction<BigInteger>.Create(1, 3);
        Fraction<BigInteger> exactBack = original.ToFraction() * third * Fraction<BigInteger>.Create(3, 1);
        var fractionBack = Money<USD>.FromFraction(exactBack);

        Assert.AreEqual(new Money<USD>(0.99m), scalarBack);
        Assert.AreEqual(original, fractionBack);
    }

    /// <summary>
    /// Verifies that allocating <c>$1.00</c> across seven equal parts sums back exactly to the original because
    /// the residual is distributed by integer minor units, not by per-share rounding.
    /// </summary>
    [TestMethod]
    public void EdgeCase_WhenAllocatingOneDollarAcrossSevenParts_ShouldSumExactly()
    {
        var dollar = new Money<USD>(1.00m);

        Money<USD>[] shares = dollar.Allocate(7);

        Money<USD> sum = Money<USD>.Zero;
        foreach (Money<USD> share in shares)
            sum += share;

        Assert.AreEqual(dollar, sum);

        // First two shares carry the 2-cent residual: 100¢ / 7 = 14 with remainder 2.
        Assert.AreEqual(new Money<USD>(0.15m), shares[0]);
        Assert.AreEqual(new Money<USD>(0.15m), shares[1]);
        Assert.AreEqual(new Money<USD>(0.14m), shares[2]);
        Assert.AreEqual(new Money<USD>(0.14m), shares[6]);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Edge case — Inflation discounting demonstrating Fraction precision retention over many compounding steps.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that inflation-adjusting <c>$1,000</c> back ten years at <c>3%</c> annual inflation through
    /// <see cref="Fraction{T}" /> produces the textbook real-value figure to the cent.
    /// </summary>
    [TestMethod]
    public void EdgeCase_WhenInflationAdjustingTenYearsAtThreePercent_ShouldRecoverTextbookRealValue()
    {
        var nominal = new Money<USD>(1000m);

        var oneOverGrowth = Fraction<BigInteger>.Create(100, 103);
        var rational = nominal.ToFraction();
        for (var i = 0; i < 10; i++)
            rational *= oneOverGrowth;

        var real = Money<USD>.FromFraction(rational);

        // 1000 / (1.03)^10 = 1000 / 1.343916379... = 744.0939...; banker's-rounds to 744.09.
        Assert.AreEqual(new Money<USD>(744.09m), real);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Edge case — extreme precision currencies (BHD, 3 dp) preserved through arithmetic.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that BHD arithmetic preserves the third decimal place exactly across addition and subtraction
    /// without drift.
    /// </summary>
    [TestMethod]
    public void EdgeCase_WhenSummingThreeMillidinarsRepeatedly_ShouldStayAtMillidinarPrecision()
    {
        var milli = new Money<BHD>(0.001m);

        Money<BHD> sum = Money<BHD>.Zero;
        for (var i = 0; i < 1000; i++)
            sum += milli;

        Assert.AreEqual(new Money<BHD>(1.000m), sum);
    }

    /// <summary>
    /// Verifies that BHD-to-USD conversion at a rate that produces sub-cent precision in USD rounds to USD's
    /// 2-dp boundary, with the documented loss of the third decimal.
    /// </summary>
    [TestMethod]
    public void EdgeCase_WhenConvertingBhdToUsdAtFractionalCentRate_ShouldRoundToUsdPrecision()
    {
        var source = new Money<BHD>(1.000m);

        Money<USD> target = source.Convert<USD>(2.6535m);    // 2.6535 → 2.65 in USD precision

        Assert.AreEqual(new Money<USD>(2.65m), target);
    }
}
