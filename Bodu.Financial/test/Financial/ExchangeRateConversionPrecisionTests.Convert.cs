// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateConversionPrecisionTests.Convert.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public sealed partial class ExchangeRateConversionPrecisionTests
{

    /// <summary>
    /// Verifies that converting the observed amount through an inverted rate divides exactly, where multiplying by the
    /// rounded reciprocal would not yield the exact result.
    /// </summary>
    [TestMethod]
    public void Convert_WhenInvertedFromObservedRate_ShouldDivideExactly()
    {
        var inverted = ExchangeRate.FromObservedRate(CurrencyCode.JPY, CurrencyCode.USD, Date, 156.42m, "ECB", isInverted: true);

        // 156.42 JPY -> USD must be exactly 1; amount * (1 / 156.42) would round to 0.999...9.
        Assert.AreEqual(1m, inverted.Convert(156.42m));
        Assert.AreNotEqual(1m, 156.42m * (1m / 156.42m));
    }
}
