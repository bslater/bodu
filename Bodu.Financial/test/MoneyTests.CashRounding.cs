// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.CashRounding.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="Money{TCurrency}.RoundToCash()" /> and the
/// <see cref="ICurrency.CashRoundingIncrement" /> default behaviour across currencies that do and do not declare
/// a cash-rounding increment.
/// </summary>
public partial class MoneyTests
{
    /// <summary>
    /// Verifies that CHF rounds to the nearest 5-rappen increment using banker's rounding.
    /// </summary>
    [TestMethod]
    [DataRow(12.34, 12.35)]   // 0.34 → 0.35 (banker's: 6.8 → 7 → ×0.05 = 0.35)
    [DataRow(12.33, 12.35)]   // 0.33 → 0.35 (6.6 → 7 → 0.35)
    [DataRow(12.32, 12.30)]   // 0.32 → 0.30 (6.4 → 6 → 0.30)
    [DataRow(12.31, 12.30)]   // 0.31 → 0.30 (6.2 → 6 → 0.30)
    [DataRow(12.30, 12.30)]   // exact multiple
    [DataRow(12.35, 12.35)]   // exact multiple
    [DataRow(12.325, 12.30)]  // halfway between 12.30 and 12.35 → banker's down to even multiple 6
    [DataRow(0.00, 0.00)]
    [DataRow(-12.34, -12.35)]
    public void RoundToCash_WhenChf_ShouldRoundToFiveRappen(double amount, double expected)
    {
        Money<CHF> result = new Money<CHF>((decimal)amount).RoundToCash();

        Assert.AreEqual(new Money<CHF>((decimal)expected), result);
    }

    /// <summary>
    /// Verifies that AUD rounds to the nearest 5-cent increment for cash totals, leaving electronic
    /// (constructor-time) precision intact at the 2-dp boundary.
    /// </summary>
    [TestMethod]
    [DataRow(5.07, 5.05)]
    [DataRow(5.08, 5.10)]
    [DataRow(5.10, 5.10)]
    [DataRow(5.125, 5.10)]      // midpoint → banker's down to even multiple 2 (5.10)
    public void RoundToCash_WhenAud_ShouldRoundToFiveCents(double amount, double expected)
    {
        Money<AUD> result = new Money<AUD>((decimal)amount).RoundToCash();

        Assert.AreEqual(new Money<AUD>((decimal)expected), result);
    }

    /// <summary>
    /// Verifies that <see cref="MidpointRounding.AwayFromZero" /> selects the upward rounding at the cash-rounding
    /// midpoint, in contrast to the banker's-rounding default. NZD is used because its 0.10 cash increment
    /// places the rounding midpoint (0.05, 0.15, ...) on a minor-unit boundary the constructor preserves; CHF's
    /// 0.05 increment would place midpoints at 0.025 / 0.075, which the 2-dp constructor pre-rounds away.
    /// </summary>
    [TestMethod]
    public void RoundToCash_WhenAwayFromZeroSpecified_ShouldRoundAwayFromZeroAtMidpoint()
    {
        Money<NZD> banker = new Money<NZD>(5.05m).RoundToCash();
        Money<NZD> awayFromZero = new Money<NZD>(5.05m).RoundToCash(MidpointRounding.AwayFromZero);

        Assert.AreEqual(new Money<NZD>(5.00m), banker);           // 50.5 → banker's down to even 50 → 5.00
        Assert.AreEqual(new Money<NZD>(5.10m), awayFromZero);     // 50.5 → AwayFromZero → 51 → 5.10
    }

    /// <summary>
    /// Verifies that NZD rounds to the nearest 10-cent denomination for cash totals.
    /// </summary>
    [TestMethod]
    [DataRow(5.04, 5.00)]
    [DataRow(5.05, 5.00)]      // midpoint → banker's down to even multiple 50
    [DataRow(5.06, 5.10)]
    [DataRow(5.15, 5.20)]      // midpoint → banker's up to even multiple 52
    public void RoundToCash_WhenNzd_ShouldRoundToTenCents(double amount, double expected)
    {
        Money<NZD> result = new Money<NZD>((decimal)amount).RoundToCash();

        Assert.AreEqual(new Money<NZD>((decimal)expected), result);
    }

    /// <summary>
    /// Verifies that SEK rounds to the nearest whole krona for cash totals.
    /// </summary>
    [TestMethod]
    [DataRow(12.49, 12)]
    [DataRow(12.50, 12)]        // midpoint → banker's down to even 12
    [DataRow(12.51, 13)]
    [DataRow(13.50, 14)]        // midpoint → banker's up to even 14
    public void RoundToCash_WhenSek_ShouldRoundToWholeKrona(double amount, int expected)
    {
        Money<SEK> result = new Money<SEK>((decimal)amount).RoundToCash();

        Assert.AreEqual(new Money<SEK>(expected), result);
    }

    /// <summary>
    /// Verifies that currencies which do not declare a cash-rounding increment (USD, JPY, EUR, BHD) leave the
    /// amount unchanged so the call is a no-op.
    /// </summary>
    [TestMethod]
    public void RoundToCash_WhenCurrencyHasNoCashIncrement_ShouldReturnUnchangedAmount()
    {
        Money<USD> usd = new Money<USD>(19.99m);
        Money<JPY> jpy = new Money<JPY>(99m);
        Money<EUR> eur = new Money<EUR>(12.34m);
        Money<BHD> bhd = new Money<BHD>(12.345m);

        Assert.AreEqual(usd, usd.RoundToCash());
        Assert.AreEqual(jpy, jpy.RoundToCash());
        Assert.AreEqual(eur, eur.RoundToCash());
        Assert.AreEqual(bhd, bhd.RoundToCash());
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.CashRoundingIncrement" /> exposes the shipped tag's increment
    /// for the four representative cash-rounded currencies and reports zero for currencies that do not declare
    /// one.
    /// </summary>
    [TestMethod]
    public void CashRoundingIncrement_WhenAccessed_ShouldReflectCurrencyMetadata()
    {
        Assert.AreEqual(0.05m, Money<CHF>.CashRoundingIncrement);
        Assert.AreEqual(0.05m, Money<AUD>.CashRoundingIncrement);
        Assert.AreEqual(0.05m, Money<CAD>.CashRoundingIncrement);
        Assert.AreEqual(0.10m, Money<NZD>.CashRoundingIncrement);
        Assert.AreEqual(1m, Money<SEK>.CashRoundingIncrement);
        Assert.AreEqual(1m, Money<NOK>.CashRoundingIncrement);

        Assert.AreEqual(0m, Money<USD>.CashRoundingIncrement);
        Assert.AreEqual(0m, Money<JPY>.CashRoundingIncrement);
        Assert.AreEqual(0m, Money<EUR>.CashRoundingIncrement);
    }
}
