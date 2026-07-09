// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.ConvertWithRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// The observation date used by the quote-first conversion tests.
    /// </summary>
    private static readonly DateOnly s_rateDate = new(2024, 1, 1);

    /// <summary>
    /// Verifies that <see cref="Money.Convert(ExchangeRate, MonetaryContext?)" /> produces the target currency rounded
    /// to its minor units.
    /// </summary>
    [TestMethod]
    public void Convert_WhenRateSourceMatches_ShouldProduceTargetCurrency()
    {
        Money usd = new(100m, CurrencyCode.USD);
        ExchangeRate rate = new(CurrencyCode.USD, CurrencyCode.AUD, s_rateDate, 1.5m, "Test");

        Money aud = usd.Convert(rate);

        Assert.AreEqual(new Money(150.00m, CurrencyCode.AUD), aud);
    }

    /// <summary>
    /// Verifies that <see cref="Money.Convert(ExchangeRate, MonetaryContext?)" /> throws when the rate's source
    /// currency does not match.
    /// </summary>
    [TestMethod]
    public void Convert_WhenRateSourceMismatches_ShouldThrowInvalidOperationException()
    {
        Money eur = new(100m, CurrencyCode.EUR);
        ExchangeRate rate = new(CurrencyCode.USD, CurrencyCode.AUD, s_rateDate, 1.5m, "Test");

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = eur.Convert(rate));
    }

    /// <summary>
    /// Verifies that <see cref="Money.ConvertWithResult(ExchangeRate, MonetaryContext?)" /> reports the source, target,
    /// rate, and the signed rounding adjustment.
    /// </summary>
    [TestMethod]
    public void ConvertWithResult_ShouldReportRoundingAdjustment()
    {
        Money usd = new(1m, CurrencyCode.USD);
        ExchangeRate rate = new(CurrencyCode.USD, CurrencyCode.AUD, s_rateDate, 1.005m, "Test");

        MoneyConversionResult result = usd.ConvertWithResult(rate);

        // 1 * 1.005 = 1.005, banker's rounding to 2 dp = 1.00, so the adjustment is -0.005.
        Assert.AreEqual(new Money(1.00m, CurrencyCode.AUD), result.Target);
        Assert.AreEqual(usd, result.Source);
        Assert.AreEqual(rate, result.Rate);
        Assert.AreEqual(-0.005m, result.RoundingAdjustment);
    }
}
